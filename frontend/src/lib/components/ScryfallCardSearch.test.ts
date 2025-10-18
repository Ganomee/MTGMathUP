import { render, screen, waitFor } from '@testing-library/svelte';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import ScryfallCardSearch from './ScryfallCardSearch.svelte';
import type { Card } from '@scryfall/api-types';

// Mock scryfall-api
vi.mock('scryfall-api', () => ({
	Cards: {
		search: vi.fn()
	}
}));

describe('ScryfallCardSearch', () => {
	const mockCards: Partial<Card>[] = [
		{
			id: '1',
			name: 'Lightning Bolt',
			mana_cost: '{R}',
			type_line: 'Instant',
			oracle_text: 'Lightning Bolt deals 3 damage to any target.'
		},
		{
			id: '2',
			name: 'Counterspell',
			mana_cost: '{U}{U}',
			type_line: 'Instant',
			oracle_text: 'Counter target spell.'
		}
	];

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it('renders search input', () => {
		render(ScryfallCardSearch);
		expect(screen.getByPlaceholderText(/search cards/i)).toBeInTheDocument();
	});

	it('shows loading state while searching', async () => {
		const { Cards } = await import('scryfall-api');
		vi.mocked(Cards.search).mockImplementation(
			() => new Promise(() => {}) // Never resolves
		);

		const { container } = render(ScryfallCardSearch);
		const input = screen.getByPlaceholderText(/search cards/i);
		
		await userEvent.type(input, 'lightning');

		await waitFor(() => {
			// Check for loading spinner (title attribute or animation class)
			const spinner = container.querySelector('[title="Searching..."]') || 
			                container.querySelector('.animate-spin');
			expect(spinner).toBeInTheDocument();
		}, { timeout: 1000 });
	});

	it('displays search results', async () => {
		const { Cards } = await import('scryfall-api');
		vi.mocked(Cards.search).mockResolvedValue(mockCards as any);

		render(ScryfallCardSearch);
		const input = screen.getByPlaceholderText(/search cards/i);
		
		await userEvent.type(input, 'lightning');

		await waitFor(() => {
			expect(screen.getByText('Lightning Bolt')).toBeInTheDocument();
			expect(screen.getByText('Counterspell')).toBeInTheDocument();
		});
	});

	it('calls onCardSelect when card is clicked', async () => {
		const { Cards } = await import('scryfall-api');
		vi.mocked(Cards.search).mockResolvedValue(mockCards as any);

		const onCardSelect = vi.fn();
		render(ScryfallCardSearch, { props: { onCardSelect } });
		
		const input = screen.getByPlaceholderText(/search cards/i);
		await userEvent.type(input, 'lightning');

		await waitFor(() => {
			expect(screen.getByText('Lightning Bolt')).toBeInTheDocument();
		});

		const cardButton = screen.getByText('Lightning Bolt').closest('button')!;
		await userEvent.click(cardButton);

		expect(onCardSelect).toHaveBeenCalledWith(expect.objectContaining({
			name: 'Lightning Bolt'
		}));
	});

	it('handles search errors gracefully', async () => {
		const { Cards } = await import('scryfall-api');
		vi.mocked(Cards.search).mockRejectedValue(new Error('Network error'));

		render(ScryfallCardSearch);
		const input = screen.getByPlaceholderText(/search cards/i);
		
		await userEvent.type(input, 'lightning');

		await waitFor(() => {
			// Check for error text in the component (using more specific text)
			expect(screen.getByText('Network error')).toBeInTheDocument();
		}, { timeout: 1500 });
	});

	it('shows no results message when search returns empty', async () => {
		const { Cards } = await import('scryfall-api');
		vi.mocked(Cards.search).mockResolvedValue([]);

		render(ScryfallCardSearch);
		const input = screen.getByPlaceholderText(/search cards/i);
		
		await userEvent.type(input, 'nonexistent card');

		await waitFor(() => {
			expect(screen.getByText(/no cards found/i)).toBeInTheDocument();
		});
	});

	it('debounces search input', async () => {
		const { Cards } = await import('scryfall-api');
		const searchSpy = vi.mocked(Cards.search).mockResolvedValue(mockCards as any);

		render(ScryfallCardSearch);
		const input = screen.getByPlaceholderText(/search cards/i);
		
		// Type multiple characters quickly
		await userEvent.type(input, 'light');

		// Should not have searched yet (debounced)
		expect(searchSpy).not.toHaveBeenCalled();

		// Wait for debounce
		await waitFor(() => {
			expect(searchSpy).toHaveBeenCalledTimes(1);
		}, { timeout: 1000 });
	});

	it('supports Scryfall search syntax', async () => {
		const { Cards } = await import('scryfall-api');
		const searchSpy = vi.mocked(Cards.search).mockResolvedValue(mockCards as any);

		render(ScryfallCardSearch);
		const input = screen.getByPlaceholderText(/search cards/i);
		
		await userEvent.type(input, 't:instant c:red cmc=1');

		await waitFor(() => {
			expect(searchSpy).toHaveBeenCalledWith('t:instant c:red cmc=1', expect.anything());
		}, { timeout: 1000 });
	});

	it('clears results when input is cleared', async () => {
		const { Cards } = await import('scryfall-api');
		vi.mocked(Cards.search).mockResolvedValue(mockCards as any);

		render(ScryfallCardSearch);
		const input = screen.getByPlaceholderText(/search cards/i) as HTMLInputElement;
		
		await userEvent.type(input, 'lightning');

		await waitFor(() => {
			expect(screen.getByText('Lightning Bolt')).toBeInTheDocument();
		});

		await userEvent.clear(input);

		await waitFor(() => {
			expect(screen.queryByText('Lightning Bolt')).not.toBeInTheDocument();
		});
	});

	it('displays mana symbols in results', async () => {
		const { Cards } = await import('scryfall-api');
		vi.mocked(Cards.search).mockResolvedValue(mockCards as any);

		render(ScryfallCardSearch);
		const input = screen.getByPlaceholderText(/search cards/i);
		
		await userEvent.type(input, 'counterspell');

		await waitFor(() => {
			const container = screen.getByText('Counterspell').closest('button')!;
			expect(container.querySelector('.mana-symbol')).toBeInTheDocument();
		});
	});

	it('shows limit on number of results', async () => {
		const manyCards = Array.from({ length: 50 }, (_, i) => ({
			id: `${i}`,
			name: `Card ${i}`,
			type_line: 'Creature'
		}));

		const { Cards } = await import('scryfall-api');
		vi.mocked(Cards.search).mockResolvedValue(manyCards as any);

		render(ScryfallCardSearch, { props: { maxResults: 10 } });
		const input = screen.getByPlaceholderText(/search cards/i);
		
		await userEvent.type(input, 'card');

		await waitFor(() => {
			// Should only show maxResults
			const results = screen.getAllByRole('button');
			expect(results.length).toBeLessThanOrEqual(11); // 10 results + potentially the clear button
		});
	});
});

