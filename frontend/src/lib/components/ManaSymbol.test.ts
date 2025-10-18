import { render } from '@testing-library/svelte';
import { describe, it, expect } from 'vitest';
import ManaSymbol from './ManaSymbol.svelte';

describe('ManaSymbol', () => {
	it('renders a single mana symbol', () => {
		const { container } = render(ManaSymbol, { props: { text: '{R}' } });
		const symbol = container.querySelector('.mana-symbol');
		expect(symbol).toBeInTheDocument();
		expect(symbol).toHaveClass('ms-r');
		expect(symbol).toHaveClass('ms-cost');
	});

	it('renders multiple mana symbols', () => {
		const { container } = render(ManaSymbol, { props: { text: '{2}{R}{G}' } });
		const symbols = container.querySelectorAll('.mana-symbol');
		expect(symbols).toHaveLength(3);
		expect(symbols[0]).toHaveClass('ms-2');
		expect(symbols[1]).toHaveClass('ms-r');
		expect(symbols[2]).toHaveClass('ms-g');
	});

	it('renders tap symbol', () => {
		const { container } = render(ManaSymbol, { props: { text: '{T}' } });
		const symbol = container.querySelector('.mana-symbol');
		expect(symbol).toHaveClass('ms-t');
	});

	it('renders mixed text and symbols', () => {
		const { container } = render(ManaSymbol, { props: { text: 'Pay {2}{R}: Deal 3 damage' } });
		expect(container.textContent).toContain('Pay');
		expect(container.textContent).toContain('Deal 3 damage');
		const symbols = container.querySelectorAll('.mana-symbol');
		expect(symbols).toHaveLength(2);
	});

	it('renders hybrid mana symbols', () => {
		const { container } = render(ManaSymbol, { props: { text: '{G/W}' } });
		const symbol = container.querySelector('.mana-symbol');
		expect(symbol).toHaveClass('ms-gw');
	});

	it('renders phyrexian mana symbols', () => {
		const { container } = render(ManaSymbol, { props: { text: '{G/P}' } });
		const symbol = container.querySelector('.mana-symbol');
		expect(symbol).toHaveClass('ms-gp');
	});

	it('renders X mana cost', () => {
		const { container } = render(ManaSymbol, { props: { text: '{X}{R}' } });
		const symbols = container.querySelectorAll('.mana-symbol');
		expect(symbols[0]).toHaveClass('ms-x');
	});

	it('handles empty text', () => {
		const { container } = render(ManaSymbol, { props: { text: '' } });
		expect(container.textContent).toBe('');
	});

	it('handles text without symbols', () => {
		const { container } = render(ManaSymbol, { props: { text: 'No symbols here' } });
		expect(container.textContent).toBe('No symbols here');
		const symbols = container.querySelectorAll('.mana-symbol');
		expect(symbols).toHaveLength(0);
	});

	it('applies custom size class', () => {
		const { container } = render(ManaSymbol, { props: { text: '{R}', size: 'large' } });
		const symbol = container.querySelector('.mana-symbol');
		expect(symbol).toHaveClass('mana-large');
	});

	it('applies inline style when inline prop is true', () => {
		const { container } = render(ManaSymbol, { props: { text: '{R}', inline: true } });
		const wrapper = container.querySelector('span');
		expect(wrapper).toHaveClass('inline');
	});

	it('renders colorless mana', () => {
		const { container } = render(ManaSymbol, { props: { text: '{C}' } });
		const symbol = container.querySelector('.mana-symbol');
		expect(symbol).toHaveClass('ms-c');
	});

	it('renders snow mana', () => {
		const { container } = render(ManaSymbol, { props: { text: '{S}' } });
		const symbol = container.querySelector('.mana-symbol');
		expect(symbol).toHaveClass('ms-s');
	});

	it('renders energy counter', () => {
		const { container } = render(ManaSymbol, { props: { text: '{E}' } });
		const symbol = container.querySelector('.mana-symbol');
		expect(symbol).toHaveClass('ms-e');
	});

	it('renders multiple different symbols in sequence', () => {
		const { container } = render(ManaSymbol, { props: { text: '{T}: Add {G}{W} or {R}{R}' } });
		const symbols = container.querySelectorAll('.mana-symbol');
		expect(symbols).toHaveLength(5);
		expect(container.textContent).toContain(': Add');
		expect(container.textContent).toContain('or');
	});
});
