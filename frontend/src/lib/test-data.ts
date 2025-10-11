/**
 * Test data for development and testing
 */

import type { Hand, PaginatedHandResponse } from './api/client';

export const mockHands: Hand[] = [
	{
		id: 1,
		cardIntIds: [1, 2, 3, 4, 5, 6, 7],
		size: 7,
		hash64: 123456789,
		canonicalKey: '1,2,3,4,5,6,7',
		createdAt: '2024-01-01T10:00:00Z'
	},
	{
		id: 2,
		cardIntIds: [2, 3, 4, 5, 6, 7, 8],
		size: 7,
		hash64: 987654321,
		canonicalKey: '2,3,4,5,6,7,8',
		createdAt: '2024-01-01T11:00:00Z'
	},
	{
		id: 3,
		cardIntIds: [1, 1, 2, 2, 3, 3, 4],
		size: 7,
		hash64: 456789123,
		canonicalKey: '1,1,2,2,3,3,4',
		createdAt: '2024-01-01T12:00:00Z'
	}
];

export const mockCardNames: Record<number, string> = {
	1: 'Lightning Bolt',
	2: 'Counterspell',
	3: 'Brainstorm',
	4: 'Ponder',
	5: 'Island',
	6: 'Mountain',
	7: 'Volcanic Island',
	8: 'Force of Will',
	9: 'Delver of Secrets',
	10: 'Snapcaster Mage'
};

export const mockAnalysisData = {
	totalComparisons: 66,
	averageHandScore: 7.2,
	mostPreferredHand: mockHands[0],
	leastPreferredHand: {
		id: 999,
		cardIntIds: [6, 6, 6, 6, 6, 6, 6],
		size: 7,
		hash64: 999999999,
		canonicalKey: '6,6,6,6,6,6,6',
		createdAt: '2024-01-01T13:00:00Z'
	},
	insights: [
		'Hands with 2-3 lands are preferred 85% of the time',
		'Early game cards (CMC ≤ 2) score 40% higher',
		'Color consistency matters - mono-color hands preferred',
		'Hands with card draw are chosen 70% more often'
	]
};

export const mockCardFrequencyData = [
	{ name: 'Lightning Bolt', count: 15, color: 'red' },
	{ name: 'Counterspell', count: 12, color: 'blue' },
	{ name: 'Brainstorm', count: 10, color: 'blue' },
	{ name: 'Ponder', count: 8, color: 'blue' },
	{ name: 'Island', count: 20, color: 'blue' },
	{ name: 'Mountain', count: 18, color: 'red' },
	{ name: 'Force of Will', count: 6, color: 'blue' },
	{ name: 'Delver of Secrets', count: 4, color: 'blue' }
];

export const mockHandSizeData = [
	{ name: '5 cards', value: 8 },
	{ name: '6 cards', value: 15 },
	{ name: '7 cards', value: 25 },
	{ name: '8 cards', value: 12 },
	{ name: '9 cards', value: 5 }
];

export const mockEvaluationTrendData = [
	{ date: '2024-01-01', evaluations: 5, cumulative: 5 },
	{ date: '2024-01-02', evaluations: 8, cumulative: 13 },
	{ date: '2024-01-03', evaluations: 12, cumulative: 25 },
	{ date: '2024-01-04', evaluations: 6, cumulative: 31 },
	{ date: '2024-01-05', evaluations: 15, cumulative: 46 },
	{ date: '2024-01-06', evaluations: 9, cumulative: 55 },
	{ date: '2024-01-07', evaluations: 11, cumulative: 66 }
];


