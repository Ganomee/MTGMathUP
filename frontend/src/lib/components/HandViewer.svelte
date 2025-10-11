<script lang="ts">
	import type { Hand } from '$lib/api/client';

	export let hand: Hand;

	// Mock card names for display (in real app, this would come from card lookup)
	const cardNames: Record<number, string> = {
		1: 'Lightning Bolt',
		2: 'Counterspell', 
		3: 'Brainstorm',
		4: 'Ponder',
		5: 'Island',
		6: 'Mountain',
		7: 'Volcanic Island',
		8: 'Force of Will',
		9: 'Delver of Secrets',
		10: 'Snapcaster Mage'
	};

	function getCardName(cardId: number): string {
		return cardNames[cardId] || `Card ${cardId}`;
	}

	function getCardType(cardId: number): string {
		// Simple type detection based on card ID
		if (cardId <= 4) return 'Instant';
		if (cardId <= 7) return 'Land';
		return 'Creature';
	}

	function getCardColor(cardId: number): string {
		// Simple color detection
		if (cardId === 1 || cardId === 6 || cardId === 7) return 'red';
		if (cardId === 2 || cardId === 3 || cardId === 4 || cardId === 5) return 'blue';
		return 'gray';
	}
</script>

<div class="space-y-4">
	<div class="flex items-center justify-between">
		<h3 class="text-lg font-medium">Hand #{hand.id}</h3>
		<span class="text-sm text-gray-500">{hand.size} cards</span>
	</div>
	
	<div class="grid grid-cols-2 gap-2">
		{#each hand.cardIntIds as cardId}
			<div class="px-3 py-2 bg-blue-50 rounded-lg text-sm font-medium">
				{getCardName(cardId)}
			</div>
		{/each}
	</div>
	
	<!-- Hand Analysis -->
	<div class="mt-4 p-3 bg-gray-50 rounded-lg">
		<div class="text-sm text-gray-600">
			<div class="flex justify-between">
				<span>Lands:</span>
				<span>{hand.cardIntIds.filter(cardId => getCardType(cardId) === 'Land').length}</span>
			</div>
			<div class="flex justify-between">
				<span>Spells:</span>
				<span>{hand.cardIntIds.filter(cardId => getCardType(cardId) !== 'Land').length}</span>
			</div>
			<div class="flex justify-between">
				<span>Hash:</span>
				<span class="font-mono text-xs">{hand.hash64.toString(16)}</span>
			</div>
		</div>
	</div>
</div>
