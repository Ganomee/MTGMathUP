<script lang="ts">
	import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

	export let data: Array<{ date: string; evaluations: number; cumulative: number }>;
	export let title: string = 'Evaluation Trend';

	// Format date for display
	function formatDate(dateStr: string): string {
		const date = new Date(dateStr);
		return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
	}

	function formatTooltipValue(value: number, name: string): [number, string] {
		return [value, name === 'evaluations' ? 'Daily Evaluations' : 'Total Evaluations'];
	}

	function formatTooltipLabel(label: string): string {
		return `Date: ${formatDate(label)}`;
	}
</script>

<div class="card">
	<h3 class="text-lg font-semibold mb-4">{title}</h3>
	<div class="h-64">
		<ResponsiveContainer width="100%" height="100%">
			<LineChart data={data} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
				<CartesianGrid strokeDasharray="3 3" />
				<XAxis 
					dataKey="date" 
					tickFormatter={formatDate}
					tick={{ fontSize: 12 }}
				/>
				<YAxis />
				<Tooltip 
					formatter={formatTooltipValue}
					labelFormatter={formatTooltipLabel}
				/>
				<Line 
					type="monotone" 
					dataKey="evaluations" 
					stroke="#3b82f6" 
					strokeWidth={2}
					name="Daily"
				/>
				<Line 
					type="monotone" 
					dataKey="cumulative" 
					stroke="#10b981" 
					strokeWidth={2}
					name="Cumulative"
				/>
			</LineChart>
		</ResponsiveContainer>
	</div>
</div>


