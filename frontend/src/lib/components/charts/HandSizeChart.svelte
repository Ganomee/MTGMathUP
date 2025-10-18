<script lang="ts">
	import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip, Legend } from 'recharts';

	export let data: Array<{ name: string; value: number; color?: string }>;
	export let title: string = 'Hand Size Distribution';

	const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4'];

	function getColor(index: number, item: any): string {
		return item.color || COLORS[index % COLORS.length];
	}

	function formatPieLabel({ name, percent }: { name: string; percent: number }): string {
		return `${name}: ${(percent * 100).toFixed(0)}%`;
	}

	function formatTooltipValue(value: number): [number, string] {
		return [value, 'Hands'];
	}

	function formatTooltipLabel(label: string): string {
		return `Size: ${label}`;
	}
</script>

<div class="card">
	<h3 class="text-lg font-semibold mb-4">{title}</h3>
	<div class="h-64">
		<ResponsiveContainer width="100%" height="100%">
			<PieChart>
				<Pie
					data={data}
					cx="50%"
					cy="50%"
					labelLine={false}
					label={formatPieLabel}
					outerRadius={80}
					fill="#8884d8"
					dataKey="value"
				>
					{#each data as item, index}
						<Cell key={`cell-${index}`} fill={getColor(index, item)} />
					{/each}
				</Pie>
				<Tooltip 
					formatter={formatTooltipValue}
					labelFormatter={formatTooltipLabel}
				/>
				<Legend />
			</PieChart>
		</ResponsiveContainer>
	</div>
</div>


