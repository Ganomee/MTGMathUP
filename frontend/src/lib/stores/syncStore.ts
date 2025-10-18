import { writable, derived, type Writable } from 'svelte/store';
import * as signalR from '@microsoft/signalr';

export interface SyncStatus {
	isOnline: boolean;
	isSyncing: boolean;
	lastSyncTime: Date | null;
	pendingChanges: number;
	error: string | null;
}

export interface Deck {
	id: number;
	name: string;
	format: string;
	owner: string;
	createdAt: string;
	updatedAt: string;
}

// Store for sync status
export const syncStatus: Writable<SyncStatus> = writable({
	isOnline: false,
	isSyncing: false,
	lastSyncTime: null,
	pendingChanges: 0,
	error: null
});

// Store for decks
export const decks: Writable<Deck[]> = writable([]);

// Store for SignalR connection
export const connection: Writable<signalR.HubConnection | null> = writable(null);

// Backend URL - use environment variable or default
const BACKEND_URL = import.meta.env.VITE_BACKEND_URL || 'http://localhost:5000';

export async function initializeSync() {
	// Create SignalR connection
	const hubConnection = new signalR.HubConnectionBuilder()
		.withUrl(`${BACKEND_URL}/sync`, {
			withCredentials: false,
			transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents
		})
		.withAutomaticReconnect()
		.configureLogging(signalR.LogLevel.Information)
		.build();

	// Handle sync status updates
	hubConnection.on('SyncStatus', (status: any) => {
		console.log('Received sync status:', status);
		syncStatus.update((s) => ({
			...s,
			isOnline: status.IsOnline || status.isOnline,
			isSyncing: status.IsSyncing || status.isSyncing,
			lastSyncTime: status.LastSyncTime || status.lastSyncTime ? new Date(status.LastSyncTime || status.lastSyncTime) : null,
			pendingChanges: status.PendingChanges || status.pendingChanges || 0
		}));
	});

	// Handle reconnection
	hubConnection.onreconnecting(() => {
		console.log('Reconnecting to SignalR...');
		syncStatus.update((s) => ({ ...s, isOnline: false, error: 'Reconnecting...' }));
	});

	hubConnection.onreconnected(() => {
		console.log('Reconnected to SignalR');
		syncStatus.update((s) => ({ ...s, isOnline: true, error: null }));
	});

	hubConnection.onclose(() => {
		console.log('SignalR connection closed');
		syncStatus.update((s) => ({ ...s, isOnline: false, error: 'Connection closed' }));
	});

	// Start connection
	try {
		await hubConnection.start();
		console.log('SignalR connection established');
		connection.set(hubConnection);
		syncStatus.update((s) => ({ ...s, isOnline: true, error: null }));
	} catch (error) {
		console.error('Failed to connect to SignalR:', error);
		syncStatus.update((s) => ({
			...s,
			isOnline: false,
			error: error instanceof Error ? error.message : 'Connection failed'
		}));
	}

	return hubConnection;
}

export async function disconnectSync() {
	const conn = await new Promise<signalR.HubConnection | null>((resolve) => {
		connection.subscribe((c) => resolve(c))();
	});

	if (conn) {
		await conn.stop();
		connection.set(null);
		syncStatus.update((s) => ({ ...s, isOnline: false }));
	}
}

// API functions for backend communication
export async function fetchDecks(): Promise<Deck[]> {
	try {
		const response = await fetch(`${BACKEND_URL}/api/decks`);
		if (!response.ok) {
			throw new Error(`HTTP error! status: ${response.status}`);
		}
		const data = await response.json();
		decks.set(data);
		return data;
	} catch (error) {
		console.error('Failed to fetch decks:', error);
		syncStatus.update((s) => ({
			...s,
			error: error instanceof Error ? error.message : 'Failed to fetch decks'
		}));
		return [];
	}
}

export async function createDeck(deck: Omit<Deck, 'id' | 'createdAt' | 'updatedAt'>): Promise<Deck | null> {
	try {
		const response = await fetch(`${BACKEND_URL}/api/decks`, {
			method: 'POST',
			headers: {
				'Content-Type': 'application/json'
			},
			body: JSON.stringify(deck)
		});
		
		if (!response.ok) {
			throw new Error(`HTTP error! status: ${response.status}`);
		}
		
		const newDeck = await response.json();
		decks.update((d) => [...d, newDeck]);
		return newDeck;
	} catch (error) {
		console.error('Failed to create deck:', error);
		syncStatus.update((s) => ({
			...s,
			error: error instanceof Error ? error.message : 'Failed to create deck'
		}));
		return null;
	}
}

export async function updateDeck(id: number, deck: Partial<Deck>): Promise<Deck | null> {
	try {
		const response = await fetch(`${BACKEND_URL}/api/decks/${id}`, {
			method: 'PUT',
			headers: {
				'Content-Type': 'application/json'
			},
			body: JSON.stringify(deck)
		});
		
		if (!response.ok) {
			throw new Error(`HTTP error! status: ${response.status}`);
		}
		
		const updatedDeck = await response.json();
		decks.update((d) => d.map((deck) => (deck.id === id ? updatedDeck : deck)));
		return updatedDeck;
	} catch (error) {
		console.error('Failed to update deck:', error);
		syncStatus.update((s) => ({
			...s,
			error: error instanceof Error ? error.message : 'Failed to update deck'
		}));
		return null;
	}
}

export async function deleteDeck(id: number): Promise<boolean> {
	try {
		const response = await fetch(`${BACKEND_URL}/api/decks/${id}`, {
			method: 'DELETE'
		});
		
		if (!response.ok) {
			throw new Error(`HTTP error! status: ${response.status}`);
		}
		
		decks.update((d) => d.filter((deck) => deck.id !== id));
		return true;
	} catch (error) {
		console.error('Failed to delete deck:', error);
		syncStatus.update((s) => ({
			...s,
			error: error instanceof Error ? error.message : 'Failed to delete deck'
		}));
		return false;
	}
}

