<script lang="ts">
	import type { Hand } from '$lib/api';
	import { getCardNames, getCardType, getCardColor } from '$lib/electric/card-utils.js';
	import { onMount } from 'svelte';

	export let hand: Hand;

	let cardNames: Record<number, string> = {};
	let cardTypes: Record<number, string> = {};
	let cardColors: Record<number, string[]> = {};
	let loading = true;

	onMount(async () => {
		try {
			// Load card names for all cards in the hand
			cardNames = await getCardNames(hand.cardIntIds);
			
			// Load card types and colors
			for (const cardId of hand.cardIntIds) {
				cardTypes[cardId] = await getCardType(cardId);
				cardColors[cardId] = await getCardColor(cardId);
			}
		} catch (error) {
			console.error('Failed to load card data:', error);
		} finally {
			loading = false;
		}
	});

	function getCardName(cardId: number): string {
		return cardNames[cardId] || `Card ${cardId}`;
	}

	function getCardTypeById(cardId: number): string {
		return cardTypes[cardId] || 'Unknown';
	}

	function getCardColorById(cardId: number): string[] {
		return cardColors[cardId] || [];
	}

	function getCardColorClass(cardId: number): string {
		const colors = getCardColorById(cardId);
		if (colors.length === 0) return 'bg-gray-50';
		if (colors.length === 1) {
			switch (colors[0]) {
				case 'R': return 'bg-red-50 border-red-200';
				case 'U': return 'bg-blue-50 border-blue-200';
				case 'G': return 'bg-green-50 border-green-200';
				case 'W': return 'bg-white border-gray-200';
				case 'B': return 'bg-gray-800 text-white border-gray-600';
				default: return 'bg-gray-50';
			}
		}
		return 'bg-gradient-to-r from-yellow-50 to-purple-50 border-yellow-200';
	}
</script>

<div class="space-y-4">
	<div class="flex items-center justify-between">
		<h3 class="text-lg font-medium">Hand #{hand.id}</h3>
		<span class="text-sm text-gray-500">{hand.size} cards</span>
	</div>
	
	{#if loading}
		<div class="grid grid-cols-2 gap-2">
			{#each hand.cardIntIds as cardId}
				<div class="px-3 py-2 bg-gray-100 rounded-lg text-sm animate-pulse">
					Loading...
				</div>
			{/each}
		</div>
	{:else}
		<div class="grid grid-cols-2 gap-2">
			{#each hand.cardIntIds as cardId}
				<div class="px-3 py-2 rounded-lg text-sm font-medium border {getCardColorClass(cardId)}">
					<div class="font-semibold">{getCardName(cardId)}</div>
					<div class="text-xs opacity-75">{getCardTypeById(cardId)}</div>
				</div>
			{/each}
		</div>
	{/if}
	
	<!-- Hand Analysis -->
	{#if !loading}
		<div class="mt-4 p-3 bg-gray-50 rounded-lg">
			<div class="text-sm text-gray-600">
				<div class="flex justify-between">
					<span>Lands:</span>
					<span>{hand.cardIntIds.filter(cardId => getCardTypeById(cardId).includes('Land')).length}</span>
				</div>
				<div class="flex justify-between">
					<span>Spells:</span>
					<span>{hand.cardIntIds.filter(cardId => !getCardTypeById(cardId).includes('Land')).length}</span>
				</div>
				<div class="flex justify-between">
					<span>Creatures:</span>
					<span>{hand.cardIntIds.filter(cardId => getCardTypeById(cardId).includes('Creature')).length}</span>
				</div>
				<div class="flex justify-between">
					<span>Hash:</span>
					<span class="font-mono text-xs">{hand.hash64.toString(16)}</span>
				</div>
			</div>
		</div>
	{/if}
</div>
