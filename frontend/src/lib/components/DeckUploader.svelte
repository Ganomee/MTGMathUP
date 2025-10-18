<script lang="ts">
	import { createEventDispatcher } from 'svelte';
	import { parseDeckList, validateDeckCards } from '$lib/electric/card-utils.js';
	import { createDeck } from '$lib/electric/store.js';
	import type { Card } from '$lib/electric/types.js';

	const dispatch = createEventDispatcher();

	let deckText = '';
	let deckUrl = '';
	let uploadMethod: 'manual' | 'url' | 'file' = 'manual';
	let loading = false;
	let validationResults: {
		valid: Array<{ name: string; card: Card; count: number }>;
		invalid: string[];
	} | null = null;
	let deckName = '';

	async function handleSubmit() {
		if (!deckText && !deckUrl) return;
		
		loading = true;
		validationResults = null;
		
		try {
			// Parse deck list
			const deckList = parseDeckList(deckText);
			const cardNames = deckList.map(item => item.name);
			
			// Validate cards against local database
			const validation = await validateDeckCards(cardNames);
			
			// Combine validation results with counts
			const validCards = deckList
				.filter(item => validation.valid.some(v => v.name === item.name))
				.map(item => ({
					...item,
					card: validation.valid.find(v => v.name === item.name)!.card
				}));
			
			const invalidCards = deckList
				.filter(item => validation.invalid.includes(item.name))
				.map(item => item.name);
			
			validationResults = {
				valid: validCards,
				invalid: invalidCards
			};
			
			// If there are valid cards, create the deck
			if (validCards.length > 0) {
				const deckId = await createDeck({
					name: deckName || 'Imported Deck',
					format: 'Unknown',
					owner: undefined
				});
				
				dispatch('deckImported', {
					id: deckId,
					name: deckName || 'Imported Deck',
					cardCount: validCards.length,
					validationResults
				});
				
				// Reset form
				deckText = '';
				deckUrl = '';
				deckName = '';
				validationResults = null;
			}
		} catch (error) {
			console.error('Deck import failed:', error);
		} finally {
			loading = false;
		}
	}

	function getCardColorClass(colors: string[]): string {
		if (colors.length === 0) return 'bg-gray-100';
		if (colors.length === 1) {
			switch (colors[0]) {
				case 'W': return 'bg-white border border-gray-300';
				case 'U': return 'bg-blue-500 text-white';
				case 'B': return 'bg-gray-800 text-white';
				case 'R': return 'bg-red-500 text-white';
				case 'G': return 'bg-green-500 text-white';
				default: return 'bg-gray-100';
			}
		}
		return 'bg-gradient-to-r from-yellow-400 to-purple-500 text-white';
	}

	function handleFileUpload(event: Event) {
		const target = event.target as HTMLInputElement;
		const file = target.files?.[0];
		
		if (file) {
			const reader = new FileReader();
			reader.onload = (e) => {
				deckText = e.target?.result as string;
			};
			reader.readAsText(file);
		}
	}
</script>

<div class="space-y-6">
	<!-- Upload Method Selection -->
	<div class="flex space-x-4">
		<button 
			class="btn {uploadMethod === 'manual' ? 'btn-primary' : 'btn-secondary'}"
			on:click={() => uploadMethod = 'manual'}
		>
			Manual Entry
		</button>
		<button 
			class="btn {uploadMethod === 'url' ? 'btn-primary' : 'btn-secondary'}"
			on:click={() => uploadMethod = 'url'}
		>
			Import URL
		</button>
		<button 
			class="btn {uploadMethod === 'file' ? 'btn-primary' : 'btn-secondary'}"
			on:click={() => uploadMethod = 'file'}
		>
			Upload File
		</button>
	</div>

	<!-- Deck Name -->
	<div>
		<label for="deckName" class="block text-sm font-medium text-gray-700 mb-2">
			Deck Name
		</label>
		<input
			id="deckName"
			bind:value={deckName}
			placeholder="My Awesome Deck"
			class="input"
		/>
	</div>

	<!-- Manual Entry -->
	{#if uploadMethod === 'manual'}
		<div>
			<label for="deckText" class="block text-sm font-medium text-gray-700 mb-2">
				Deck List
			</label>
			<textarea
				id="deckText"
				bind:value={deckText}
				placeholder="4 Lightning Bolt&#10;4 Counterspell&#10;2 Brainstorm&#10;..."
				class="input h-32"
				rows="8"
			></textarea>
			<p class="text-sm text-gray-500 mt-1">
				Enter cards in the format: "4 Card Name" (one per line)
			</p>
		</div>
	{/if}

	<!-- URL Import -->
	{#if uploadMethod === 'url'}
		<div>
			<label for="deckUrl" class="block text-sm font-medium text-gray-700 mb-2">
				Deck URL
			</label>
			<input
				id="deckUrl"
				bind:value={deckUrl}
				type="url"
				placeholder="https://cubecobra.com/cube/list/..."
				class="input"
			/>
			<p class="text-sm text-gray-500 mt-1">
				Paste a CubeCobra or Moxfield deck URL
			</p>
		</div>
	{/if}

	<!-- File Upload -->
	{#if uploadMethod === 'file'}
		<div>
			<label for="deckFile" class="block text-sm font-medium text-gray-700 mb-2">
				Deck File
			</label>
			<input
				id="deckFile"
				type="file"
				accept=".txt,.json"
				on:change={handleFileUpload}
				class="input"
			/>
			<p class="text-sm text-gray-500 mt-1">
				Upload a .txt or .json deck file
			</p>
		</div>
	{/if}

	<!-- Submit Button -->
	<button
		on:click={handleSubmit}
		disabled={loading || (!deckText && !deckUrl)}
		class="btn btn-primary w-full"
	>
		{loading ? 'Importing...' : 'Import Deck'}
	</button>

	<!-- Validation Results -->
	{#if validationResults}
		<div class="mt-6 space-y-4">
			<!-- Valid Cards -->
			{#if validationResults.valid.length > 0}
				<div>
					<h3 class="text-lg font-semibold text-green-700 mb-3">
						Valid Cards ({validationResults.valid.length})
					</h3>
					<div class="grid grid-cols-1 md:grid-cols-2 gap-2">
						{#each validationResults.valid as item}
							<div class="flex items-center justify-between p-3 bg-green-50 border border-green-200 rounded-lg">
								<div class="flex items-center space-x-3">
									<div class="w-6 h-6 rounded-full border {getCardColorClass(item.card.colors)} flex items-center justify-center text-xs font-bold">
										{item.card.cmc}
									</div>
									<div>
										<div class="font-medium text-green-800">{item.name}</div>
										<div class="text-sm text-green-600">{item.card.type}</div>
									</div>
								</div>
								<span class="text-sm font-bold text-green-700">x{item.count}</span>
							</div>
						{/each}
					</div>
				</div>
			{/if}

			<!-- Invalid Cards -->
			{#if validationResults.invalid.length > 0}
				<div>
					<h3 class="text-lg font-semibold text-red-700 mb-3">
						Invalid Cards ({validationResults.invalid.length})
					</h3>
					<div class="p-4 bg-red-50 border border-red-200 rounded-lg">
						<p class="text-red-700 mb-2">The following cards were not found in the database:</p>
						<div class="flex flex-wrap gap-2">
							{#each validationResults.invalid as cardName}
								<span class="px-2 py-1 bg-red-100 text-red-800 rounded text-sm">
									{cardName}
								</span>
							{/each}
						</div>
						<p class="text-sm text-red-600 mt-2">
							These cards may need to be imported from Scryfall first.
						</p>
					</div>
				</div>
			{/if}
		</div>
	{/if}
</div>
