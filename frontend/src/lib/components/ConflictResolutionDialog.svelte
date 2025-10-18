<script lang="ts">
  import { ConflictNotifier, type ConflictNotification, type ConflictResolution } from '$lib/electric/conflict-resolution.js';
  import { onMount, onDestroy } from 'svelte';

  let notifications: ConflictNotification[] = [];
  let conflictNotifier: ConflictNotifier;

  onMount(() => {
    conflictNotifier = ConflictNotifier.getInstance();
    notifications = conflictNotifier.getNotifications();

    // Listen for conflict notification changes
    const handleChange = () => {
      notifications = conflictNotifier.getNotifications();
    };

    window.addEventListener('conflict-notifications-changed', handleChange);
    
    return () => {
      window.removeEventListener('conflict-notifications-changed', handleChange);
    };
  });

  function resolveConflict(notification: ConflictNotification, resolution: ConflictResolution) {
    notification.resolve(resolution);
  }

  function getConflictDescription(conflict: any): string {
    switch (conflict.table) {
      case 'decks':
        return `Deck "${conflict.localVersion.name || conflict.remoteVersion.name}" was modified in both local and remote versions`;
      case 'deck_cards':
        return `Deck card count was changed in both local and remote versions`;
      case 'hand_evals':
        return `Hand evaluation was modified in both local and remote versions`;
      default:
        return `Record in ${conflict.table} was modified in both local and remote versions`;
    }
  }

  function formatTimestamp(timestamp: string): string {
    return new Date(timestamp).toLocaleString();
  }
</script>

{#if notifications.length > 0}
  <div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
    <div class="bg-white rounded-lg shadow-xl max-w-2xl w-full mx-4 max-h-[80vh] overflow-y-auto">
      <div class="p-6">
        <h2 class="text-xl font-bold text-gray-900 mb-4">Conflict Resolution</h2>
        
        {#each notifications as notification}
          <div class="border border-gray-200 rounded-lg p-4 mb-4">
            <h3 class="font-semibold text-gray-800 mb-2">
              {getConflictDescription(notification.conflict)}
            </h3>
            
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
              <!-- Local Version -->
              <div class="bg-blue-50 border border-blue-200 rounded p-3">
                <h4 class="font-medium text-blue-800 mb-2">Local Version</h4>
                <div class="text-sm text-blue-700">
                  <pre class="whitespace-pre-wrap overflow-x-auto">{JSON.stringify(notification.conflict.localVersion, null, 2)}</pre>
                </div>
                <div class="text-xs text-blue-600 mt-2">
                  Updated: {formatTimestamp(notification.conflict.localVersion.updated_at || notification.conflict.localVersion.created_at)}
                </div>
              </div>

              <!-- Remote Version -->
              <div class="bg-green-50 border border-green-200 rounded p-3">
                <h4 class="font-medium text-green-800 mb-2">Remote Version</h4>
                <div class="text-sm text-green-700">
                  <pre class="whitespace-pre-wrap overflow-x-auto">{JSON.stringify(notification.conflict.remoteVersion, null, 2)}</pre>
                </div>
                <div class="text-xs text-green-600 mt-2">
                  Updated: {formatTimestamp(notification.conflict.remoteVersion.updated_at || notification.conflict.remoteVersion.created_at)}
                </div>
              </div>
            </div>

            <!-- Resolution Options -->
            <div class="flex flex-wrap gap-2">
              <button
                on:click={() => resolveConflict(notification, { action: 'keep-local', message: 'Keeping local version' })}
                class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors"
              >
                Keep Local
              </button>
              
              <button
                on:click={() => resolveConflict(notification, { action: 'keep-remote', message: 'Keeping remote version' })}
                class="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 transition-colors"
              >
                Keep Remote
              </button>
              
              <button
                on:click={() => resolveConflict(notification, { action: 'merge', message: 'Merging versions' })}
                class="px-4 py-2 bg-purple-600 text-white rounded hover:bg-purple-700 transition-colors"
              >
                Merge
              </button>
              
              <button
                on:click={() => resolveConflict(notification, { action: 'manual', message: 'Manual resolution required' })}
                class="px-4 py-2 bg-gray-600 text-white rounded hover:bg-gray-700 transition-colors"
              >
                Manual
              </button>
            </div>
          </div>
        {/each}
      </div>
    </div>
  </div>
{/if}

<style>
  pre {
    font-family: 'Courier New', monospace;
    font-size: 0.75rem;
    line-height: 1.2;
  }
</style>
