import { writable } from 'svelte/store';

export interface BackendHealth {
  isOnline: boolean;
  lastCheck: Date | null;
  error: string | null;
}

export const backendHealth = writable<BackendHealth>({
  isOnline: false,
  lastCheck: null,
  error: null
});

const BACKEND_URL = 'http://localhost:5000';
const CHECK_INTERVAL = 5000; // Check every 5 seconds

let healthCheckInterval: number | null = null;

/**
 * Check if backend is reachable
 */
export async function checkBackendHealth(): Promise<boolean> {
  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 2000); // 2 second timeout

    const response = await fetch(`${BACKEND_URL}/health`, {
      signal: controller.signal,
      cache: 'no-cache'
    });

    clearTimeout(timeoutId);

    const isHealthy = response.ok;
    
    backendHealth.set({
      isOnline: isHealthy,
      lastCheck: new Date(),
      error: isHealthy ? null : `HTTP ${response.status}`
    });

    return isHealthy;
  } catch (error: any) {
    backendHealth.set({
      isOnline: false,
      lastCheck: new Date(),
      error: error.name === 'AbortError' ? 'Timeout' : error.message
    });
    return false;
  }
}

/**
 * Start periodic health checks
 */
export function startHealthCheck(): void {
  if (healthCheckInterval !== null) {
    return; // Already running
  }

  // Initial check
  checkBackendHealth();

  // Periodic checks
  healthCheckInterval = window.setInterval(() => {
    checkBackendHealth();
  }, CHECK_INTERVAL);

  console.log('🏥 Backend health check started');
}

/**
 * Stop periodic health checks
 */
export function stopHealthCheck(): void {
  if (healthCheckInterval !== null) {
    clearInterval(healthCheckInterval);
    healthCheckInterval = null;
    console.log('🏥 Backend health check stopped');
  }
}

