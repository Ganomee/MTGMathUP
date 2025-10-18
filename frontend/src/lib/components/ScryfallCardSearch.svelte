<script lang="ts">
	import { Cards } from 'scryfall-api';
	import type { Card } from '@scryfall/api-types';
	import ManaSymbol from './ManaSymbol.svelte';

	export let onCardSelect: (card: Card) => void = () => {};
	export let placeholder = 'Search cards using Scryfall syntax...';
	export let maxResults = 50;

	let searchQuery = '';
	let searchResults: Card[] = [];
	let loading = false;
	let error = '';
	let showResults = false;

	// Debounce search
	let searchTimeout: ReturnType<typeof setTimeout>;
	const DEBOUNCE_MS = 500;

	async function performSearch() {
		const query = searchQuery.trim();
		
		if (!query) {
			searchResults = [];
			showResults = false;
			error = '';
			return;
		}

		loading = true;
		error = '';

		try {
			// Use scryfall-api to search cards
			const results = await Cards.search(query, {
				unique: 'cards',
				order: 'name'
			});

			// Convert iterator/array to array and limit results
			const cardsArray = Array.isArray(results) ? results : await results.all();
			searchResults = cardsArray.slice(0, maxResults);
			showResults = true;
		} catch (err: any) {
			console.error('Search error:', err);
			error = err.message || 'Failed to search cards';
			searchResults = [];
			showResults = false;
		} finally {
			loading = false;
		}
	}

	function handleInput() {
		clearTimeout(searchTimeout);
		
		if (!searchQuery.trim()) {
			searchResults = [];
			showResults = false;
			error = '';
			loading = false;
			return;
		}

		loading = true;
		searchTimeout = setTimeout(() => {
			performSearch();
		}, DEBOUNCE_MS);
	}

	function selectCard(card: Card) {
		onCardSelect(card);
		searchQuery = '';
		searchResults = [];
		showResults = false;
	}

	function getCardColorClass(colors?: string[]): string {
		if (!colors || colors.length === 0) return 'text-gray-600';
		if (colors.length === 1) {
			switch (colors[0]) {
				case 'W': return 'text-yellow-600';
				case 'U': return 'text-blue-600';
				case 'B': return 'text-gray-900';
				case 'R': return 'text-red-600';
				case 'G': return 'text-green-600';
				default: return 'text-gray-600';
			}
		}
		return 'text-purple-600'; // Multicolor
	}
</script>

<div class="relative w-full">
	<!-- Search Input -->
	<div class="relative">
		<input
			type="text"
			bind:value={searchQuery}
			on:input={handleInput}
			placeholder={placeholder}
			class="w-full px-4 py-2 pl-10 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
			aria-label="Search cards"
		/>
		
		<!-- Search Icon -->
		<div class="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400">
			🔍
		</div>

		<!-- Loading/Clear Button -->
		<div class="absolute right-3 top-1/2 transform -translate-y-1/2">
			{#if loading}
				<div class="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-500" title="Searching..."></div>
			{:else if searchQuery}
				<button
					on:click={() => { searchQuery = ''; handleInput(); }}
					class="text-gray-400 hover:text-gray-600"
					aria-label="Clear search"
				>
					✕
				</button>
			{/if}
		</div>
	</div>

	<!-- Help Text -->
	<div class="mt-1 text-xs text-gray-500">
		Examples: <code class="bg-gray-100 px-1 rounded">t:instant c:red</code>, 
		<code class="bg-gray-100 px-1 rounded">cmc&lt;=3</code>, 
		<code class="bg-gray-100 px-1 rounded">o:"draw a card"</code>
	</div>

	<!-- Error Message -->
	{#if error}
		<div class="absolute top-full left-0 right-0 mt-1 bg-red-50 border border-red-200 rounded-lg p-3 z-50 shadow-lg">
			<p class="text-sm text-red-700">
				<strong>Error:</strong> {error}
			</p>
		</div>
	{/if}

	<!-- Search Results -->
	{#if showResults && searchResults.length > 0}
		<div class="absolute top-full left-0 right-0 mt-1 bg-white border border-gray-300 rounded-lg shadow-lg z-50 max-h-96 overflow-y-auto">
			<div class="p-2">
				<div class="text-xs text-gray-500 mb-2 px-2">
					Found {searchResults.length} card{searchResults.length !== 1 ? 's' : ''}
					{#if searchResults.length >= maxResults}
						(showing first {maxResults})
					{/if}
				</div>
				
				{#each searchResults as card (card.id)}
					<button
						on:click={() => selectCard(card)}
						class="w-full px-3 py-2 text-left hover:bg-blue-50 rounded transition-colors border-b border-gray-100 last:border-b-0"
					>
						<div class="flex items-start justify-between gap-3">
							<!-- Card Info -->
							<div class="flex-1 min-w-0">
								<div class="font-medium text-gray-900 truncate {getCardColorClass(card.colors)}">
									{card.name}
								</div>
								<div class="text-xs text-gray-600 mt-0.5">
									{card.type_line}
								</div>
								{#if card.oracle_text}
									<div class="text-xs text-gray-500 mt-1 line-clamp-2">
										<ManaSymbol text={card.oracle_text} size="small" inline={true} />
									</div>
								{/if}
							</div>

							<!-- Mana Cost & CMC -->
							<div class="flex flex-col items-end gap-1 flex-shrink-0">
								{#if card.mana_cost}
									<div class="flex items-center">
										<ManaSymbol text={card.mana_cost} size="small" inline={true} />
									</div>
								{/if}
								{#if card.cmc !== undefined}
									<div class="text-xs text-gray-500">
										CMC: {card.cmc}
									</div>
								{/if}
							</div>
						</div>
					</button>
				{/each}
			</div>
		</div>
	{:else if showResults && searchResults.length === 0 && !loading && !error}
		<div class="absolute top-full left-0 right-0 mt-1 bg-white border border-gray-300 rounded-lg shadow-lg z-50 p-4 text-center text-gray-500">
			No cards found for "{searchQuery}"
		</div>
	{/if}
</div>

<style>
	.line-clamp-2 {
		display: -webkit-box;
		-webkit-line-clamp: 2;
		-webkit-box-orient: vertical;
		overflow: hidden;
	}

	code {
		font-family: 'Courier New', monospace;
	}
</style>

