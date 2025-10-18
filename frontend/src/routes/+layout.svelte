<script lang="ts">
	import '../app.css';
	import ConflictResolutionDialog from '$lib/components/ConflictResolutionDialog.svelte';
	import { syncStatus } from '$lib/db/sync-manager';
	import { backendHealth, startHealthCheck, stopHealthCheck } from '$lib/utils/backend-health';
	import { onMount, onDestroy } from 'svelte';
	import { page } from '$app/stores';

	onMount(() => {
		// Start backend health monitoring
		startHealthCheck();
	});

	onDestroy(() => {
		stopHealthCheck();
	});

	$: isAdminPage = $page.url.pathname === '/admin';
</script>

<div class="min-h-full">
	<!-- Navigation -->
	<nav class="bg-white shadow-sm border-b border-gray-200">
		<div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
			<div class="flex justify-between h-16">
				<div class="flex items-center">
					<a href="/" class="flex items-center">
						<h1 class="text-xl font-bold text-blue-600">MTG Mullagain</h1>
					</a>
				</div>
				<div class="flex items-center space-x-4">
				<!-- Backend Status Indicator -->
				<span 
					class="inline-block px-3 py-1 rounded-full text-sm font-semibold border-2 cursor-help relative group"
					class:bg-green-100={$backendHealth.isOnline}
					class:text-green-800={$backendHealth.isOnline}
					class:border-green-400={$backendHealth.isOnline}
					class:bg-yellow-100={!$backendHealth.isOnline}
					class:text-yellow-800={!$backendHealth.isOnline}
					class:border-yellow-400={!$backendHealth.isOnline}
					title="{$backendHealth.isOnline ? 'Connected to backend server' : 'Using only PGlite (local database)'}">
					{$backendHealth.isOnline ? '🟢 Online - Connected to Backend Server' : '⚠️ Offline - Using Only PGlite'}
						
						<!-- Hover Details -->
						<div class="absolute right-0 top-full mt-2 w-64 bg-gray-900 text-white text-xs rounded-lg shadow-lg p-3 z-50 hidden group-hover:block">
							<div class="space-y-1">
								<div class="flex justify-between">
									<span class="font-semibold">Mode:</span>
									<span class:text-green-400={$backendHealth.isOnline} class:text-yellow-400={!$backendHealth.isOnline}>
										{$backendHealth.isOnline ? 'Server Connected' : 'Local Only (PGlite)'}
									</span>
								</div>
								{#if $backendHealth.lastCheck}
									<div class="flex justify-between">
										<span class="font-semibold">Last Check:</span>
										<span>{new Date($backendHealth.lastCheck).toLocaleTimeString()}</span>
									</div>
								{/if}
								{#if $backendHealth.error}
									<div class="flex justify-between">
										<span class="font-semibold">Error:</span>
										<span class="text-red-400">{$backendHealth.error}</span>
									</div>
								{/if}
								{#if isAdminPage}
									<div class="border-t border-gray-700 pt-2 mt-2">
										<div class="flex justify-between">
											<span class="font-semibold">Sync Enabled:</span>
											<span class:text-green-400={$syncStatus.isEnabled} class:text-gray-400={!$syncStatus.isEnabled}>
												{$syncStatus.isEnabled ? 'Yes' : 'Paused'}
											</span>
										</div>
										{#if $syncStatus.pendingChanges > 0}
											<div class="flex justify-between">
												<span class="font-semibold">Pending:</span>
												<span class="text-yellow-400">{$syncStatus.pendingChanges} changes</span>
											</div>
										{/if}
									</div>
								{/if}
							</div>
						</div>
					</span>
					<a href="/admin" class="text-gray-600 hover:text-gray-900 font-semibold">⚙️ Admin</a>
					<a href="/import" class="text-gray-600 hover:text-gray-900">Import</a>
					<a href="/compare" class="text-gray-600 hover:text-gray-900">Compare</a>
					<a href="/analysis" class="text-gray-600 hover:text-gray-900">Analysis</a>
				</div>
			</div>
		</div>
	</nav>

	<!-- Main content -->
	<main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		<slot />
	</main>

	<!-- Conflict Resolution Dialog -->
	<ConflictResolutionDialog />
</div>
