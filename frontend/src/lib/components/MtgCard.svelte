<script lang="ts">
	import { onMount } from 'svelte';
	import type { Card } from '@scryfall/api-types';
	import ManaSymbol from './ManaSymbol.svelte';
	import { getOrCacheImage } from '$lib/utils/image-cache';

	export let card: Card;
	export let size: 'small' | 'medium' | 'large' = 'medium';
	export let onClick: ((card: Card) => void) | undefined = undefined;

	let imageUrl: string | null = null;
	let imageLoaded = false;
	let showTextOverlay = false;

	// Size mapping for card dimensions
	const sizeClasses = {
		small: 'card-small w-48 h-64',
		medium: 'card-medium w-64 h-88',
		large: 'card-large w-80 h-112'
	};

	// Get appropriate image quality based on size
	function getImageUrl(): string | undefined {
		if (!card.image_uris) return undefined;
		
		switch (size) {
			case 'small':
				return card.image_uris.small;
			case 'large':
				return card.image_uris.large;
			case 'medium':
			default:
				return card.image_uris.normal;
		}
	}

	// Check if card is a creature
	function isCreature(): boolean {
		return card.type_line?.toLowerCase().includes('creature') || false;
	}

	// Load and cache image
	onMount(async () => {
		const imgUrl = getImageUrl();
		if (imgUrl) {
			const cachedUrl = await getOrCacheImage(imgUrl);
			if (cachedUrl) {
				imageUrl = cachedUrl;
				imageLoaded = true;
			}
		}
	});

	function handleClick() {
		if (onClick) {
			onClick(card);
		}
	}
</script>

<div
	class="mtg-card group relative rounded-lg overflow-hidden shadow-lg transition-all hover:shadow-xl {sizeClasses[size]} {onClick ? 'cursor-pointer hover:scale-105' : ''}"
	on:click={handleClick}
	on:keypress={(e) => e.key === 'Enter' && handleClick()}
	role={onClick ? 'button' : 'img'}
	tabindex={onClick ? 0 : undefined}
	aria-label={card.name}
>
	{#if imageLoaded && imageUrl}
		<!-- Image Display Mode -->
		<img
			src={imageUrl}
			alt={card.name}
			class="w-full h-full object-cover"
		/>
		
		<!-- Text Overlay on Hover -->
		<div 
			class="absolute inset-0 bg-black bg-opacity-90 p-4 opacity-0 group-hover:opacity-100 transition-opacity duration-200 overflow-y-auto"
			on:mouseenter={() => showTextOverlay = true}
			on:mouseleave={() => showTextOverlay = false}
			role="tooltip"
			aria-label="Card details"
		>
			<div class="text-white space-y-2">
				<!-- Card Name and Mana Cost -->
				<div class="flex justify-between items-start">
					<h3 class="font-bold text-lg flex-1">{card.name}</h3>
					{#if card.mana_cost}
						<div class="ml-2">
							<ManaSymbol text={card.mana_cost} size="small" inline={true} />
						</div>
					{/if}
				</div>

				<!-- Type Line -->
				<div class="text-sm text-gray-300 italic border-b border-gray-600 pb-2">
					{card.type_line}
				</div>

				<!-- Oracle Text -->
				{#if card.oracle_text}
					<div class="text-sm leading-relaxed">
						<ManaSymbol text={card.oracle_text} size="small" />
					</div>
				{/if}

				<!-- Power/Toughness for Creatures -->
				{#if isCreature() && card.power && card.toughness}
					<div class="text-right font-bold text-lg mt-auto pt-2">
						{card.power}/{card.toughness}
					</div>
				{/if}
			</div>
		</div>
	{:else}
		<!-- Text Display Mode (Initial/Fallback) -->
		<div class="card-text-display w-full h-full bg-gradient-to-br from-gray-100 to-gray-200 border-2 border-gray-400 rounded-lg p-4 flex flex-col justify-between">
			<!-- Card Header -->
			<div class="space-y-2">
				<!-- Name and Mana Cost -->
				<div class="flex justify-between items-start">
					<h3 class="font-bold text-sm flex-1">{card.name}</h3>
					{#if card.mana_cost}
						<div class="ml-2">
							<ManaSymbol text={card.mana_cost} size="small" inline={true} />
						</div>
					{/if}
				</div>

				<!-- Type Line -->
				<div class="text-xs text-gray-700 italic border-b border-gray-400 pb-1">
					{card.type_line}
				</div>
			</div>

			<!-- Oracle Text -->
			{#if card.oracle_text}
				<div class="flex-1 overflow-y-auto py-2">
					<div class="text-xs leading-relaxed">
						<ManaSymbol text={card.oracle_text} size="small" />
					</div>
				</div>
			{/if}

			<!-- Power/Toughness for Creatures -->
			{#if isCreature() && card.power && card.toughness}
				<div class="text-right font-bold text-lg mt-2 pt-2 border-t border-gray-400">
					{card.power}/{card.toughness}
				</div>
			{/if}

			<!-- Loading Indicator -->
			{#if !imageLoaded && getImageUrl()}
				<div class="absolute bottom-2 right-2 text-xs text-gray-500">
					Loading image...
				</div>
			{/if}
		</div>
	{/if}
</div>

<style>
	.mtg-card {
		user-select: none;
	}

	/* Ensure proper aspect ratio for MTG cards (typically 2.5:3.5) */
	.card-small {
		aspect-ratio: 5 / 7;
	}

	.card-medium {
		aspect-ratio: 5 / 7;
	}

	.card-large {
		aspect-ratio: 5 / 7;
	}
</style>

