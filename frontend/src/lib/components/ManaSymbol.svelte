<script lang="ts">
	/**
	 * Component for rendering MTG mana symbols using mana-font
	 * Converts text like "{R}" to mana symbols
	 */
	export let text: string = '';
	export let size: 'small' | 'medium' | 'large' = 'medium';
	export let inline: boolean = false;

	// Size mapping for mana-font classes
	const sizeMap = {
		small: 'mana-small',
		medium: 'mana-medium',
		large: 'mana-large'
	};

	interface TextSegment {
		type: 'text' | 'symbol';
		content: string;
		manaClass?: string;
	}

	// Parse text and convert {X} patterns to mana symbols
	function parseText(input: string): TextSegment[] {
		const segments: TextSegment[] = [];
		const regex = /\{([^}]+)\}/g;
		let lastIndex = 0;
		let match;

		while ((match = regex.exec(input)) !== null) {
			// Add text before the symbol
			if (match.index > lastIndex) {
				segments.push({
					type: 'text',
					content: input.slice(lastIndex, match.index)
				});
			}

			// Add the mana symbol
			const symbolContent = match[1];
			const manaClass = getManaClass(symbolContent);
			segments.push({
				type: 'symbol',
				content: symbolContent,
				manaClass
			});

			lastIndex = regex.lastIndex;
		}

		// Add remaining text
		if (lastIndex < input.length) {
			segments.push({
				type: 'text',
				content: input.slice(lastIndex)
			});
		}

		return segments;
	}

	// Convert symbol content to mana-font class
	function getManaClass(symbol: string): string {
		const normalized = symbol.toLowerCase().replace(/\//g, '');
		return `ms ms-${normalized} ms-cost`;
	}

	$: segments = parseText(text);
</script>

<span class:inline>
	{#each segments as segment}
		{#if segment.type === 'text'}
			{segment.content}
		{:else}
			<i class="mana-symbol {segment.manaClass} {sizeMap[size]}" aria-label={segment.content}></i>
		{/if}
	{/each}
</span>

<style>
	.inline {
		display: inline;
	}
	
	.mana-symbol {
		display: inline-block;
		vertical-align: middle;
	}

	/* Size adjustments for mana symbols */
	:global(.mana-small) {
		font-size: 1em;
	}

	:global(.mana-medium) {
		font-size: 1.25em;
	}

	:global(.mana-large) {
		font-size: 1.5em;
	}
</style>

