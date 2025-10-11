<script lang="ts">
	export let card: any;
	export let size: 'small' | 'medium' | 'large' = 'medium';

	// TODO: Replace with actual card type from backend
	interface Card {
		id: string;
		name: string;
		manaCost: string;
		type: string;
		oracleText: string;
		powerToughness?: string;
		imageUrl?: string;
	}

	// Mock card data for now
	if (!card) {
		card = {
			id: '1',
			name: 'Lightning Bolt',
			manaCost: '{R}',
			type: 'Instant',
			oracleText: 'Lightning Bolt deals 3 damage to any target.',
			imageUrl: 'https://cards.scryfall.com/normal/front/1/1/11e6b0b8-8c5a-4b0a-9e5a-1b2c3d4e5f6a.jpg'
		};
	}

	const sizeClasses = {
		small: 'w-16 h-24',
		medium: 'w-24 h-36',
		large: 'w-32 h-48'
	};
</script>

<div class="card-display {sizeClasses[size]}">
	{#if card.imageUrl}
		<img 
			src={card.imageUrl} 
			alt={card.name}
			class="w-full h-full object-cover rounded-lg shadow-sm"
			loading="lazy"
		/>
	{:else}
		<!-- Fallback card display -->
		<div class="w-full h-full bg-white border-2 border-gray-300 rounded-lg p-2 flex flex-col justify-between">
			<div class="text-xs font-bold text-center">{card.manaCost}</div>
			<div class="text-xs font-medium text-center">{card.name}</div>
			<div class="text-xs text-gray-600 text-center">{card.type}</div>
		</div>
	{/if}
</div>
