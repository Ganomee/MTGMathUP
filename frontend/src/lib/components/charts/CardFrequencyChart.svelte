<script lang="ts">
	import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

	export let data: Array<{ name: string; count: number; color?: string }>;
	export let title: string = 'Card Frequency';

	// Color mapping for MTG colors
	const colorMap: Record<string, string> = {
		red: '#dc2626',
		blue: '#2563eb',
		green: '#16a34a',
		white: '#f3f4f6',
		black: '#374151',
		multicolor: '#7c3aed',
		artifact: '#6b7280',
		land: '#92400e'
	};

	function getColor(item: any): string {
		return item.color || colorMap[item.name.toLowerCase()] || '#3b82f6';
	}
</script>

<div class="card">
	<h3 class="text-lg font-semibold mb-4">{title}</h3>
	<div class="h-64">
		<ResponsiveContainer width="100%" height="100%">
			<BarChart data={data} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
				<CartesianGrid strokeDasharray="3 3" />
				<XAxis 
					dataKey="name" 
					tick={{ fontSize: 12 }}
					angle={-45}
					textAnchor="end"
					height={60}
				/>
				<YAxis />
				<Tooltip 
					formatter={(value: number) => [value, 'Count']}
					labelFormatter={(label: string) => `Card: ${label}`}
				/>
				<Bar 
					dataKey="count" 
					fill={(entry: any) => getColor(entry)}
					radius={[4, 4, 0, 0]}
				/>
			</BarChart>
		</ResponsiveContainer>
	</div>
</div>


