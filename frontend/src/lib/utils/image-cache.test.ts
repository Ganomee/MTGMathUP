import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import * as imageCache from './image-cache';

describe('Image Cache', () => {
	const mockUrl = 'https://cards.scryfall.io/normal/front/test.jpg';
	const mockBlobUrl = 'blob:mock-url';
	const mockBlob = new Blob(['test'], { type: 'image/jpeg' });

	// Mock URL.createObjectURL which doesn't exist in jsdom
	beforeEach(() => {
		global.URL.createObjectURL = vi.fn(() => mockBlobUrl);
		global.URL.revokeObjectURL = vi.fn();
		vi.clearAllMocks();
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	describe('cacheImage', () => {
		it('should cache an image from URL', async () => {
			global.fetch = vi.fn().mockResolvedValue({
				ok: true,
				blob: () => Promise.resolve(mockBlob)
			});

			const result = await imageCache.cacheImage(mockUrl);
			expect(result).toBe(mockBlobUrl);
			expect(global.fetch).toHaveBeenCalledWith(mockUrl);
			expect(URL.createObjectURL).toHaveBeenCalledWith(mockBlob);
		});

		it('should handle fetch errors', async () => {
			global.fetch = vi.fn().mockResolvedValue({
				ok: false,
				status: 404
			});

			const result = await imageCache.cacheImage(mockUrl);
			expect(result).toBe(null);
		});

		it('should handle network errors', async () => {
			global.fetch = vi.fn().mockRejectedValue(new Error('Network error'));

			const result = await imageCache.cacheImage(mockUrl);
			expect(result).toBe(null);
		});
	});

	describe('getCachedImage', () => {
		it('should return cached image if exists and not expired', async () => {
			// First cache an image
			global.fetch = vi.fn().mockResolvedValue({
				ok: true,
				blob: () => Promise.resolve(mockBlob)
			});

			await imageCache.cacheImage(mockUrl);

			// Then retrieve it
			const result = await imageCache.getCachedImage(mockUrl);
			expect(result).toBe(mockBlobUrl);
		});

		it('should return null if image not cached', async () => {
			const result = await imageCache.getCachedImage('https://non-existent-url.jpg');
			expect(result).toBe(null);
		});

		it('should return null and remove expired images', async () => {
			// Cache an image
			global.fetch = vi.fn().mockResolvedValue({
				ok: true,
				blob: () => Promise.resolve(mockBlob)
			});

			await imageCache.cacheImage(mockUrl);

			// Mock Date.now to simulate expiry (8 days later)
			const originalDateNow = Date.now;
			Date.now = vi.fn(() => originalDateNow() + 8 * 24 * 60 * 60 * 1000);

			const result = await imageCache.getCachedImage(mockUrl);
			expect(result).toBe(null);

			// Restore Date.now
			Date.now = originalDateNow;
		});
	});

	describe('getOrCacheImage', () => {
		it('should return cached image if available', async () => {
			// First cache an image
			global.fetch = vi.fn().mockResolvedValue({
				ok: true,
				blob: () => Promise.resolve(mockBlob)
			});

			await imageCache.cacheImage(mockUrl);

			// Clear fetch mock to ensure we're not fetching again
			vi.clearAllMocks();

			const result = await imageCache.getOrCacheImage(mockUrl);
			expect(result).toBe(mockBlobUrl);
			expect(global.fetch).not.toHaveBeenCalled();
		});

		it('should cache and return image if not in cache', async () => {
			global.fetch = vi.fn().mockResolvedValue({
				ok: true,
				blob: () => Promise.resolve(mockBlob)
			});

			const result = await imageCache.getOrCacheImage('https://new-image.jpg');
			expect(result).toBe(mockBlobUrl);
			expect(global.fetch).toHaveBeenCalled();
		});

	it('should fallback to original URL if caching fails', async () => {
		// Clear cache to ensure we're testing the failure case
		await imageCache.clearImageCache();
		
		global.fetch = vi.fn().mockRejectedValue(new Error('Network error'));

		const result = await imageCache.getOrCacheImage('https://new-failing-url.jpg');
		expect(result).toBe('https://new-failing-url.jpg');
	});
	});

	describe('preloadImages', () => {
		it('should preload multiple images', async () => {
			global.fetch = vi.fn().mockResolvedValue({
				ok: true,
				blob: () => Promise.resolve(mockBlob)
			});

			const urls = [
				'https://image1.jpg',
				'https://image2.jpg',
				'https://image3.jpg'
			];

			await imageCache.preloadImages(urls);
			expect(global.fetch).toHaveBeenCalledTimes(3);
		});

		it('should handle partial failures', async () => {
			global.fetch = vi.fn()
				.mockResolvedValueOnce({ ok: true, blob: () => Promise.resolve(mockBlob) })
				.mockRejectedValueOnce(new Error('Failed'))
				.mockResolvedValueOnce({ ok: true, blob: () => Promise.resolve(mockBlob) });

			const urls = ['https://image1.jpg', 'https://image2.jpg', 'https://image3.jpg'];

			// Should not throw despite one failure
			await expect(imageCache.preloadImages(urls)).resolves.not.toThrow();
		});
	});

	describe('clearImageCache', () => {
		it('should clear all cached images', async () => {
			// Cache some images first
			global.fetch = vi.fn().mockResolvedValue({
				ok: true,
				blob: () => Promise.resolve(mockBlob)
			});

			await imageCache.cacheImage('https://image1.jpg');
			await imageCache.cacheImage('https://image2.jpg');

			// Clear cache
			await imageCache.clearImageCache();

			// Verify images are gone
			const result1 = await imageCache.getCachedImage('https://image1.jpg');
			const result2 = await imageCache.getCachedImage('https://image2.jpg');
			expect(result1).toBe(null);
			expect(result2).toBe(null);
		});
	});

	describe('getCacheStats', () => {
		it('should return cache statistics', async () => {
			// Clear cache first
			await imageCache.clearImageCache();

			// Cache some images
			global.fetch = vi.fn().mockResolvedValue({
				ok: true,
				blob: () => Promise.resolve(mockBlob)
			});

			await imageCache.cacheImage('https://image1.jpg');
			await imageCache.cacheImage('https://image2.jpg');
			await imageCache.cacheImage('https://image3.jpg');

			const stats = await imageCache.getCacheStats();
			expect(stats.count).toBe(3);
			expect(stats.keys).toHaveLength(3);
		});

		it('should return empty stats when cache is empty', async () => {
			await imageCache.clearImageCache();

			const stats = await imageCache.getCacheStats();
			expect(stats.count).toBe(0);
			expect(stats.keys).toHaveLength(0);
		});
	});

	describe('cleanExpiredImages', () => {
		it('should remove only expired images', async () => {
			await imageCache.clearImageCache();

			global.fetch = vi.fn().mockResolvedValue({
				ok: true,
				blob: () => Promise.resolve(mockBlob)
			});

			// Cache some images
			await imageCache.cacheImage('https://image1.jpg');
			await imageCache.cacheImage('https://image2.jpg');

			// Mock Date.now to simulate some images being expired
			const originalDateNow = Date.now;
			Date.now = vi.fn(() => originalDateNow() + 8 * 24 * 60 * 60 * 1000);

			const removed = await imageCache.cleanExpiredImages();
			expect(removed).toBe(2); // Both images should be expired

			// Restore Date.now
			Date.now = originalDateNow;
		});
	});
});
