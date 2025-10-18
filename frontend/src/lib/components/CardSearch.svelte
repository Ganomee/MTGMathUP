<script lang="ts">
	import { searchCardsWithCache, filterCardsWithCache } from '$lib/electric/card-utils.js';
	import type { Card } from '$lib/electric/types.js';
	import { onMount } from 'svelte';
	import ManaSymbol from './ManaSymbol.svelte';

	export let onCardSelect: (card: Card) => void = () => {};
	export let placeholder = 'Search cards...';
	export let showFilters = true;

	let searchQuery = '';
	let searchResults: Card[] = [];
	let loading = false;
	let showResults = false;

	// Filter options
	let filterType = '';
	let filterColors: string[] = [];
	let filterCmcMin = '';
	let filterCmcMax = '';
	let filterLimit = 50;

	const colorOptions = [
		{ value: 'W', label: 'White', class: 'bg-white border-gray-300' },
		{ value: 'U', label: 'Blue', class: 'bg-blue-500 text-white' },
		{ value: 'B', label: 'Black', class: 'bg-gray-800 text-white' },
		{ value: 'R', label: 'Red', class: 'bg-red-500 text-white' },
		{ value: 'G', label: 'Green', class: 'bg-green-500 text-white' }
	];

	const typeOptions = [
		'Creature', 'Instant', 'Sorcery', 'Artifact', 'Enchantment', 'Planeswalker', 'Land'
	];

	async function performSearch() {
		if (!searchQuery.trim() && !hasActiveFilters()) {
			searchResults = [];
			showResults = false;
			return;
		}

		loading = true;
		try {
			let results: Card[] = [];

			if (searchQuery.trim()) {
				// Text search
				results = await searchCardsWithCache(searchQuery);
			} else {
				// Filter-only search
				const filters = {
					type: filterType || undefined,
					colors: filterColors.length > 0 ? filterColors : undefined,
					cmcMin: filterCmcMin ? parseInt(filterCmcMin) : undefined,
					cmcMax: filterCmcMax ? parseInt(filterCmcMax) : undefined,
					limit: filterLimit
				};
				results = await filterCardsWithCache(filters);
			}

			searchResults = results;
			showResults = true;
		} catch (error) {
			console.error('Search failed:', error);
			searchResults = [];
		} finally {
			loading = false;
		}
	}

	function hasActiveFilters(): boolean {
		return !!(filterType || filterColors.length > 0 || filterCmcMin || filterCmcMax);
	}

	function toggleColor(color: string) {
		const index = filterColors.indexOf(color);
		if (index > -1) {
			filterColors = filterColors.filter(c => c !== color);
		} else {
			filterColors = [...filterColors, color];
		}
	}

	function clearFilters() {
		filterType = '';
		filterColors = [];
		filterCmcMin = '';
		filterCmcMax = '';
	}

	function selectCard(card: Card) {
		onCardSelect(card);
		showResults = false;
		searchQuery = '';
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

	// Debounced search
	let searchTimeout: ReturnType<typeof setTimeout>;
	function handleSearchInput() {
		clearTimeout(searchTimeout);
		searchTimeout = setTimeout(performSearch, 300);
	}

	// Watch for filter changes
	$: if (hasActiveFilters()) {
		handleSearchInput();
	}
</script>

<div class="relative w-full">
	<!-- Search Input -->
	<div class="relative">
		<input
			type="text"
			bind:value={searchQuery}
			on:input={handleSearchInput}
			placeholder={placeholder}
			class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
		/>
		{#if loading}
			<div class="absolute right-3 top-1/2 transform -translate-y-1/2">
				<div class="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-500"></div>
			</div>
		{/if}
	</div>

	<!-- Filters -->
	{#if showFilters}
		<div class="mt-4 p-4 bg-gray-50 rounded-lg">
			<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
				<!-- Type Filter -->
				<div>
					<label class="block text-sm font-medium text-gray-700 mb-1">Type</label>
					<select bind:value={filterType} on:change={handleSearchInput} class="w-full px-3 py-2 border border-gray-300 rounded-md">
						<option value="">Any Type</option>
						{#each typeOptions as type}
							<option value={type}>{type}</option>
						{/each}
					</select>
				</div>

				<!-- CMC Filter -->
				<div>
					<label class="block text-sm font-medium text-gray-700 mb-1">CMC</label>
					<div class="flex space-x-2">
						<input
							type="number"
							bind:value={filterCmcMin}
							on:input={handleSearchInput}
							placeholder="Min"
							min="0"
							class="w-full px-2 py-1 border border-gray-300 rounded text-sm"
						/>
						<input
							type="number"
							bind:value={filterCmcMax}
							on:input={handleSearchInput}
							placeholder="Max"
							min="0"
							class="w-full px-2 py-1 border border-gray-300 rounded text-sm"
						/>
					</div>
				</div>

				<!-- Limit -->
				<div>
					<label class="block text-sm font-medium text-gray-700 mb-1">Limit</label>
					<select bind:value={filterLimit} on:change={handleSearchInput} class="w-full px-3 py-2 border border-gray-300 rounded-md">
						<option value={25}>25</option>
						<option value={50}>50</option>
						<option value={100}>100</option>
					</select>
				</div>

				<!-- Clear Filters -->
				<div class="flex items-end">
					<button
						on:click={clearFilters}
						class="w-full px-3 py-2 bg-gray-200 text-gray-700 rounded-md hover:bg-gray-300 transition-colors"
					>
						Clear Filters
					</button>
				</div>
			</div>

			<!-- Color Filters -->
			<div class="mt-4">
				<label class="block text-sm font-medium text-gray-700 mb-2">Colors</label>
				<div class="flex flex-wrap gap-2">
					{#each colorOptions as color}
						<button
							on:click={() => toggleColor(color.value)}
							class="px-3 py-1 rounded-full text-sm border transition-colors {color.class} {filterColors.includes(color.value) ? 'ring-2 ring-blue-500' : ''}"
						>
							{color.label}
						</button>
					{/each}
				</div>
			</div>
		</div>
	{/if}

	<!-- Search Results -->
	{#if showResults && searchResults.length > 0}
		<div class="absolute top-full left-0 right-0 mt-1 bg-white border border-gray-300 rounded-lg shadow-lg z-50 max-h-96 overflow-y-auto">
			{#each searchResults as card}
				<button
					on:click={() => selectCard(card)}
					class="w-full px-4 py-3 text-left hover:bg-gray-50 border-b border-gray-100 last:border-b-0 transition-colors"
				>
					<div class="flex items-center justify-between">
						<div class="flex-1">
							<div class="font-medium text-gray-900">{card.name}</div>
							<div class="text-sm text-gray-600">{card.type}</div>
							{#if card.oracle_text}
								<div class="text-xs text-gray-500 mt-1 line-clamp-2">
									<ManaSymbol text={card.oracle_text} size="small" inline={true} />
								</div>
							{/if}
						</div>
						<div class="flex items-center space-x-2 ml-4">
							{#if card.mana_cost}
								<div class="bg-gray-100 px-2 py-1 rounded">
									<ManaSymbol text={card.mana_cost} size="small" inline={true} />
								</div>
							{/if}
							<div class="w-6 h-6 rounded-full border {getCardColorClass(card.colors)} flex items-center justify-center text-xs font-bold">
								{card.cmc}
							</div>
						</div>
					</div>
				</button>
			{/each}
		</div>
	{:else if showResults && searchResults.length === 0 && !loading}
		<div class="absolute top-full left-0 right-0 mt-1 bg-white border border-gray-300 rounded-lg shadow-lg z-50 p-4 text-center text-gray-500">
			No cards found
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
</style>
