<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import { initPGlite, getPGlite } from '$lib/db/pglite-client';
	import * as crud from '$lib/db/crud-operations';
	import { syncManager, syncStatus } from '$lib/db/sync-manager';
	import ManaSymbol from '$lib/components/ManaSymbol.svelte';

	type TableName = 'decks' | 'cards' | 'hands' | 'handevals';
	
	let selectedTable: TableName = 'decks';
	let items: any[] = [];
	let loading = false;
	let showForm = false;
	let editingItem: any = null;
	let formData: any = {};
	let useLocalDB = true; // Toggle between PGlite (local) and Server direct query
	let serverItems: any[] = [];
	let activityLog: string[] = [];
	let pgliteInitialized = false;
	let isServerView = false; // Toggle for split view
	let formFeedback: { type: 'success' | 'error' | null, message: string } = { type: null, message: '' };
	
	// Scryfall Import Management
	let showImportSection = false;
	let importStatus: any = null;
	let importLoading = false;
	let importTriggering = false;
	let imageDownloading = false;
	let forceFullDownload = false;
	
	const BACKEND_URL = 'http://localhost:5000';
	
	/**
	 * Transform form data to match backend DTO expectations
	 */
	function transformFormData(table: TableName, data: any): any {
		switch (table) {
			case 'hands':
				// Convert comma-separated string to int array
				const cardIds = typeof data.card_int_ids === 'string' 
					? data.card_int_ids.split(',').map((s: string) => parseInt(s.trim())).filter((n: number) => !isNaN(n))
					: data.card_int_ids;
				return {
					card_int_ids: cardIds,
					size: parseInt(data.size) || cardIds.length
				};
			case 'handevals':
				return {
					hand1_id: parseInt(data.hand1_id),
					hand2_id: parseInt(data.hand2_id),
					preferred_hand: parseInt(data.preferred_hand),
					context_json: data.context_json || null
				};
			case 'cards':
				return {
					name: data.name,
					mana_cost: data.mana_cost || null,
					type: data.type || null,
					oracle_text: data.oracle_text || null,
					cmc: data.cmc ? parseInt(data.cmc) : null
				};
			default:
				return data;
		}
	}

	const tableSchemas = {
		decks: {
			name: 'Decks',
			fields: [
				{ name: 'name', label: 'Deck Name', type: 'text', required: true },
				{ name: 'format', label: 'Format', type: 'select', options: ['Standard', 'Modern', 'Commander', 'Legacy', 'Vintage', 'Pauper', 'Pioneer'], required: true },
				{ name: 'owner', label: 'Owner', type: 'text', required: false }
			],
			endpoint: '/api/decks'
		},
		cards: {
			name: 'Cards',
			fields: [
				{ name: 'name', label: 'Card Name', type: 'text', required: true },
				{ name: 'mana_cost', label: 'Mana Cost', type: 'text', required: false },
				{ name: 'type', label: 'Type', type: 'text', required: true },
				{ name: 'oracle_text', label: 'Oracle Text', type: 'textarea', required: false },
				{ name: 'cmc', label: 'CMC', type: 'number', required: false }
			],
			endpoint: '/api/cards'
		},
		hands: {
			name: 'Hands',
			fields: [
				{ name: 'card_int_ids', label: 'Card Int IDs (comma-separated)', type: 'text', required: true },
				{ name: 'size', label: 'Hand Size', type: 'number', required: true }
			],
			endpoint: '/api/hands'
		},
		handevals: {
			name: 'Hand Evaluations',
			fields: [
				{ name: 'hand1_id', label: 'Hand 1 ID', type: 'number', required: true },
				{ name: 'hand2_id', label: 'Hand 2 ID', type: 'number', required: true },
				{ name: 'preferred_hand', label: 'Preferred Hand (1 or 2)', type: 'number', required: true }
			],
			endpoint: '/api/handevals'
		}
	};

	function addLog(message: string) {
		const timestamp = new Date().toLocaleTimeString();
		activityLog = [`[${timestamp}] ${message}`, ...activityLog].slice(0, 50); // Keep last 50 logs
	}

	onMount(async () => {
		try {
			addLog('🚀 Initializing PGlite database...');
			await initPGlite();
			pgliteInitialized = true;
			addLog('✅ PGlite initialized successfully');
			
			addLog('🔄 Starting sync manager...');
			await syncManager.startSync(5000); // Sync every 5 seconds
			addLog('✅ Sync manager started');
			
			await loadItems();
		} catch (error: any) {
			addLog(`❌ Initialization failed: ${error.message}`);
			console.error(error);
		}
	});

	onDestroy(() => {
		addLog('⏸️  Stopping sync manager...');
		syncManager.stopSync();
	});

	// Scryfall Import Management Functions
	async function loadImportStatus() {
		importLoading = true;
		try {
			const response = await fetch(`${BACKEND_URL}/api/import/status`);
			if (response.ok) {
				importStatus = await response.json();
			} else {
				console.error('Failed to load import status:', response.status);
			}
		} catch (error: any) {
			console.error('Error loading import status:', error);
		} finally {
			importLoading = false;
		}
	}

	async function triggerImport() {
		if (!confirm('Are you sure you want to trigger a full Scryfall import? This will download ~160 MB and may take 30-60 minutes.')) {
			return;
		}
		
		importTriggering = true;
		addLog('🔄 Triggering Scryfall import...');
		try {
			const response = await fetch(`${BACKEND_URL}/api/import/scryfall`, {
				method: 'POST'
			});
			if (response.ok) {
				const result = await response.json();
				if (result.success) {
					addLog(`✅ Import completed! Processed ${result.cardsProcessed} cards in ${result.duration.toFixed(2)}s`);
				} else {
					addLog(`❌ Import failed: ${result.message}`);
				}
				await loadImportStatus();
			} else {
				addLog(`❌ Failed to trigger import: ${response.status}`);
			}
		} catch (error: any) {
			console.error('Error triggering import:', error);
			addLog(`❌ Error: ${error.message}`);
		} finally {
			importTriggering = false;
		}
	}

	async function downloadImages() {
		const confirmMsg = forceFullDownload
			? 'Are you sure you want to force download ALL card images? This will re-download all images even if cached.'
			: 'Download new card images that are not yet cached?';
		
		if (!confirm(confirmMsg)) {
			return;
		}
		
		imageDownloading = true;
		addLog(`🖼️  ${forceFullDownload ? 'Force downloading' : 'Downloading new'} card images...`);
		try {
			const response = await fetch(`${BACKEND_URL}/api/import/images`, {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({ forceFullDownload })
			});
			if (response.ok) {
				const result = await response.json();
				if (result.success) {
					addLog(`✅ Images downloaded! Processed ${result.imagesDownloaded} images in ${result.duration.toFixed(2)}s`);
				} else {
					addLog(`❌ Image download failed: ${result.message}`);
				}
			} else {
				addLog(`❌ Failed to download images: ${response.status}`);
			}
		} catch (error: any) {
			console.error('Error downloading images:', error);
			addLog(`❌ Error: ${error.message}`);
		} finally {
			imageDownloading = false;
		}
	}

	async function toggleImportSection() {
		showImportSection = !showImportSection;
		if (showImportSection && !importStatus) {
			await loadImportStatus();
		}
	}

	async function loadItems() {
		loading = true;
		try {
			if (useLocalDB) {
				// Load from PGlite (local database)
				addLog(`📂 Loading ${selectedTable} from PGlite (local)...`);
				items = await loadFromPGlite(selectedTable);
				addLog(`✅ Loaded ${items.length} ${selectedTable} from local DB`);
			} else {
				// Load directly from server
				addLog(`🌐 Loading ${selectedTable} from server...`);
				const schema = tableSchemas[selectedTable];
				const response = await fetch(`${BACKEND_URL}${schema.endpoint}`);
				if (response.ok) {
					items = await response.json();
					addLog(`✅ Loaded ${items.length} ${selectedTable} from server`);
				} else {
					addLog(`❌ Failed to load from server: ${response.status}`);
				}
			}
		} catch (error: any) {
			console.error('Failed to load items:', error);
			addLog(`❌ Error: ${error.message}`);
		} finally {
			loading = false;
		}
	}
	
	async function loadFromPGlite(table: TableName): Promise<any[]> {
		switch (table) {
			case 'decks':
				return await crud.getDecks();
			case 'cards':
				return await crud.getCards();
			case 'hands':
				return await crud.getHands();
			case 'handevals':
				return await crud.getHandEvals();
		}
	}

	async function loadServerView() {
		try {
			addLog('🌐 Fetching server view...');
			const schema = tableSchemas[selectedTable];
			const response = await fetch(`${BACKEND_URL}${schema.endpoint}`);
			if (response.ok) {
				serverItems = await response.json();
				addLog(`✅ Server view loaded (${serverItems.length} items)`);
			}
		} catch (error: any) {
			console.error('Failed to load server items:', error);
			addLog(`❌ Failed to load server view: ${error.message}`);
		}
	}

	function toggleDBSource() {
		useLocalDB = !useLocalDB;
		addLog(`📍 Switched to ${useLocalDB ? 'Local PGlite' : 'Server Direct'} view`);
		loadItems();
	}

	function toggleSync() {
		if ($syncStatus.isEnabled) {
			syncManager.stopSync();
			addLog('⏸️  Sync paused');
		} else {
			syncManager.startSync();
			addLog('▶️  Sync resumed');
		}
	}
	
	async function forceSync() {
		addLog('🔄 Forcing manual sync...');
		await syncManager.performSync();
		await loadItems();
	}

	async function handleCreate() {
		if (!validateForm()) return;
		
		formFeedback = { type: null, message: '' };
		
		try {
			addLog(`➕ Creating ${selectedTable}...`);
			
			// Transform data to match backend DTO
			const transformedData = transformFormData(selectedTable, formData);
			
			// Always create in PGlite (will be synced automatically)
			let created;
			switch (selectedTable) {
				case 'decks':
					created = await crud.createDeck(transformedData);
					break;
				case 'cards':
					created = await crud.createCard(transformedData);
					break;
				case 'hands':
					created = await crud.createHand(transformedData);
					break;
				case 'handevals':
					created = await crud.createHandEval(transformedData);
					break;
			}
			
			addLog(`✅ Created ${selectedTable} (ID: ${created.id})`);
			addLog(`📤 Queued for sync to server`);
			formFeedback = { type: 'success', message: `Successfully created! (ID: ${created.id})` };
			
			// Wait a moment to show the success message
			setTimeout(async () => {
				await loadItems();
				closeForm();
			}, 1500);
		} catch (error: any) {
			console.error('Failed to create:', error);
			addLog(`❌ Error: ${error.message}`);
			formFeedback = { type: 'error', message: `Failed to create: ${error.message}` };
		}
	}

	async function handleUpdate() {
		if (!editingItem || !validateForm()) return;
		
		try {
			addLog(`✏️  Updating ${selectedTable}...`);
			
			// Always update in PGlite (will be synced automatically)
			let updated;
			switch (selectedTable) {
				case 'decks':
					updated = await crud.updateDeck(editingItem.id, formData);
					break;
				case 'cards':
					updated = await crud.updateCard(editingItem.id, formData);
					break;
				case 'hands':
					// No update for hands (immutable)
					throw new Error('Hands cannot be updated');
				case 'handevals':
					// No update for handevals (immutable)
					throw new Error('Hand evaluations cannot be updated');
			}
			
			addLog(`✅ Updated ${selectedTable} (ID: ${editingItem.id})`);
			addLog(`📤 Queued for sync to server`);
			await loadItems();
			closeForm();
		} catch (error: any) {
			console.error('Failed to update:', error);
			addLog(`❌ Error: ${error.message}`);
		}
	}

	async function handleDelete(id: string) {
		if (!confirm('Are you sure you want to delete this item?')) return;
		
		try {
			addLog(`🗑️  Deleting ${selectedTable} (ID: ${id})...`);
			
			// Always delete from PGlite (will be synced automatically)
			switch (selectedTable) {
				case 'decks':
					await crud.deleteDeck(id);
					break;
				case 'cards':
					await crud.deleteCard(id);
					break;
				case 'hands':
					await crud.deleteHand(id);
					break;
				case 'handevals':
					await crud.deleteHandEval(id);
					break;
			}
			
			addLog(`✅ Deleted ${selectedTable} (ID: ${id})`);
			addLog(`📤 Queued for sync to server`);
			await loadItems();
		} catch (error: any) {
			console.error('Failed to delete:', error);
			addLog(`❌ Error: ${error.message}`);
		}
	}

	function validateForm(): boolean {
		const schema = tableSchemas[selectedTable];
		for (const field of schema.fields) {
			if (field.required && !formData[field.name]) {
				alert(`${field.label} is required`);
				return false;
			}
		}
		return true;
	}

	function openCreateForm() {
		editingItem = null;
		formData = {};
		formFeedback = { type: null, message: '' };
		showForm = true;
	}

	function openEditForm(item: any) {
		editingItem = item;
		formData = { ...item };
		formFeedback = { type: null, message: '' };
		showForm = true;
	}

	function closeForm() {
		showForm = false;
		editingItem = null;
		formData = {};
		formFeedback = { type: null, message: '' };
	}

	async function changeTable(table: any) {
		selectedTable = table;
		await loadItems();
	}

	async function syncViews() {
		await loadItems();
		await loadServerView();
	}
</script>

<div class="container mx-auto p-6 max-w-7xl">
	<div class="mb-8">
		<h1 class="text-4xl font-bold text-gray-800 mb-2">Database Admin Panel</h1>
		<p class="text-gray-600">Manage all tables with offline/online sync simulation</p>
	</div>

	<!-- Admin Controls - PGlite Sync Manager -->
	<div class="bg-white rounded-lg shadow-md p-6 mb-6">
		<div class="flex justify-between items-center">
			<div>
				<h2 class="text-xl font-semibold mb-2">🗄️ PGlite Sync Manager</h2>
				<div class="flex items-center space-x-4">
					<!-- Sync Control -->
					<button
						on:click={toggleSync}
						class="px-4 py-2 rounded font-semibold transition border-2 shadow"
						class:bg-green-100={$syncStatus.isEnabled}
						class:text-green-800={$syncStatus.isEnabled}
						class:border-green-300={$syncStatus.isEnabled}
						class:bg-gray-100={!$syncStatus.isEnabled}
						class:text-gray-800={!$syncStatus.isEnabled}
						class:border-gray-300={!$syncStatus.isEnabled}>
						{$syncStatus.isEnabled ? '⏸️  Pause Sync' : '▶️  Resume Sync'}
					</button>
					
					<!-- Force Sync -->
					<button
						on:click={forceSync}
						class="px-4 py-2 bg-blue-100 text-blue-800 rounded hover:bg-blue-200 transition font-semibold border-2 border-blue-300 shadow"
						disabled={!$syncStatus.isEnabled}>
						🔄 Force Sync
					</button>
					
					<!-- DB Source Toggle -->
					<button
						on:click={toggleDBSource}
						class="px-4 py-2 rounded font-semibold transition border-2 shadow"
						class:bg-purple-100={useLocalDB}
						class:text-purple-800={useLocalDB}
						class:border-purple-300={useLocalDB}
						class:bg-orange-100={!useLocalDB}
						class:text-orange-800={!useLocalDB}
						class:border-orange-300={!useLocalDB}>
						{useLocalDB ? '💻 Local PGlite' : '🌐 Server Direct'}
					</button>
					
					<!-- Sync Status Indicators -->
					{#if $syncStatus.isSyncing}
						<span class="inline-block px-3 py-1 rounded-full bg-blue-100 text-blue-800 text-sm font-semibold border border-blue-300">
							🔄 Syncing...
						</span>
					{/if}
					
					{#if $syncStatus.pendingChanges > 0}
						<span class="inline-block px-3 py-1 rounded-full bg-orange-100 text-orange-800 text-sm font-semibold border border-orange-300">
							📦 {$syncStatus.pendingChanges} Pending
						</span>
					{/if}
					
					{#if $syncStatus.lastSync}
						<span class="text-sm text-gray-600">
							Last sync: {new Date($syncStatus.lastSync).toLocaleTimeString()}
						</span>
					{/if}
					
					{#if $syncStatus.error}
						<span class="inline-block px-3 py-1 rounded-full bg-red-100 text-red-800 text-sm font-semibold border border-red-300">
							❌ Error: {$syncStatus.error}
						</span>
					{/if}
				</div>
			</div>

			<div>
				<label class="flex items-center space-x-2 cursor-pointer">
					<span class="text-sm font-medium text-gray-700">Show Server Comparison:</span>
					<input
						type="checkbox"
						bind:checked={isServerView}
						on:change={loadServerView}
						class="w-5 h-5 text-blue-600 rounded focus:ring-2 focus:ring-blue-500"
					/>
				</label>
			</div>
		</div>
	</div>

	<!-- Scryfall Import Management -->
	<div class="bg-white rounded-lg shadow-md p-4 mb-6">
		<button
			on:click={toggleImportSection}
			class="flex items-center justify-between w-full text-left font-semibold text-lg text-gray-800 hover:text-blue-600 transition">
			<span class="flex items-center space-x-2">
				<span>🃏 Scryfall Card Import</span>
				{#if importStatus && importStatus.card_count}
					<span class="text-sm font-normal text-gray-600">
						({importStatus.card_count.toLocaleString()} cards)
					</span>
				{/if}
			</span>
			<span class="text-2xl transform transition-transform" class:rotate-180={showImportSection}>
				▼
			</span>
		</button>

		{#if showImportSection}
			<div class="mt-4 space-y-4 border-t pt-4">
				{#if importLoading}
					<div class="flex items-center justify-center py-8">
						<div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
						<span class="ml-3 text-gray-600">Loading import status...</span>
					</div>
				{:else if importStatus}
					<!-- Status Cards -->
					<div class="grid grid-cols-1 md:grid-cols-3 gap-4">
						<!-- Total Cards -->
						<div class="bg-blue-50 rounded-lg p-4 border-2 border-blue-200">
							<div class="text-sm font-semibold text-blue-600 uppercase mb-1">Total Cards</div>
							<div class="text-3xl font-bold text-blue-900">
								{importStatus.card_count.toLocaleString()}
							</div>
						</div>

						<!-- Last Import -->
						<div class="bg-green-50 rounded-lg p-4 border-2 border-green-200">
							<div class="text-sm font-semibold text-green-600 uppercase mb-1">Last Import</div>
							<div class="text-lg font-semibold text-green-900">
								{#if importStatus.last_import}
									{new Date(importStatus.last_import.imported_at).toLocaleDateString()}
									<div class="text-xs text-green-700 mt-1">
										{new Date(importStatus.last_import.imported_at).toLocaleTimeString()}
									</div>
								{:else}
									<span class="text-gray-500">Never</span>
								{/if}
							</div>
						</div>

						<!-- Data Updated -->
						<div class="bg-purple-50 rounded-lg p-4 border-2 border-purple-200">
							<div class="text-sm font-semibold text-purple-600 uppercase mb-1">Data Updated</div>
							<div class="text-lg font-semibold text-purple-900">
								{#if importStatus.last_import && importStatus.last_import.updated_at}
									{new Date(importStatus.last_import.updated_at).toLocaleDateString()}
								{:else}
									<span class="text-gray-500">N/A</span>
								{/if}
							</div>
						</div>
					</div>

					<!-- Actions -->
					<div class="space-y-4 pt-2">
						<!-- Card Data Import -->
						<div class="flex items-center space-x-4">
							<button
								on:click={triggerImport}
								disabled={importTriggering}
								class="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition font-semibold shadow-lg disabled:opacity-50 disabled:cursor-not-allowed flex items-center space-x-2">
								{#if importTriggering}
									<div class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
									<span>Importing...</span>
								{:else}
									<span>⬇️</span>
									<span>Trigger Full Import</span>
								{/if}
							</button>

							<button
								on:click={loadImportStatus}
								class="px-4 py-3 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition font-semibold border-2 border-gray-300">
								🔄 Refresh Status
							</button>

							<div class="text-sm text-gray-600 italic">
								Note: Full import takes 30-60 minutes and downloads ~160 MB
							</div>
						</div>

						<!-- Image Download -->
						<div class="flex items-center space-x-4 pt-2 border-t border-gray-200">
							<button
								on:click={downloadImages}
								disabled={imageDownloading}
								class="px-6 py-3 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition font-semibold shadow-lg disabled:opacity-50 disabled:cursor-not-allowed flex items-center space-x-2">
								{#if imageDownloading}
									<div class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
									<span>Downloading...</span>
								{:else}
									<span>🖼️</span>
									<span>Get New Images</span>
								{/if}
							</button>

							<label class="flex items-center space-x-2 cursor-pointer">
								<input 
									type="checkbox" 
									bind:checked={forceFullDownload}
									class="w-4 h-4 text-purple-600 border-gray-300 rounded focus:ring-purple-500"
								/>
								<span class="text-sm font-medium text-gray-700">Force Full Download</span>
							</label>

							<div class="text-sm text-gray-600 italic">
								Downloads card images from Scryfall
							</div>
						</div>
					</div>

					<!-- Import History -->
					{#if importStatus.import_history && importStatus.import_history.length > 0}
						<div class="border-t pt-4 mt-4">
							<h4 class="font-semibold text-gray-800 mb-3">📜 Import History</h4>
							<div class="overflow-x-auto">
								<table class="min-w-full divide-y divide-gray-200">
									<thead class="bg-gray-50">
										<tr>
											<th class="px-4 py-2 text-left text-xs font-semibold text-gray-600 uppercase">ID</th>
											<th class="px-4 py-2 text-left text-xs font-semibold text-gray-600 uppercase">Imported At</th>
											<th class="px-4 py-2 text-left text-xs font-semibold text-gray-600 uppercase">Data Updated</th>
											<th class="px-4 py-2 text-left text-xs font-semibold text-gray-600 uppercase">Download URI</th>
										</tr>
									</thead>
									<tbody class="bg-white divide-y divide-gray-200">
										{#each importStatus.import_history as history}
											<tr class="hover:bg-gray-50">
												<td class="px-4 py-2 text-sm text-gray-600 font-mono">
													{history.id.substring(0, 8)}...
												</td>
												<td class="px-4 py-2 text-sm text-gray-900">
													{new Date(history.imported_at).toLocaleString()}
												</td>
												<td class="px-4 py-2 text-sm text-gray-900">
													{history.updated_at ? new Date(history.updated_at).toLocaleString() : 'N/A'}
												</td>
												<td class="px-4 py-2 text-sm text-blue-600 truncate max-w-xs">
													{#if history.download_uri}
														<a href={history.download_uri} target="_blank" class="hover:underline">
															{history.download_uri.split('/').pop()}
														</a>
													{:else}
														N/A
													{/if}
												</td>
											</tr>
										{/each}
									</tbody>
								</table>
							</div>
						</div>
					{/if}
				{:else}
					<div class="text-center py-8 text-gray-500">
						Failed to load import status
					</div>
				{/if}
			</div>
		{/if}
	</div>

	<!-- Table Selector -->
	<div class="bg-white rounded-lg shadow-md p-4 mb-6">
		<div class="flex space-x-2">
			{#each Object.entries(tableSchemas) as [key, schema]}
				<button
					on:click={() => changeTable(key)}
					class="px-6 py-3 rounded-lg font-semibold transition-all"
					class:bg-blue-500={selectedTable === key}
					class:text-white={selectedTable === key}
					class:bg-gray-100={selectedTable !== key}
					class:text-gray-700={selectedTable !== key}
					class:hover:bg-blue-600={selectedTable === key}
					class:hover:bg-gray-200={selectedTable !== key}>
					{schema.name}
				</button>
			{/each}
		</div>
	</div>

	<!-- Main Content Area -->
	<div class="grid grid-cols-1 gap-6" class:lg:grid-cols-2={isServerView}>
		<!-- Client View (or full view if not split) -->
		<div class="bg-white rounded-lg shadow-md p-6">
			<div class="flex justify-between items-center mb-6">
				<h2 class="text-2xl font-semibold">
					{isServerView ? '💻 Client View' : tableSchemas[selectedTable].name}
				</h2>
			<button
				on:click={openCreateForm}
				class="px-4 py-2 bg-green-100 text-green-800 rounded hover:bg-green-200 transition font-semibold shadow-lg border-2 border-green-300"
				style="min-width: 120px; opacity: 1; display: inline-block;"
				title="Add new item (synced via PGlite)">
				✨ Add New
			</button>
			</div>

			{#if loading}
				<div class="text-center py-8">
					<div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
					<p class="mt-2 text-gray-600">Loading...</p>
				</div>
			{:else if items.length === 0}
				<div class="text-center py-12 text-gray-500">
					<p class="text-lg">No items found</p>
					<p class="text-sm mt-2">Click "Add New" to create one</p>
				</div>
			{:else}
				<div class="overflow-x-auto">
					<table class="w-full">
						<thead class="bg-gray-100 border-b-2 border-gray-200">
							<tr>
								<th class="px-4 py-3 text-left text-sm font-semibold text-gray-700">ID</th>
								{#each tableSchemas[selectedTable].fields.slice(0, 3) as field}
									<th class="px-4 py-3 text-left text-sm font-semibold text-gray-700">{field.label}</th>
								{/each}
								<th class="px-4 py-3 text-left text-sm font-semibold text-gray-700">Actions</th>
							</tr>
						</thead>
						<tbody>
							{#each items as item}
								<tr class="border-b border-gray-200 hover:bg-gray-50 transition">
									<td class="px-4 py-3 text-sm text-gray-700">{item.id}</td>
									{#each tableSchemas[selectedTable].fields.slice(0, 3) as field}
										<td class="px-4 py-3 text-sm text-gray-700">
											{#if field.name === 'mana_cost' && item[field.name]}
												<ManaSymbol text={item[field.name]} size="small" inline={true} />
											{:else if field.name === 'oracle_text' && item[field.name]}
												<div class="line-clamp-2">
													<ManaSymbol text={item[field.name]} size="small" inline={true} />
												</div>
											{:else}
												{item[field.name] || '-'}
											{/if}
										</td>
									{/each}
									<td class="px-4 py-3 text-sm">
										<div class="flex space-x-2">
											<button
												on:click={() => openEditForm(item)}
												class="px-3 py-1 bg-yellow-100 text-yellow-800 rounded text-xs hover:bg-yellow-200 transition font-semibold shadow border border-yellow-300"
												style="display: inline-block; opacity: 1;"
												title="Edit item (changes synced via PGlite)">
												✏️ Edit
											</button>
											<button
												on:click={() => handleDelete(item.id)}
												class="px-3 py-1 bg-red-100 text-red-800 rounded text-xs hover:bg-red-200 transition font-semibold shadow border border-red-300"
												style="display: inline-block; opacity: 1;"
												title="Delete item (changes synced via PGlite)">
												🗑️ Delete
											</button>
										</div>
									</td>
								</tr>
							{/each}
						</tbody>
					</table>
				</div>
			{/if}
		</div>

		<!-- Server View (when split view enabled) -->
		{#if isServerView}
			<div class="bg-yellow-50 rounded-lg shadow-md p-6 border-2 border-yellow-300">
				<div class="flex justify-between items-center mb-6">
					<h2 class="text-2xl font-semibold text-yellow-800">🗄️ Server View (Read-Only)</h2>
					<button
						on:click={loadServerView}
						class="px-4 py-2 bg-yellow-100 text-yellow-800 rounded hover:bg-yellow-200 transition font-semibold border border-yellow-300 shadow">
						🔄 Refresh Server
					</button>
				</div>

				{#if serverItems.length === 0}
					<div class="text-center py-12 text-yellow-700">
						<p class="text-lg">No items on server</p>
					</div>
				{:else}
					<div class="overflow-x-auto">
						<table class="w-full">
							<thead class="bg-yellow-100 border-b-2 border-yellow-300">
								<tr>
									<th class="px-4 py-3 text-left text-sm font-semibold text-yellow-800">ID</th>
									{#each tableSchemas[selectedTable].fields.slice(0, 3) as field}
										<th class="px-4 py-3 text-left text-sm font-semibold text-yellow-800">{field.label}</th>
									{/each}
								</tr>
							</thead>
							<tbody>
								{#each serverItems as item}
									<tr class="border-b border-yellow-200">
										<td class="px-4 py-3 text-sm text-yellow-900">{item.id}</td>
									{#each tableSchemas[selectedTable].fields.slice(0, 3) as field}
										<td class="px-4 py-3 text-sm text-yellow-900">
											{#if field.name === 'mana_cost' && item[field.name]}
												<ManaSymbol text={item[field.name]} size="small" inline={true} />
											{:else if field.name === 'oracle_text' && item[field.name]}
												<div class="line-clamp-2">
													<ManaSymbol text={item[field.name]} size="small" inline={true} />
												</div>
											{:else}
												{item[field.name] || '-'}
											{/if}
										</td>
									{/each}
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{/if}
			</div>
		{/if}
	</div>

	<!-- Form Modal -->
	{#if showForm}
		<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4" style="z-index: 1000;">
			<div class="bg-white rounded-lg shadow-xl p-6 max-w-2xl w-full max-h-[90vh] overflow-y-auto">
				<h3 class="text-2xl font-semibold mb-6">
					{editingItem ? '✏️ Edit' : '✨ Create New'} {tableSchemas[selectedTable].name.slice(0, -1)}
				</h3>

				<div class="space-y-4">
					{#each tableSchemas[selectedTable].fields as field}
						<div>
							<label class="block text-sm font-medium text-gray-700 mb-1">
								{field.label}
								{#if field.required}<span class="text-red-500">*</span>{/if}
							</label>

							{#if field.type === 'select' && 'options' in field && field.options}
								<select
									bind:value={formData[field.name]}
									class="w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-blue-500">
									<option value="">Select...</option>
									{#each field.options as option}
										<option value={option}>{option}</option>
									{/each}
								</select>
							{:else if field.type === 'textarea'}
								<textarea
									bind:value={formData[field.name]}
									rows="4"
									class="w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-blue-500"
								></textarea>
							{:else if field.type === 'number'}
								<input
									type="number"
									bind:value={formData[field.name]}
									class="w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-blue-500"
								/>
							{:else}
								<input
									type="text"
									bind:value={formData[field.name]}
									class="w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-blue-500"
								/>
							{/if}
						</div>
					{/each}
				</div>

				<!-- Feedback Message -->
				{#if formFeedback.type}
					<div class="mt-6 p-4 rounded-lg" 
						class:bg-green-50={formFeedback.type === 'success'}
						class:border-green-200={formFeedback.type === 'success'}
						class:text-green-800={formFeedback.type === 'success'}
						class:bg-red-50={formFeedback.type === 'error'}
						class:border-red-200={formFeedback.type === 'error'}
						class:text-red-800={formFeedback.type === 'error'}
						style="border-width: 2px;">
						<div class="flex items-center">
							<span class="text-2xl mr-3">
								{#if formFeedback.type === 'success'}✅{:else}❌{/if}
							</span>
							<span class="font-semibold">{formFeedback.message}</span>
						</div>
					</div>
				{/if}

				<div class="flex justify-end space-x-3 mt-6" style="position: relative; z-index: 10;">
					<button
						type="button"
						on:click={closeForm}
						class="px-6 py-2 bg-gray-200 text-gray-800 rounded hover:bg-gray-300 transition font-semibold shadow border border-gray-400"
						style="min-width: 100px; opacity: 1; display: inline-block;">
						Cancel
					</button>
					<button
						type="button"
						on:click={editingItem ? handleUpdate : handleCreate}
						class="px-6 py-2 bg-green-100 text-green-800 rounded hover:bg-green-200 transition font-semibold shadow-lg border-2 border-green-300"
						style="min-width: 120px; opacity: 1; display: inline-block;"
						title="{editingItem ? 'Update item (synced via PGlite)' : 'Create item (synced via PGlite)'}">
						{editingItem ? '💾 Update' : '✨ Create'}
					</button>
				</div>
			</div>
		</div>
	{/if}

	<!-- Activity Log Section (Always Visible) -->
	<div class="bg-white rounded-lg shadow-md p-6 mt-6">
		<h2 class="text-xl font-semibold mb-4 flex items-center">
			<span
				class="inline-block w-3 h-3 rounded-full mr-2"
				class:bg-green-500={$syncStatus.isEnabled}
				class:bg-red-500={!$syncStatus.isEnabled}
			></span>
			Activity Log
		</h2>

		<div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
			<div class="p-4 bg-gray-50 rounded">
				<p class="text-sm text-gray-600">Sync Status</p>
				<p class="text-lg font-semibold" class:text-green-600={$syncStatus.isEnabled} class:text-gray-600={!$syncStatus.isEnabled}>
					{$syncStatus.isEnabled ? '✅ Enabled' : '⏸️  Paused'}
				</p>
			</div>

			<div class="p-4 bg-gray-50 rounded">
				<p class="text-sm text-gray-600">Syncing</p>
				<p class="text-lg font-semibold">
					{$syncStatus.isSyncing ? '🔄 Yes' : 'No'}
				</p>
			</div>

			<div class="p-4 bg-gray-50 rounded">
				<p class="text-sm text-gray-600">Last Sync</p>
				<p class="text-lg font-semibold">
					{$syncStatus.lastSync
						? new Date($syncStatus.lastSync).toLocaleTimeString()
						: 'Never'}
				</p>
			</div>

			<div class="p-4 bg-gray-50 rounded">
				<p class="text-sm text-gray-600">Pending</p>
				<p class="text-lg font-semibold">{$syncStatus.pendingChanges}</p>
			</div>
		</div>

		{#if $syncStatus.error}
			<div class="mb-4 p-4 bg-red-50 border border-red-200 rounded">
				<p class="text-red-700">
					<strong>Error:</strong>
					{$syncStatus.error}
				</p>
			</div>
		{/if}

		<div class="bg-gray-50 rounded p-4 max-h-64 overflow-y-auto">
			<div class="flex justify-between items-center mb-2">
				<h3 class="text-sm font-semibold text-gray-700">Recent Activity</h3>
				<button
					on:click={() => activityLog = []}
					class="text-xs text-gray-500 hover:text-gray-700 underline">
					Clear Log
				</button>
			</div>
			{#if activityLog.length === 0}
				<p class="text-sm text-gray-500 italic">No activity yet</p>
			{:else}
				<div class="space-y-1">
					{#each activityLog as logEntry}
						<div class="text-xs font-mono text-gray-600 border-l-2 border-gray-300 pl-2">
							{logEntry}
						</div>
					{/each}
				</div>
			{/if}
		</div>
	</div>
</div>

<style>
	button {
		cursor: pointer;
		user-select: none;
	}
	
	button:disabled {
		cursor: not-allowed;
	}

	.line-clamp-2 {
		display: -webkit-box;
		-webkit-line-clamp: 2;
		-webkit-box-orient: vertical;
		overflow: hidden;
	}
</style>

