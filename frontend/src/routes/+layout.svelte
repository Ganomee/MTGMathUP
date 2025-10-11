<script lang="ts">
	import '../app.css';
	import { QueryClient, QueryClientProvider } from '@tanstack/svelte-query';
	import OfflineStatus from '$lib/components/OfflineStatus.svelte';

	// Create a client
	const queryClient = new QueryClient({
		defaultOptions: {
			queries: {
				staleTime: 1000 * 60 * 5, // 5 minutes
				retry: (failureCount, error) => {
					// Don't retry on network errors when offline
					if (error instanceof Error && error.message.includes('fetch')) {
						return false;
					}
					return failureCount < 3;
				}
			}
		}
	});
</script>

<QueryClientProvider client={queryClient}>
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
						<a href="/import" class="text-gray-600 hover:text-gray-900">Import</a>
						<a href="/compare" class="text-gray-600 hover:text-gray-900">Compare</a>
						<a href="/analysis" class="text-gray-600 hover:text-gray-900">Analysis</a>
						<OfflineStatus />
					</div>
				</div>
			</div>
		</nav>

		<!-- Main content -->
		<main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
			<slot />
		</main>
	</div>
</QueryClientProvider>
