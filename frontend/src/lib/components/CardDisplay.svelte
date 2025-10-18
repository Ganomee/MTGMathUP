<script lang="ts">
	import type { Card } from '$lib/api';
	import ManaSymbol from './ManaSymbol.svelte';

	export let card: Card | null = null;
	export let size: 'small' | 'medium' | 'large' = 'medium';

	// Mock card data for now
	if (!card) {
		card = {
			id: '1',
			name: 'Lightning Bolt',
			mana_cost: '{R}',
			type: 'Instant',
			oracle_text: 'Lightning Bolt deals 3 damage to any target.',
			cmc: 1,
			colors: ['R'],
			created_at: new Date().toISOString(),
			updated_at: new Date().toISOString()
		};
	}

	const sizeClasses = {
		small: 'w-16 h-24',
		medium: 'w-24 h-36',
		large: 'w-32 h-48'
	};
</script>

<div class="card-display {sizeClasses[size]}">
	<!-- Fallback card display -->
	<div class="w-full h-full bg-white border-2 border-gray-300 rounded-lg p-2 flex flex-col justify-between">
		<div class="text-xs font-bold text-center">
			<ManaSymbol text={card.mana_cost || ''} size="small" inline={true} />
		</div>
		<div class="text-xs font-medium text-center">{card.name}</div>
		<div class="text-xs text-gray-600 text-center">{card.type}</div>
	</div>
</div>
