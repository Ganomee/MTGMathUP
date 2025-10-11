<script lang="ts">
	import { onMount } from 'svelte';
	import { apiClient, type Hand, type HandEvaluationRequest } from '$lib/api/client';
	import HandCompare from '$lib/components/HandCompare.svelte';
	import HandViewer from '$lib/components/HandViewer.svelte';

	let deckId: number = 1; // Default to first deck
	let hand1: Hand | null = null;
	let hand2: Hand | null = null;
	let loading = false;
	let error: string | null = null;
	let evaluationCount = 0;

	onMount(() => {
		generateNewHands();
	});

	async function generateNewHands() {
		if (!deckId) return;
		
		loading = true;
		error = null;
		
		try {
			// Generate two random hands from the same deck
			const [newHand1, newHand2] = await Promise.all([
				apiClient.generateRandomHand(deckId, 7),
				apiClient.generateRandomHand(deckId, 7)
			]);
			
			hand1 = newHand1;
			hand2 = newHand2;
		} catch (err) {
			error = err instanceof Error ? err.message : 'Failed to generate hands';
			console.error('Error generating hands:', err);
		} finally {
			loading = false;
		}
	}

	async function handleHandEvaluation(event: CustomEvent) {
		if (!hand1 || !hand2) return;
		
		try {
			const evaluation: HandEvaluationRequest = {
				hand1Id: hand1.id,
				hand2Id: hand2.id,
				preferredHand: event.detail.preferredHand,
				context: {
					notes: event.detail.context?.notes || '',
					timestamp: new Date().toISOString(),
					evaluationNumber: evaluationCount + 1
				}
			};
			
			// Send evaluation to backend
			await apiClient.evaluateHands(evaluation);
			evaluationCount++;
			
			console.log('Evaluation submitted:', evaluation);
			
			// Generate new hands for next comparison
			await generateNewHands();
		} catch (err) {
			error = err instanceof Error ? err.message : 'Failed to submit evaluation';
			console.error('Error submitting evaluation:', err);
		}
	}

	function handleDeckChange(event: Event) {
		const target = event.target as HTMLSelectElement;
		deckId = parseInt(target.value);
		generateNewHands();
	}
</script>

<svelte:head>
	<title>Compare Hands - MTG Mullagain</title>
</svelte:head>

<div class="max-w-6xl mx-auto">
	<h1 class="text-3xl font-bold text-gray-900 mb-8">Compare Hands</h1>
	
	<!-- Deck Selection -->
	<div class="mb-6">
		<label for="deckSelect" class="block text-sm font-medium text-gray-700 mb-2">
			Select Deck
		</label>
		<select 
			id="deckSelect" 
			bind:value={deckId} 
			on:change={handleDeckChange}
			class="input max-w-xs"
		>
			<option value="1">Test Deck (Legacy)</option>
			<option value="2">Modern Burn</option>
			<option value="3">Standard Control</option>
		</select>
	</div>

	<!-- Evaluation Counter -->
	<div class="mb-6 text-sm text-gray-600">
		Evaluations completed: <span class="font-medium">{evaluationCount}</span>
	</div>

	<!-- Error Display -->
	{#if error}
		<div class="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
			<p class="text-red-800">{error}</p>
		</div>
	{/if}

	<!-- Loading State -->
	{#if loading}
		<div class="card text-center py-12">
			<div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
			<p class="text-gray-600">Generating hands...</p>
		</div>
	{:else if hand1 && hand2}
		<div class="grid lg:grid-cols-2 gap-8">
			<div class="card">
				<h2 class="text-xl font-semibold mb-4">Hand A</h2>
				<HandViewer hand={hand1} />
			</div>
			
			<div class="card">
				<h2 class="text-xl font-semibold mb-4">Hand B</h2>
				<HandViewer hand={hand2} />
			</div>
		</div>
		
		<div class="mt-8">
			<HandCompare 
				{hand1} 
				{hand2} 
				on:evaluation={handleHandEvaluation} 
			/>
		</div>
	{:else}
		<div class="card text-center py-12">
			<h2 class="text-xl font-semibold mb-4">No Hands Available</h2>
			<p class="text-gray-600 mb-6">
				Unable to generate hands. Please check your deck selection.
			</p>
			<button on:click={generateNewHands} class="btn btn-primary">
				Try Again
			</button>
		</div>
	{/if}
</div>
