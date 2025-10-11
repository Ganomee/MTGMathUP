/**
 * API client for MTG Mullagain backend
 */

const API_BASE = import.meta.env.VITE_API_BASE || 'http://localhost:5000/api';

export interface Hand {
  id: number;
  cardIntIds: number[];
  size: number;
  hash64: number;
  canonicalKey: string;
  createdAt: string;
  similarityScore?: number;
  sharedCardsCount?: number;
}

export interface PaginatedHandResponse {
  hands: Hand[];
  totalCount: number;
  offset: number;
  limit: number;
  hasMore: boolean;
}

export interface HandQueryRequest {
  cardIntIds: number[];
  minSharedCards?: number;
  limit?: number;
  offset?: number;
}

export interface HandSimilarityRequest {
  handId: number;
  limit?: number;
  threshold?: number;
}

export interface HandEvaluationRequest {
  hand1Id: number;
  hand2Id: number;
  preferredHand: number;
  context?: Record<string, any>;
}

export interface Deck {
  id: number;
  name: string;
  format: string;
  owner?: string;
  createdAt: string;
  updatedAt: string;
}

export interface Card {
  id: string;
  name: string;
  manaCost: string;
  oracleText: string;
  type: string;
  powerToughness?: string;
  cmc: number;
  colors: string[];
  createdAt: string;
  updatedAt: string;
}

class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string = API_BASE) {
    this.baseUrl = baseUrl;
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    
    const config: RequestInit = {
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
      ...options,
    };

    try {
      const response = await fetch(url, config);
      
      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`API Error ${response.status}: ${errorText}`);
      }

      return await response.json();
    } catch (error) {
      console.error(`API request failed for ${endpoint}:`, error);
      throw error;
    }
  }

  // Hand endpoints
  async findEqualHands(cardIntIds: number[], limit = 50, offset = 0): Promise<PaginatedHandResponse> {
    return this.request<PaginatedHandResponse>('/hands/equal', {
      method: 'POST',
      body: JSON.stringify(cardIntIds),
    });
  }

  async findHandsContaining(request: HandQueryRequest): Promise<PaginatedHandResponse> {
    return this.request<PaginatedHandResponse>('/hands/contains', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async findOverlappingHands(request: HandQueryRequest): Promise<PaginatedHandResponse> {
    return this.request<PaginatedHandResponse>('/hands/overlap', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async findSimilarHands(request: HandSimilarityRequest): Promise<PaginatedHandResponse> {
    return this.request<PaginatedHandResponse>('/hands/similar', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async generateRandomHand(deckId: number, handSize = 7): Promise<Hand> {
    return this.request<Hand>(`/hands/generate?deckId=${deckId}&handSize=${handSize}`, {
      method: 'POST',
    });
  }

  async getHand(id: number): Promise<Hand> {
    return this.request<Hand>(`/hands/${id}`);
  }

  async evaluateHands(request: HandEvaluationRequest): Promise<void> {
    return this.request<void>('/hands/evaluate', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  // Deck endpoints
  async getDecks(): Promise<Deck[]> {
    return this.request<Deck[]>('/decks');
  }

  async getDeck(id: number): Promise<Deck> {
    return this.request<Deck>(`/decks/${id}`);
  }

  async createDeck(deck: Partial<Deck>): Promise<Deck> {
    return this.request<Deck>('/decks', {
      method: 'POST',
      body: JSON.stringify(deck),
    });
  }

  // Card endpoints
  async getCards(search?: string, limit = 50, offset = 0): Promise<Card[]> {
    const params = new URLSearchParams();
    if (search) params.append('search', search);
    params.append('limit', limit.toString());
    params.append('offset', offset.toString());
    
    return this.request<Card[]>(`/cards?${params.toString()}`);
  }

  async getCard(id: string): Promise<Card> {
    return this.request<Card>(`/cards/${id}`);
  }

  // Health check
  async healthCheck(): Promise<string> {
    return this.request<string>('/health');
  }
}

// Export singleton instance
export const apiClient = new ApiClient();

// Export types for use in components
export type { Hand, PaginatedHandResponse, HandQueryRequest, HandSimilarityRequest, HandEvaluationRequest, Deck, Card };


