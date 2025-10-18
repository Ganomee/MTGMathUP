<script lang="ts">
	import ManaSymbol from '$lib/components/ManaSymbol.svelte';
	import MtgCard from '$lib/components/MtgCard.svelte';
	// Don't import ScryfallCardSearch on component page - it has browser issues in build
	import type { Card } from '@scryfall/api-types';

	// Component state toggles
	let manaSymbolSize: 'small' | 'medium' | 'large' = 'medium';
	let manaSymbolText = '{2}{U}{U}';
	let cardSize: 'small' | 'medium' | 'large' = 'medium';

	// Sample data
	const sampleCard: Partial<Card> = {
		id: '1',
		name: 'Counterspell',
		mana_cost: '{U}{U}',
		type_line: 'Instant',
		oracle_text: 'Counter target spell.',
		cmc: 2,
		colors: ['U'],
		image_uris: {
			small: 'https://cards.scryfall.io/small/front/a/4/a457f08e-b88c-4363-b3b4-0a87f6344e79.jpg',
			normal: 'https://cards.scryfall.io/normal/front/a/4/a457f08e-b88c-4363-b3b4-0a87f6344e79.jpg',
			large: 'https://cards.scryfall.io/large/front/a/4/a457f08e-b88c-4363-b3b4-0a87f6344e79.jpg',
			png: 'https://cards.scryfall.io/png/front/a/4/a457f08e-b88c-4363-b3b4-0a87f6344e79.png',
			art_crop: 'https://cards.scryfall.io/art_crop/front/a/4/a457f08e-b88c-4363-b3b4-0a87f6344e79.jpg',
			border_crop: 'https://cards.scryfall.io/border_crop/front/a/4/a457f08e-b88c-4363-b3b4-0a87f6344e79.jpg'
		}
	} as Card;

	const sampleCreature: Partial<Card> = {
		id: '2',
		name: 'Grizzly Bears',
		mana_cost: '{1}{G}',
		type_line: 'Creature — Bear',
		oracle_text: 'A bear is a bear.',
		power: '2',
		toughness: '2',
		cmc: 2,
		colors: ['G'],
		image_uris: {
			small: 'https://cards.scryfall.io/small/front/4/0/409f9b88-f03e-40b6-9883-68c14c37c0de.jpg',
			normal: 'https://cards.scryfall.io/normal/front/4/0/409f9b88-f03e-40b6-9883-68c14c37c0de.jpg',
			large: 'https://cards.scryfall.io/large/front/4/0/409f9b88-f03e-40b6-9883-68c14c37c0de.jpg',
			png: 'https://cards.scryfall.io/png/front/4/0/409f9b88-f03e-40b6-9883-68c14c37c0de.png',
			art_crop: 'https://cards.scryfall.io/art_crop/front/4/0/409f9b88-f03e-40b6-9883-68c14c37c0de.jpg',
			border_crop: 'https://cards.scryfall.io/border_crop/front/4/0/409f9b88-f03e-40b6-9883-68c14c37c0de.jpg'
		}
	} as Card;

	const manaExamples = [
		{ label: 'Colorless', text: '{2}{3}{4}' },
		{ label: 'Monocolored', text: '{W}{U}{B}{R}{G}' },
		{ label: 'Hybrid', text: '{W/U}{U/B}{B/R}{R/G}{G/W}' },
		{ label: 'Phyrexian', text: '{W/P}{U/P}{B/P}{R/P}{G/P}' },
		{ label: 'Mixed', text: '{2}{R}{R/G}{G/P}' },
		{ label: 'Special', text: '{T}{Q}{E}{C}{S}{X}' },
		{ label: 'Complex', text: '{T}: Add {G}{G}. Pay {2}{G/P}: Target creature gets +2/+2' }
	];
</script>

<svelte:head>
	<title>Component Library - MTG Mullagain</title>
</svelte:head>

<div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
	<!-- Header -->
	<div class="mb-8">
		<div class="flex items-center justify-between mb-4">
			<h1 class="text-4xl font-bold text-gray-900">Component Library</h1>
			<a 
				href="/" 
				class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition"
			>
				← Back to App
			</a>
		</div>
		<p class="text-gray-600">
			Interactive showcase of all custom components and common UI elements
		</p>
	</div>

	<!-- Navigation -->
	<div class="mb-8 bg-white rounded-lg shadow-sm p-4">
		<nav class="flex flex-wrap gap-2">
			<a href="#mana-symbols" class="px-4 py-2 bg-gray-100 hover:bg-gray-200 rounded-lg transition">Mana Symbols</a>
			<a href="#mtg-cards" class="px-4 py-2 bg-gray-100 hover:bg-gray-200 rounded-lg transition">MTG Cards</a>
			<a href="#search" class="px-4 py-2 bg-gray-100 hover:bg-gray-200 rounded-lg transition">Card Search</a>
			<a href="#buttons" class="px-4 py-2 bg-gray-100 hover:bg-gray-200 rounded-lg transition">Buttons</a>
			<a href="#tables" class="px-4 py-2 bg-gray-100 hover:bg-gray-200 rounded-lg transition">Tables</a>
			<a href="#forms" class="px-4 py-2 bg-gray-100 hover:bg-gray-200 rounded-lg transition">Forms</a>
		</nav>
	</div>

	<!-- MTG Components Section -->
	<div class="space-y-8">
		<!-- Mana Symbols -->
		<section id="mana-symbols" class="bg-white rounded-lg shadow-md p-6">
			<h2 class="text-2xl font-bold text-gray-900 mb-4">🔮 Mana Symbol Component</h2>
			<p class="text-gray-600 mb-6">Renders MTG mana symbols from text patterns like <code class="bg-gray-100 px-2 py-1 rounded">{'{R}'}</code></p>

			<!-- Controls -->
			<div class="mb-6 p-4 bg-gray-50 rounded-lg">
				<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
					<div>
						<label class="block text-sm font-medium text-gray-700 mb-2">Text Input:</label>
						<input
							type="text"
							bind:value={manaSymbolText}
							class="input"
							placeholder="{'{2}{U}{U}'}"
						/>
					</div>
					<div>
						<label class="block text-sm font-medium text-gray-700 mb-2">Size:</label>
						<select bind:value={manaSymbolSize} class="input">
							<option value="small">Small</option>
							<option value="medium">Medium</option>
							<option value="large">Large</option>
						</select>
					</div>
				</div>
			</div>

			<!-- Preview -->
			<div class="mb-6 p-6 bg-gradient-to-br from-blue-50 to-purple-50 rounded-lg border-2 border-blue-200">
				<div class="text-center">
					<ManaSymbol text={manaSymbolText} size={manaSymbolSize} />
				</div>
			</div>

			<!-- Examples -->
			<div>
				<h3 class="text-lg font-semibold text-gray-900 mb-3">Examples:</h3>
				<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
					{#each manaExamples as example}
						<div class="p-4 bg-gray-50 rounded-lg border border-gray-200">
							<div class="text-sm font-medium text-gray-700 mb-2">{example.label}:</div>
							<div class="flex items-center gap-2">
								<code class="text-xs bg-white px-2 py-1 rounded flex-1">{example.text}</code>
								<div>
									<ManaSymbol text={example.text} size="small" inline={true} />
								</div>
							</div>
						</div>
					{/each}
				</div>
			</div>
		</section>

		<!-- MTG Card Component -->
		<section id="mtg-cards" class="bg-white rounded-lg shadow-md p-6">
			<h2 class="text-2xl font-bold text-gray-900 mb-4">🃏 MTG Card Component</h2>
			<p class="text-gray-600 mb-6">Professional card display with text and image modes, hover effects, and Scryfall integration</p>

			<!-- Controls -->
			<div class="mb-6 p-4 bg-gray-50 rounded-lg">
				<label class="block text-sm font-medium text-gray-700 mb-2">Size:</label>
				<select bind:value={cardSize} class="input max-w-xs">
					<option value="small">Small</option>
					<option value="medium">Medium</option>
					<option value="large">Large</option>
				</select>
			</div>

			<!-- Preview -->
			<div class="mb-6">
				<h3 class="text-lg font-semibold text-gray-900 mb-3">Instant Card:</h3>
				<div class="flex flex-wrap gap-6 p-6 bg-gradient-to-br from-blue-50 to-indigo-50 rounded-lg border-2 border-blue-200">
					<div>
						<p class="text-sm text-gray-600 mb-2">With Image Loading:</p>
						<MtgCard card={sampleCard} size={cardSize} />
					</div>
					<div>
						<p class="text-sm text-gray-600 mb-2">Text Only (no images):</p>
						<MtgCard 
							card={{...sampleCard, image_uris: undefined}} 
							size={cardSize} 
						/>
					</div>
				</div>
			</div>

			<div>
				<h3 class="text-lg font-semibold text-gray-900 mb-3">Creature Card:</h3>
				<div class="flex flex-wrap gap-6 p-6 bg-gradient-to-br from-green-50 to-emerald-50 rounded-lg border-2 border-green-200">
					<div>
						<p class="text-sm text-gray-600 mb-2">With Image:</p>
						<MtgCard card={sampleCreature} size={cardSize} />
					</div>
					<div>
						<p class="text-sm text-gray-600 mb-2">Text Only:</p>
						<MtgCard 
							card={{...sampleCreature, image_uris: undefined}} 
							size={cardSize} 
						/>
					</div>
				</div>
			</div>
		</section>

		<!-- Scryfall Card Search -->
		<section id="search" class="bg-white rounded-lg shadow-md p-6">
			<h2 class="text-2xl font-bold text-gray-900 mb-4">🔍 Scryfall Card Search</h2>
			<p class="text-gray-600 mb-6">Powerful search component with full Scryfall syntax support and debounced searching</p>

			<div class="p-4 bg-blue-50 border border-blue-200 rounded-lg">
				<p class="text-sm text-blue-900">
					<strong>Note:</strong> The ScryfallCardSearch component is available in the app at 
					<code class="bg-blue-100 px-2 py-1 rounded">/import</code> page.
					It uses the Scryfall API which requires server-side execution.
				</p>
				<div class="mt-4">
					<h3 class="font-semibold text-blue-900 mb-2">Features:</h3>
					<ul class="list-disc list-inside text-sm text-blue-800 space-y-1">
						<li>Full Scryfall syntax support (e.g., <code>t:instant c:red</code>)</li>
						<li>Debounced search for performance</li>
						<li>Results with mana symbols</li>
						<li>Card selection callback</li>
						<li>Configurable max results</li>
					</ul>
				</div>
			</div>
		</section>

		<!-- Buttons -->
		<section id="buttons" class="bg-white rounded-lg shadow-md p-6">
			<h2 class="text-2xl font-bold text-gray-900 mb-4">🔘 Buttons</h2>
			<p class="text-gray-600 mb-6">Common button styles used throughout the application</p>

			<div class="space-y-6">
				<!-- Primary Buttons -->
				<div>
					<h3 class="text-lg font-semibold text-gray-900 mb-3">Primary Buttons:</h3>
					<div class="flex flex-wrap gap-4">
						<button class="btn btn-primary">Primary Button</button>
						<button class="btn btn-primary" disabled>Disabled</button>
						<button class="btn btn-primary text-sm">Small Primary</button>
						<button class="btn btn-primary text-lg px-6 py-3">Large Primary</button>
					</div>
				</div>

				<!-- Secondary Buttons -->
				<div>
					<h3 class="text-lg font-semibold text-gray-900 mb-3">Secondary Buttons:</h3>
					<div class="flex flex-wrap gap-4">
						<button class="btn btn-secondary">Secondary Button</button>
						<button class="btn btn-secondary" disabled>Disabled</button>
						<button class="btn btn-secondary text-sm">Small Secondary</button>
						<button class="btn btn-secondary text-lg px-6 py-3">Large Secondary</button>
					</div>
				</div>

				<!-- Colored Buttons -->
				<div>
					<h3 class="text-lg font-semibold text-gray-900 mb-3">Colored Variants:</h3>
					<div class="flex flex-wrap gap-4">
						<button class="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition">
							Success
						</button>
						<button class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition">
							Danger
						</button>
						<button class="px-4 py-2 bg-yellow-500 text-white rounded-lg hover:bg-yellow-600 transition">
							Warning
						</button>
						<button class="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition">
							Info
						</button>
					</div>
				</div>

				<!-- Icon Buttons -->
				<div>
					<h3 class="text-lg font-semibold text-gray-900 mb-3">With Icons:</h3>
					<div class="flex flex-wrap gap-4">
						<button class="btn btn-primary">✨ Add New</button>
						<button class="btn bg-yellow-100 text-yellow-800 hover:bg-yellow-200 border border-yellow-300">
							✏️ Edit
						</button>
						<button class="btn bg-red-100 text-red-800 hover:bg-red-200 border border-red-300">
							🗑️ Delete
						</button>
						<button class="btn bg-blue-100 text-blue-800 hover:bg-blue-200 border border-blue-300">
							🔄 Refresh
						</button>
					</div>
				</div>
			</div>
		</section>

		<!-- Tables -->
		<section id="tables" class="bg-white rounded-lg shadow-md p-6">
			<h2 class="text-2xl font-bold text-gray-900 mb-4">📊 Tables</h2>
			<p class="text-gray-600 mb-6">Data table styles used in the admin panel</p>

			<div class="overflow-x-auto">
				<table class="w-full">
					<thead class="bg-gray-100 border-b-2 border-gray-200">
						<tr>
							<th class="px-4 py-3 text-left text-sm font-semibold text-gray-700">ID</th>
							<th class="px-4 py-3 text-left text-sm font-semibold text-gray-700">Card Name</th>
							<th class="px-4 py-3 text-left text-sm font-semibold text-gray-700">Mana Cost</th>
							<th class="px-4 py-3 text-left text-sm font-semibold text-gray-700">Type</th>
							<th class="px-4 py-3 text-left text-sm font-semibold text-gray-700">CMC</th>
							<th class="px-4 py-3 text-left text-sm font-semibold text-gray-700">Actions</th>
						</tr>
					</thead>
					<tbody>
						<tr class="border-b border-gray-200 hover:bg-gray-50 transition">
							<td class="px-4 py-3 text-sm text-gray-700">1</td>
							<td class="px-4 py-3 text-sm text-gray-700">Lightning Bolt</td>
							<td class="px-4 py-3 text-sm text-gray-700">
								<ManaSymbol text={"{R}"} size="small" inline={true} />
							</td>
							<td class="px-4 py-3 text-sm text-gray-700">Instant</td>
							<td class="px-4 py-3 text-sm text-gray-700">1</td>
							<td class="px-4 py-3 text-sm">
								<div class="flex space-x-2">
									<button class="px-3 py-1 bg-yellow-100 text-yellow-800 rounded text-xs hover:bg-yellow-200 transition">
										✏️ Edit
									</button>
									<button class="px-3 py-1 bg-red-100 text-red-800 rounded text-xs hover:bg-red-200 transition">
										🗑️ Delete
									</button>
								</div>
							</td>
						</tr>
						<tr class="border-b border-gray-200 hover:bg-gray-50 transition">
							<td class="px-4 py-3 text-sm text-gray-700">2</td>
							<td class="px-4 py-3 text-sm text-gray-700">Counterspell</td>
							<td class="px-4 py-3 text-sm text-gray-700">
								<ManaSymbol text={"{U}{U}"} size="small" inline={true} />
							</td>
							<td class="px-4 py-3 text-sm text-gray-700">Instant</td>
							<td class="px-4 py-3 text-sm text-gray-700">2</td>
							<td class="px-4 py-3 text-sm">
								<div class="flex space-x-2">
									<button class="px-3 py-1 bg-yellow-100 text-yellow-800 rounded text-xs hover:bg-yellow-200 transition">
										✏️ Edit
									</button>
									<button class="px-3 py-1 bg-red-100 text-red-800 rounded text-xs hover:bg-red-200 transition">
										🗑️ Delete
									</button>
								</div>
							</td>
						</tr>
						<tr class="border-b border-gray-200 hover:bg-gray-50 transition">
							<td class="px-4 py-3 text-sm text-gray-700">3</td>
							<td class="px-4 py-3 text-sm text-gray-700">Grizzly Bears</td>
							<td class="px-4 py-3 text-sm text-gray-700">
								<ManaSymbol text={"{1}{G}"} size="small" inline={true} />
							</td>
							<td class="px-4 py-3 text-sm text-gray-700">Creature</td>
							<td class="px-4 py-3 text-sm text-gray-700">2</td>
							<td class="px-4 py-3 text-sm">
								<div class="flex space-x-2">
									<button class="px-3 py-1 bg-yellow-100 text-yellow-800 rounded text-xs hover:bg-yellow-200 transition">
										✏️ Edit
									</button>
									<button class="px-3 py-1 bg-red-100 text-red-800 rounded text-xs hover:bg-red-200 transition">
										🗑️ Delete
									</button>
								</div>
							</td>
						</tr>
					</tbody>
				</table>
			</div>
		</section>

		<!-- Forms -->
		<section id="forms" class="bg-white rounded-lg shadow-md p-6">
			<h2 class="text-2xl font-bold text-gray-900 mb-4">📝 Form Elements</h2>
			<p class="text-gray-600 mb-6">Common form inputs and controls</p>

			<div class="space-y-6 max-w-2xl">
				<!-- Text Input -->
				<div>
					<label class="block text-sm font-medium text-gray-700 mb-2">Text Input:</label>
					<input type="text" class="input" placeholder="Enter text..." />
				</div>

				<!-- Textarea -->
				<div>
					<label class="block text-sm font-medium text-gray-700 mb-2">Textarea:</label>
					<textarea class="input" rows="3" placeholder="Enter multiple lines..."></textarea>
				</div>

				<!-- Select -->
				<div>
					<label class="block text-sm font-medium text-gray-700 mb-2">Select:</label>
					<select class="input">
						<option>Option 1</option>
						<option>Option 2</option>
						<option>Option 3</option>
					</select>
				</div>

				<!-- Number Input -->
				<div>
					<label class="block text-sm font-medium text-gray-700 mb-2">Number Input:</label>
					<input type="number" class="input" value="0" min="0" />
				</div>

				<!-- Checkbox -->
				<div>
					<label class="flex items-center space-x-2 cursor-pointer">
						<input type="checkbox" class="w-4 h-4 text-blue-600 rounded focus:ring-2 focus:ring-blue-500" />
						<span class="text-sm font-medium text-gray-700">Checkbox Option</span>
					</label>
				</div>

				<!-- Radio Buttons -->
				<div>
					<label class="block text-sm font-medium text-gray-700 mb-2">Radio Buttons:</label>
					<div class="space-y-2">
						<label class="flex items-center space-x-2 cursor-pointer">
							<input type="radio" name="radio-group" class="w-4 h-4 text-blue-600 focus:ring-2 focus:ring-blue-500" checked />
							<span class="text-sm text-gray-700">Option A</span>
						</label>
						<label class="flex items-center space-x-2 cursor-pointer">
							<input type="radio" name="radio-group" class="w-4 h-4 text-blue-600 focus:ring-2 focus:ring-blue-500" />
							<span class="text-sm text-gray-700">Option B</span>
						</label>
					</div>
				</div>

				<!-- Form with Validation -->
				<div class="p-4 bg-gray-50 rounded-lg border border-gray-200">
					<h3 class="text-sm font-semibold text-gray-900 mb-3">Form Example:</h3>
					<form class="space-y-4" on:submit|preventDefault>
						<div>
							<label class="block text-sm font-medium text-gray-700 mb-1">Required Field:</label>
							<input type="text" class="input" required placeholder="This field is required" />
						</div>
						<div>
							<label class="block text-sm font-medium text-gray-700 mb-1">Email:</label>
							<input type="email" class="input" placeholder="email@example.com" />
						</div>
						<div class="flex gap-2">
							<button type="submit" class="btn btn-primary">Submit</button>
							<button type="reset" class="btn btn-secondary">Reset</button>
						</div>
					</form>
				</div>
			</div>
		</section>

		<!-- Cards/Containers -->
		<section id="cards" class="bg-white rounded-lg shadow-md p-6">
			<h2 class="text-2xl font-bold text-gray-900 mb-4">📦 Cards & Containers</h2>
			<p class="text-gray-600 mb-6">Various card and container styles</p>

			<div class="grid grid-cols-1 md:grid-cols-2 gap-6">
				<!-- Standard Card -->
				<div class="card">
					<h3 class="text-lg font-semibold text-gray-900 mb-2">Standard Card</h3>
					<p class="text-gray-600">This is a standard card with the <code class="bg-gray-100 px-2 py-1 rounded text-xs">card</code> class.</p>
				</div>

				<!-- Colored Card -->
				<div class="bg-blue-50 rounded-lg shadow-sm border border-blue-200 p-6">
					<h3 class="text-lg font-semibold text-blue-900 mb-2">Colored Card</h3>
					<p class="text-blue-700">This is a card with custom colored styling.</p>
				</div>

				<!-- Success Card -->
				<div class="bg-green-50 rounded-lg shadow-sm border border-green-200 p-6">
					<h3 class="text-lg font-semibold text-green-900 mb-2">✅ Success State</h3>
					<p class="text-green-700">Operation completed successfully!</p>
				</div>

				<!-- Error Card -->
				<div class="bg-red-50 rounded-lg shadow-sm border border-red-200 p-6">
					<h3 class="text-lg font-semibold text-red-900 mb-2">❌ Error State</h3>
					<p class="text-red-700">Something went wrong. Please try again.</p>
				</div>
			</div>
		</section>
	</div>

	<!-- Footer -->
	<div class="mt-12 text-center text-gray-500 text-sm">
		<p>Component Library • MTG Mullagain • All tests passing ✅</p>
	</div>
</div>

<style>
	code {
		font-family: 'Courier New', monospace;
	}
</style>

