import adapter from '@sveltejs/adapter-static';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	kit: {
		// Use static adapter for PWA deployment
		adapter: adapter({
			pages: 'build',
			assets: 'build',
			fallback: 'index.html',
			precompress: false,
			strict: true
		}),
		// Enable PWA features
		serviceWorker: {
			register: false
		},
		// Configure paths
		paths: {
			base: ''
		}
	}
};

export default config;
