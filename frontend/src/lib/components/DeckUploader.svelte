<script lang="ts">
	import { createEventDispatcher } from 'svelte';

	const dispatch = createEventDispatcher();

	let deckText = '';
	let deckUrl = '';
	let uploadMethod: 'manual' | 'url' | 'file' = 'manual';
	let loading = false;

	async function handleSubmit() {
		if (!deckText && !deckUrl) return;
		
		loading = true;
		
		try {
			// TODO: Implement deck import logic
			// This would call the backend API to:
			// 1. Parse the deck list
			// 2. Validate cards against Scryfall
			// 3. Create deck in database
			// 4. Return deck ID
			
			await new Promise(resolve => setTimeout(resolve, 1000)); // Mock delay
			
			const mockDeck = {
				id: 1,
				name: 'Imported Deck',
				cardCount: deckText.split('\n').length
			};
			
			dispatch('deckImported', mockDeck);
			
			// Reset form
			deckText = '';
			deckUrl = '';
		} catch (error) {
			console.error('Deck import failed:', error);
		} finally {
			loading = false;
		}
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
</div>
