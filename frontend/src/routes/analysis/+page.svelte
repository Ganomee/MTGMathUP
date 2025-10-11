<script lang="ts">
	import { onMount } from 'svelte';
	import { apiClient, type Hand } from '$lib/api/client';
	import CardFrequencyChart from '$lib/components/charts/CardFrequencyChart.svelte';
	import HandSizeChart from '$lib/components/charts/HandSizeChart.svelte';
	import EvaluationTrendChart from '$lib/components/charts/EvaluationTrendChart.svelte';

	let analysisData: any = null;
	let loading = true;
	let error: string | null = null;

	// Mock data for charts (in real app, this would come from backend analysis)
	let cardFrequencyData = [
		{ name: 'Lightning Bolt', count: 15, color: 'red' },
		{ name: 'Counterspell', count: 12, color: 'blue' },
		{ name: 'Brainstorm', count: 10, color: 'blue' },
		{ name: 'Ponder', count: 8, color: 'blue' },
		{ name: 'Island', count: 20, color: 'blue' },
		{ name: 'Mountain', count: 18, color: 'red' },
		{ name: 'Force of Will', count: 6, color: 'blue' },
		{ name: 'Delver of Secrets', count: 4, color: 'blue' }
	];

	let handSizeData = [
		{ name: '5 cards', value: 8 },
		{ name: '6 cards', value: 15 },
		{ name: '7 cards', value: 25 },
		{ name: '8 cards', value: 12 },
		{ name: '9 cards', value: 5 }
	];

	let evaluationTrendData = [
		{ date: '2024-01-01', evaluations: 5, cumulative: 5 },
		{ date: '2024-01-02', evaluations: 8, cumulative: 13 },
		{ date: '2024-01-03', evaluations: 12, cumulative: 25 },
		{ date: '2024-01-04', evaluations: 6, cumulative: 31 },
		{ date: '2024-01-05', evaluations: 15, cumulative: 46 },
		{ date: '2024-01-06', evaluations: 9, cumulative: 55 },
		{ date: '2024-01-07', evaluations: 11, cumulative: 66 }
	];

	onMount(async () => {
		try {
			// TODO: Fetch analysis data from backend
			// This would include:
			// - Hand evaluation statistics
			// - Most/least preferred hands
			// - Card frequency analysis
			// - Hand size distribution
			// - Evaluation trends over time
			
			// For now, simulate API call
			await new Promise(resolve => setTimeout(resolve, 1000));
			
			analysisData = {
				totalComparisons: 66,
				averageHandScore: 7.2,
				mostPreferredHand: {
					id: 1,
					cardIntIds: [1, 2, 3, 4, 5, 6, 7],
					size: 7,
					hash64: 123456789,
					canonicalKey: '1,2,3,4,5,6,7',
					createdAt: '2024-01-01T10:00:00Z'
				},
				leastPreferredHand: {
					id: 2,
					cardIntIds: [6, 6, 6, 6, 6, 6, 6],
					size: 7,
					hash64: 987654321,
					canonicalKey: '6,6,6,6,6,6,6',
					createdAt: '2024-01-01T11:00:00Z'
				},
				insights: [
					'Hands with 2-3 lands are preferred 85% of the time',
					'Early game cards (CMC ≤ 2) score 40% higher',
					'Color consistency matters - mono-color hands preferred',
					'Hands with card draw are chosen 70% more often'
				]
			};
		} catch (err) {
			error = err instanceof Error ? err.message : 'Failed to load analysis data';
			console.error('Error loading analysis:', err);
		} finally {
			loading = false;
		}
	});

	function getCardName(cardId: number): string {
		const cardNames: Record<number, string> = {
			1: 'Lightning Bolt',
			2: 'Counterspell',
			3: 'Brainstorm',
			4: 'Ponder',
			5: 'Island',
			6: 'Mountain',
			7: 'Volcanic Island'
		};
		return cardNames[cardId] || `Card ${cardId}`;
	}
</script>

<svelte:head>
	<title>Analysis - MTG Mullagain</title>
</svelte:head>

<div class="max-w-7xl mx-auto">
	<h1 class="text-3xl font-bold text-gray-900 mb-8">Deck Analysis</h1>
	
	{#if loading}
		<div class="card text-center py-12">
			<div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
			<p class="text-gray-600">Loading analysis...</p>
		</div>
	{:else if error}
		<div class="card text-center py-12">
			<div class="text-red-600 mb-4">
				<svg class="w-12 h-12 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z" />
				</svg>
			</div>
			<h2 class="text-xl font-semibold mb-4">Error Loading Analysis</h2>
			<p class="text-gray-600 mb-6">{error}</p>
			<button on:click={() => window.location.reload()} class="btn btn-primary">
				Try Again
			</button>
		</div>
	{:else if analysisData}
		<!-- Statistics Overview -->
		<div class="grid md:grid-cols-3 gap-6 mb-8">
			<div class="card text-center">
				<div class="text-3xl font-bold text-blue-600 mb-2">{analysisData.totalComparisons}</div>
				<div class="text-sm text-gray-600">Total Comparisons</div>
			</div>
			<div class="card text-center">
				<div class="text-3xl font-bold text-green-600 mb-2">{analysisData.averageHandScore}</div>
				<div class="text-sm text-gray-600">Average Score</div>
			</div>
			<div class="card text-center">
				<div class="text-3xl font-bold text-purple-600 mb-2">85%</div>
				<div class="text-sm text-gray-600">Preference Rate</div>
			</div>
		</div>

		<!-- Charts Section -->
		<div class="grid lg:grid-cols-2 gap-6 mb-8">
			<CardFrequencyChart data={cardFrequencyData} title="Most Preferred Cards" />
			<HandSizeChart data={handSizeData} title="Hand Size Distribution" />
		</div>

		<div class="grid lg:grid-cols-1 gap-6 mb-8">
			<EvaluationTrendChart data={evaluationTrendData} title="Evaluation Trend Over Time" />
		</div>

		<!-- Insights Section -->
		<div class="grid md:grid-cols-2 gap-6 mb-8">
			<div class="card">
				<h2 class="text-lg font-semibold mb-4">Key Insights</h2>
				<div class="space-y-3">
					{#each analysisData.insights as insight}
						<div class="flex items-start space-x-3">
							<div class="w-2 h-2 bg-blue-500 rounded-full mt-2 flex-shrink-0"></div>
							<p class="text-sm text-gray-600">{insight}</p>
						</div>
					{/each}
				</div>
			</div>
			
			<div class="card">
				<h2 class="text-lg font-semibold mb-4">Recommendations</h2>
				<div class="space-y-3">
					<div class="p-3 bg-blue-50 rounded-lg">
						<p class="text-sm font-medium text-blue-900">Optimize Land Count</p>
						<p class="text-xs text-blue-700">Aim for 2-3 lands in opening hands</p>
					</div>
					<div class="p-3 bg-green-50 rounded-lg">
						<p class="text-sm font-medium text-green-900">Early Game Focus</p>
						<p class="text-xs text-green-700">Prioritize cards with CMC ≤ 2</p>
					</div>
					<div class="p-3 bg-purple-50 rounded-lg">
						<p class="text-sm font-medium text-purple-900">Card Draw Priority</p>
						<p class="text-xs text-purple-700">Include more card draw effects</p>
					</div>
				</div>
			</div>
		</div>
		
		<!-- Hand Examples -->
		<div class="grid md:grid-cols-2 gap-6">
			<div class="card">
				<h2 class="text-lg font-semibold mb-4">Most Preferred Hand</h2>
				<div class="space-y-2">
					{#each analysisData.mostPreferredHand.cardIntIds as cardId}
						<div class="px-3 py-2 bg-green-50 rounded-lg text-sm">
							{getCardName(cardId)}
						</div>
					{/each}
				</div>
				<div class="mt-4 text-xs text-gray-500">
					Hash: {analysisData.mostPreferredHand.hash64.toString(16)}
				</div>
			</div>
			
			<div class="card">
				<h2 class="text-lg font-semibold mb-4">Least Preferred Hand</h2>
				<div class="space-y-2">
					{#each analysisData.leastPreferredHand.cardIntIds as cardId}
						<div class="px-3 py-2 bg-red-50 rounded-lg text-sm">
							{getCardName(cardId)}
						</div>
					{/each}
				</div>
				<div class="mt-4 text-xs text-gray-500">
					Hash: {analysisData.leastPreferredHand.hash64.toString(16)}
				</div>
			</div>
		</div>
	{:else}
		<div class="card text-center py-12">
			<h2 class="text-xl font-semibold mb-4">No Data Available</h2>
			<p class="text-gray-600 mb-6">
				Start comparing hands to see analysis results.
			</p>
			<a href="/compare" class="btn btn-primary">
				Start Comparing
			</a>
		</div>
	{/if}
</div>
