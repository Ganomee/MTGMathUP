<script lang="ts">
  import { mlFeaturesAvailable } from '$lib/electric/store.js';

  export let feature: string;
  export let children: any;

  function getIndicatorColor() {
    return $mlFeaturesAvailable ? 'text-green-600' : 'text-gray-400';
  }

  function getIndicatorIcon() {
    return $mlFeaturesAvailable ? '✅' : '❌';
  }

  function getTooltipText() {
    return $mlFeaturesAvailable 
      ? `${feature} is available` 
      : `${feature} requires internet connection`;
  }
</script>

<div class="relative group">
  <!-- Feature Content -->
  <div class="{!$mlFeaturesAvailable ? 'opacity-50 pointer-events-none' : ''}">
    <slot />
  </div>

  <!-- ML Feature Indicator -->
  <div class="absolute -top-1 -right-1 bg-white rounded-full border border-gray-200 p-1 shadow-sm">
    <span 
      class="text-xs {getIndicatorColor()}"
      title={getTooltipText()}
    >
      {getIndicatorIcon()}
    </span>
  </div>

  <!-- Tooltip -->
  <div class="absolute bottom-full right-0 mb-2 px-2 py-1 bg-gray-800 text-white text-xs rounded opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap z-10">
    {getTooltipText()}
  </div>
</div>
