<script lang="ts">
	import { createEventDispatcher } from 'svelte';
	import type { Hand } from '$lib/api';

	export let hand1: Hand;
	export let hand2: Hand;

	const dispatch = createEventDispatcher();

	let selectedHand: 1 | 2 | null = null;
	let context = '';
	let submitting = false;

	async function submitEvaluation() {
		if (!selectedHand) return;
		
		submitting = true;
		
		try {
			// TODO: Send evaluation to backend
			// This would call POST /api/hands/evaluate with:
			// - hand1Id
			// - hand2Id  
			// - preferredHand
			// - context JSON
			
			await new Promise(resolve => setTimeout(resolve, 500)); // Mock delay
			
			dispatch('evaluation', {
				hand1Id: hand1.id,
				hand2Id: hand2.id,
				preferredHand: selectedHand,
				context: {
					notes: context,
					timestamp: new Date().toISOString()
				}
			});
			
			// Reset form
			selectedHand = null;
			context = '';
		} catch (error) {
			console.error('Evaluation submission failed:', error);
		} finally {
			submitting = false;
		}
	}
</script>

<div class="card">
	<h2 class="text-xl font-semibold mb-6">Which hand would you keep?</h2>
	
	<!-- Hand Selection -->
	<div class="grid md:grid-cols-2 gap-6 mb-6">
		<button
			class="p-4 border-2 rounded-lg transition-colors {selectedHand === 1 ? 'border-blue-500 bg-blue-50' : 'border-gray-200 hover:border-gray-300'}"
			on:click={() => selectedHand = 1}
		>
			<div class="text-center">
				<div class="text-2xl mb-2">🃏</div>
				<div class="font-medium">Hand A</div>
				<div class="text-sm text-gray-600">{hand1.cardIntIds.length} cards</div>
			</div>
		</button>
		
		<button
			class="p-4 border-2 rounded-lg transition-colors {selectedHand === 2 ? 'border-blue-500 bg-blue-50' : 'border-gray-200 hover:border-gray-300'}"
			on:click={() => selectedHand = 2}
		>
			<div class="text-center">
				<div class="text-2xl mb-2">🃏</div>
				<div class="font-medium">Hand B</div>
				<div class="text-sm text-gray-600">{hand2.cardIntIds.length} cards</div>
			</div>
		</button>
	</div>
	
	<!-- Context Input -->
	<div class="mb-6">
		<label for="context" class="block text-sm font-medium text-gray-700 mb-2">
			Additional Notes (optional)
		</label>
		<textarea
			id="context"
			bind:value={context}
			placeholder="e.g., 'Playing against aggro', 'Need early removal', etc."
			class="input h-20"
			rows="3"
		></textarea>
	</div>
	
	<!-- Submit Button -->
	<button
		on:click={submitEvaluation}
		disabled={!selectedHand || submitting}
		class="btn btn-primary w-full"
	>
		{submitting ? 'Submitting...' : 'Submit Choice'}
	</button>
	
	{#if !selectedHand}
		<p class="text-sm text-gray-500 text-center mt-2">
			Select a hand to continue
		</p>
	{/if}
</div>
