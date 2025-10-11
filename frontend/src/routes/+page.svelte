<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';

	onMount(() => {
		// Check if we have any decks, if not redirect to import
		// TODO: Implement deck checking logic
	});
</script>

<svelte:head>
	<title>MTG Mullagain - Optimize Your Mulligan Decisions</title>
</svelte:head>

<div class="text-center">
	<div class="max-w-3xl mx-auto">
		<h1 class="text-4xl font-bold text-gray-900 mb-6">
			Welcome to MTG Mullagain
		</h1>
		<p class="text-xl text-gray-600 mb-8">
			Analyze and optimize your Magic: The Gathering mulligan decisions using statistical analysis and machine learning.
		</p>
		
		<div class="grid md:grid-cols-3 gap-6 mb-12">
			<div class="card text-center">
				<div class="text-blue-600 text-3xl mb-4">📊</div>
				<h3 class="text-lg font-semibold mb-2">Import Your Deck</h3>
				<p class="text-gray-600">Upload your deck list from CubeCobra, Moxfield, or enter manually.</p>
			</div>
			
			<div class="card text-center">
				<div class="text-green-600 text-3xl mb-4">🎯</div>
				<h3 class="text-lg font-semibold mb-2">Compare Hands</h3>
				<p class="text-gray-600">Draw two hands and choose which one you'd keep in different scenarios.</p>
			</div>
			
			<div class="card text-center">
				<div class="text-purple-600 text-3xl mb-4">🧠</div>
				<h3 class="text-lg font-semibold mb-2">Analyze Results</h3>
				<p class="text-gray-600">See aggregated results and learn which hands are statistically better.</p>
			</div>
		</div>
		
		<div class="space-x-4">
			<a href="/import" class="btn btn-primary">
				Get Started
			</a>
			<a href="/compare" class="btn btn-secondary">
				Try Demo
			</a>
		</div>
	</div>
</div>
