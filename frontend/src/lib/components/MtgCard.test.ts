import { render, screen, waitFor } from '@testing-library/svelte';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { tick } from 'svelte';
import MtgCard from './MtgCard.svelte';
import * as imageCache from '$lib/utils/image-cache';
import type { Card } from '@scryfall/api-types';

describe('MtgCard', () => {
	const mockCard: Partial<Card> = {
		name: 'Lightning Bolt',
		mana_cost: '{R}',
		type_line: 'Instant',
		oracle_text: 'Lightning Bolt deals 3 damage to any target.',
		power: undefined,
		toughness: undefined,
		image_uris: {
			small: 'https://cards.scryfall.io/small/test.jpg',
			normal: 'https://cards.scryfall.io/normal/test.jpg',
			large: 'https://cards.scryfall.io/large/test.jpg',
			png: 'https://cards.scryfall.io/png/test.png',
			art_crop: 'https://cards.scryfall.io/art_crop/test.jpg',
			border_crop: 'https://cards.scryfall.io/border_crop/test.jpg'
		}
	};

	const mockCreatureCard: Partial<Card> = {
		name: 'Grizzly Bears',
		mana_cost: '{1}{G}',
		type_line: 'Creature — Bear',
		oracle_text: 'A bear is a bear.',
		power: '2',
		toughness: '2',
		image_uris: {
			small: 'https://cards.scryfall.io/small/bear.jpg',
			normal: 'https://cards.scryfall.io/normal/bear.jpg',
			large: 'https://cards.scryfall.io/large/bear.jpg',
			png: 'https://cards.scryfall.io/png/bear.png',
			art_crop: 'https://cards.scryfall.io/art_crop/bear.jpg',
			border_crop: 'https://cards.scryfall.io/border_crop/bear.jpg'
		}
	};

	beforeEach(() => {
		vi.clearAllMocks();
		global.URL.createObjectURL = vi.fn(() => 'blob:mock-url');
	});

	it('renders card name', () => {
		render(MtgCard, { props: { card: mockCard as Card } });
		expect(screen.getByText('Lightning Bolt')).toBeInTheDocument();
	});

	it('renders mana cost with ManaSymbol component', () => {
		const { container } = render(MtgCard, { props: { card: mockCard as Card } });
		const manaSymbol = container.querySelector('.mana-symbol');
		expect(manaSymbol).toBeInTheDocument();
	});

	it('renders type line', () => {
		render(MtgCard, { props: { card: mockCard as Card } });
		expect(screen.getByText('Instant')).toBeInTheDocument();
	});

	it('renders oracle text', () => {
		render(MtgCard, { props: { card: mockCard as Card } });
		expect(screen.getByText(/Lightning Bolt deals 3 damage/)).toBeInTheDocument();
	});

	it('renders power and toughness for creatures', () => {
		render(MtgCard, { props: { card: mockCreatureCard as Card } });
		expect(screen.getByText('2/2')).toBeInTheDocument();
	});

	it('does not render power/toughness for non-creatures', () => {
		render(MtgCard, { props: { card: mockCard as Card } });
		expect(screen.queryByText('/')).not.toBeInTheDocument();
	});

	it('starts with text display mode', () => {
		const { container } = render(MtgCard, { props: { card: mockCard as Card } });
		const textDisplay = container.querySelector('.card-text-display');
		expect(textDisplay).toBeInTheDocument();
	});

	it('shows loading indicator when image is being fetched', async () => {
		// Simulate slow image loading
		vi.spyOn(imageCache, 'getOrCacheImage').mockImplementation(
			() => new Promise(() => {}) // Never resolves
		);

		const { container } = render(MtgCard, { props: { card: mockCard as Card } });
		
		await tick();
		
		// Should show loading text
		expect(container.textContent).toContain('Loading image...');
	});

	it('renders in text mode by default', () => {
		const { container } = render(MtgCard, { props: { card: mockCard as Card } });
		
		// Should start with text display
		const textDisplay = container.querySelector('.card-text-display');
		expect(textDisplay).toBeInTheDocument();
	});

	it('has hover effect classes for text overlay', () => {
		const { container } = render(MtgCard, { props: { card: mockCard as Card } });

		// Should have group class for hover effects
		const cardElement = container.querySelector('.mtg-card');
		expect(cardElement).toHaveClass('group');
	});

	it('handles different card sizes', () => {
		const { container } = render(MtgCard, { 
			props: { card: mockCard as Card, size: 'large' } 
		});
		
		const cardElement = container.querySelector('.mtg-card');
		expect(cardElement).toHaveClass('card-large');
	});

	it('applies size classes correctly', () => {
		const { container: smallContainer } = render(MtgCard, { 
			props: { card: mockCard as Card, size: 'small' } 
		});
		expect(smallContainer.querySelector('.card-small')).toBeInTheDocument();

		const { container: mediumContainer } = render(MtgCard, { 
			props: { card: mockCard as Card, size: 'medium' } 
		});
		expect(mediumContainer.querySelector('.card-medium')).toBeInTheDocument();

		const { container: largeContainer } = render(MtgCard, { 
			props: { card: mockCard as Card, size: 'large' } 
		});
		expect(largeContainer.querySelector('.card-large')).toBeInTheDocument();
	});

	it('handles cards without images gracefully', () => {
		const cardNoImage = { ...mockCard, image_uris: undefined };
		const { container } = render(MtgCard, { props: { card: cardNoImage as Card } });
		
		// Should still show text display
		const textDisplay = container.querySelector('.card-text-display');
		expect(textDisplay).toBeInTheDocument();
	});

	it('handles image loading errors by showing text display', () => {
		vi.spyOn(imageCache, 'getOrCacheImage').mockResolvedValue(null);

		const { container } = render(MtgCard, { props: { card: mockCard as Card } });

		// Should show text display (default state)
		const textDisplay = container.querySelector('.card-text-display');
		expect(textDisplay).toBeInTheDocument();
	});

	it('applies clickable styles when onClick is provided', () => {
		const onClick = vi.fn();
		const { container } = render(MtgCard, { 
			props: { card: mockCard as Card, onClick } 
		});
		
		const cardElement = container.querySelector('.mtg-card');
		expect(cardElement).toHaveClass('cursor-pointer');
	});

	it('calls onClick handler when clicked', async () => {
		const onClick = vi.fn();
		const { container } = render(MtgCard, { 
			props: { card: mockCard as Card, onClick } 
		});
		
		const cardElement = container.querySelector('.mtg-card') as HTMLElement;
		cardElement.click();

		expect(onClick).toHaveBeenCalledWith(mockCard);
	});

	it('shows correct aria labels for accessibility', () => {
		const { container } = render(MtgCard, { props: { card: mockCard as Card } });
		
		const cardElement = container.querySelector('.mtg-card');
		expect(cardElement).toHaveAttribute('aria-label', 'Lightning Bolt');
		expect(cardElement).toHaveAttribute('role', 'img');
	});
});

