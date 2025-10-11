<script lang="ts">
	import { onMount } from 'svelte';
	import { browser } from '$app/environment';

	let isOnline = true;
	let lastSync = new Date();

	onMount(() => {
		if (!browser) return;

		// Check initial online status
		isOnline = navigator.onLine;

		// Listen for online/offline events
		const handleOnline = () => {
			isOnline = true;
			lastSync = new Date();
		};

		const handleOffline = () => {
			isOnline = false;
		};

		window.addEventListener('online', handleOnline);
		window.addEventListener('offline', handleOffline);

		// Cleanup
		return () => {
			window.removeEventListener('online', handleOnline);
			window.removeEventListener('offline', handleOffline);
		};
	});

	function formatLastSync(date: Date): string {
		const now = new Date();
		const diff = now.getTime() - date.getTime();
		const minutes = Math.floor(diff / 60000);
		
		if (minutes < 1) return 'Just now';
		if (minutes === 1) return '1 minute ago';
		return `${minutes} minutes ago`;
	}
</script>

<div class="flex items-center space-x-2 text-sm">
	{#if isOnline}
		<div class="flex items-center text-green-600">
			<div class="w-2 h-2 bg-green-500 rounded-full mr-2"></div>
			<span>Online</span>
		</div>
		<div class="text-gray-500">
			Synced {formatLastSync(lastSync)}
		</div>
	{:else}
		<div class="flex items-center text-orange-600">
			<div class="w-2 h-2 bg-orange-500 rounded-full mr-2"></div>
			<span>Offline</span>
		</div>
		<div class="text-gray-500">
			Last sync: {formatLastSync(lastSync)}
		</div>
	{/if}
</div>
