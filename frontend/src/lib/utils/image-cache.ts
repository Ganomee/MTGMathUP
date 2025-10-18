import localforage from 'localforage';

/**
 * Image cache using IndexedDB via localforage
 * Stores Scryfall card images for offline access
 */

// Create a dedicated storage instance for images
const imageStore = localforage.createInstance({
	name: 'mtg-mullagain',
	storeName: 'card-images'
});

// Cache expiry: 7 days
const CACHE_EXPIRY_MS = 7 * 24 * 60 * 60 * 1000;

interface CachedImage {
	blob: Blob;
	timestamp: number;
	url: string;
}

/**
 * Generate a cache key from URL
 */
function getCacheKey(url: string): string {
	return `img_${btoa(url).replace(/[/+=]/g, '_')}`;
}

/**
 * Check if cached image is expired
 */
function isExpired(timestamp: number): boolean {
	return Date.now() - timestamp > CACHE_EXPIRY_MS;
}

/**
 * Cache an image from a URL
 */
export async function cacheImage(url: string): Promise<string | null> {
	try {
		const response = await fetch(url);
		if (!response.ok) {
			console.error(`Failed to fetch image: ${response.status}`);
			return null;
		}

		const blob = await response.blob();
		const cached: CachedImage = {
			blob,
			timestamp: Date.now(),
			url
		};

		const key = getCacheKey(url);
		await imageStore.setItem(key, cached);

		// Return blob URL for immediate use
		return URL.createObjectURL(blob);
	} catch (error) {
		console.error('Error caching image:', error);
		return null;
	}
}

/**
 * Get a cached image, returns blob URL
 */
export async function getCachedImage(url: string): Promise<string | null> {
	try {
		const key = getCacheKey(url);
		const cached = await imageStore.getItem<CachedImage>(key);

		if (!cached) {
			return null;
		}

		// Check if expired
		if (isExpired(cached.timestamp)) {
			await imageStore.removeItem(key);
			return null;
		}

		// Return blob URL
		return URL.createObjectURL(cached.blob);
	} catch (error) {
		console.error('Error retrieving cached image:', error);
		return null;
	}
}

/**
 * Get or cache an image (convenience method)
 */
export async function getOrCacheImage(url: string): Promise<string> {
	// Try to get from cache first
	const cached = await getCachedImage(url);
	if (cached) {
		return cached;
	}

	// Otherwise, cache it and return
	const blobUrl = await cacheImage(url);
	return blobUrl || url; // Fallback to original URL if caching fails
}

/**
 * Preload and cache multiple images
 */
export async function preloadImages(urls: string[]): Promise<void> {
	const promises = urls.map(url => cacheImage(url));
	await Promise.allSettled(promises);
}

/**
 * Clear all cached images
 */
export async function clearImageCache(): Promise<void> {
	try {
		await imageStore.clear();
	} catch (error) {
		console.error('Error clearing image cache:', error);
	}
}

/**
 * Get cache statistics
 */
export async function getCacheStats(): Promise<{ count: number; keys: string[] }> {
	try {
		const keys = await imageStore.keys();
		return {
			count: keys.length,
			keys
		};
	} catch (error) {
		console.error('Error getting cache stats:', error);
		return { count: 0, keys: [] };
	}
}

/**
 * Remove expired images from cache
 */
export async function cleanExpiredImages(): Promise<number> {
	try {
		const keys = await imageStore.keys();
		let removed = 0;

		for (const key of keys) {
			const cached = await imageStore.getItem<CachedImage>(key);
			if (cached && isExpired(cached.timestamp)) {
				await imageStore.removeItem(key);
				removed++;
			}
		}

		return removed;
	} catch (error) {
		console.error('Error cleaning expired images:', error);
		return 0;
	}
}

