<script lang="ts">
	import { createEventDispatcher } from 'svelte';
	import DeckUploader from '$lib/components/DeckUploader.svelte';

	const dispatch = createEventDispatcher();

	function handleDeckImported(event: CustomEvent<any>) {
		// TODO: Handle deck import success
		console.log('Deck imported:', event.detail);
	}
</script>

<svelte:head>
	<title>Import Deck - MTG Mullagain</title>
</svelte:head>

<div class="max-w-2xl mx-auto">
	<h1 class="text-3xl font-bold text-gray-900 mb-8">Import Your Deck</h1>
	
	<div class="card mb-8">
		<h2 class="text-xl font-semibold mb-4">Supported Formats</h2>
		<div class="grid md:grid-cols-2 gap-4">
			<div class="p-4 bg-gray-50 rounded-lg">
				<h3 class="font-medium mb-2">CubeCobra</h3>
				<p class="text-sm text-gray-600">Paste your CubeCobra deck URL or export</p>
			</div>
			<div class="p-4 bg-gray-50 rounded-lg">
				<h3 class="font-medium mb-2">Moxfield</h3>
				<p class="text-sm text-gray-600">Import from Moxfield deck links</p>
			</div>
			<div class="p-4 bg-gray-50 rounded-lg">
				<h3 class="font-medium mb-2">Manual Entry</h3>
				<p class="text-sm text-gray-600">Enter cards manually or paste deck list</p>
			</div>
			<div class="p-4 bg-gray-50 rounded-lg">
				<h3 class="font-medium mb-2">Scryfall Bulk</h3>
				<p class="text-sm text-gray-600">Import from Scryfall bulk data</p>
			</div>
		</div>
	</div>

	<DeckUploader on:deckImported={handleDeckImported} />
</div>
