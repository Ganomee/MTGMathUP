import { getPGlite } from './pglite-client'
import { v4 as uuidv4 } from 'uuid'

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

export interface Deck {
  id: string
  name: string
  format: string | null
  owner: string | null
  created_at: Date
  updated_at: Date
}

export interface Card {
  id: string
  deck_id: string
  name: string
  card_type: string | null
  mana_cost: string | null
  quantity: number
  created_at: Date
}

export interface Hand {
  id: string
  deck_id: string
  cards: any // JSONB
  on_play: boolean
  created_at: Date
}

export interface HandEval {
  id: string
  hand_id: string
  score: number | null
  reasoning: string | null
  created_at: Date
}

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

/**
 * Queue a change for server synchronization
 */
async function queueChange(
  tableName: string,
  operation: 'INSERT' | 'UPDATE' | 'DELETE',
  recordId: string,
  data: any
): Promise<void> {
  const db = getPGlite()
  
  await db.query(
    `INSERT INTO sync_queue (table_name, operation, record_id, data) 
     VALUES ($1, $2, $3, $4)`,
    [tableName, operation, recordId, JSON.stringify(data)]
  )
  
  console.log(`📝 Queued ${operation} for ${tableName}/${recordId}`)
}

// ============================================================================
// DECK OPERATIONS
// ============================================================================

export async function createDeck(data: Omit<Deck, 'id' | 'created_at' | 'updated_at'>): Promise<Deck> {
  const db = getPGlite()
  
  // Decks use auto-increment BIGINT, not UUID
  const result = await db.query(
    `INSERT INTO decks (name, format, owner) 
     VALUES ($1, $2, $3) 
     RETURNING *`,
    [data.name, data.format || null, data.owner || null]
  )
  
  const deck = result.rows[0] as Deck
  await queueChange('decks', 'INSERT', String(deck.id), deck)
  
  return deck
}

export async function getDecks(): Promise<Deck[]> {
  const db = getPGlite()
  const result = await db.query('SELECT * FROM decks ORDER BY created_at DESC')
  return result.rows as Deck[]
}

export async function getDeck(id: string): Promise<Deck | null> {
  const db = getPGlite()
  const result = await db.query('SELECT * FROM decks WHERE id = $1', [id])
  return (result.rows[0] as Deck) || null
}

export async function updateDeck(id: string, data: Partial<Omit<Deck, 'id' | 'created_at'>>): Promise<Deck> {
  const db = getPGlite()
  
  const fields: string[] = []
  const values: any[] = []
  let paramIndex = 1
  
  if (data.name !== undefined) {
    fields.push(`name = $${paramIndex++}`)
    values.push(data.name)
  }
  if (data.format !== undefined) {
    fields.push(`format = $${paramIndex++}`)
    values.push(data.format)
  }
  if (data.owner !== undefined) {
    fields.push(`owner = $${paramIndex++}`)
    values.push(data.owner)
  }
  
  fields.push(`updated_at = NOW()`)
  values.push(id)
  
  const result = await db.query(
    `UPDATE decks SET ${fields.join(', ')} WHERE id = $${paramIndex} RETURNING *`,
    values
  )
  
  const deck = result.rows[0] as Deck
  await queueChange('decks', 'UPDATE', id, deck)
  
  return deck
}

export async function deleteDeck(id: string): Promise<void> {
  const db = getPGlite()
  
  await db.query('DELETE FROM decks WHERE id = $1', [id])
  await queueChange('decks', 'DELETE', id, null)
}

// ============================================================================
// CARD OPERATIONS
// ============================================================================

export async function createCard(data: Omit<Card, 'id' | 'created_at'>): Promise<Card> {
  const db = getPGlite()
  const id = uuidv4()
  
  // Cards use UUID - insert the fields that actually exist in the schema
  const result = await db.query(
    `INSERT INTO cards (id, name, mana_cost, oracle_text, type, power_toughness, cmc, colors) 
     VALUES ($1, $2, $3, $4, $5, $6, $7, $8) 
     RETURNING *`,
    [
      id, 
      data.name, 
      data.mana_cost || '', 
      data.oracle_text || '', 
      data.type || '', 
      data.power_toughness || null,
      data.cmc || 0,
      data.colors || []
    ]
  )
  
  const card = result.rows[0] as Card
  await queueChange('cards', 'INSERT', id, card)
  
  return card
}

export async function getCards(deckId?: string): Promise<Card[]> {
  const db = getPGlite()
  
  if (deckId) {
    const result = await db.query('SELECT * FROM cards WHERE deck_id = $1 ORDER BY name', [deckId])
    return result.rows as Card[]
  } else {
    const result = await db.query('SELECT * FROM cards ORDER BY name')
    return result.rows as Card[]
  }
}

export async function updateCard(id: string, data: Partial<Omit<Card, 'id' | 'created_at'>>): Promise<Card> {
  const db = getPGlite()
  
  const fields: string[] = []
  const values: any[] = []
  let paramIndex = 1
  
  if (data.name !== undefined) {
    fields.push(`name = $${paramIndex++}`)
    values.push(data.name)
  }
  if (data.card_type !== undefined) {
    fields.push(`card_type = $${paramIndex++}`)
    values.push(data.card_type)
  }
  if (data.mana_cost !== undefined) {
    fields.push(`mana_cost = $${paramIndex++}`)
    values.push(data.mana_cost)
  }
  if (data.quantity !== undefined) {
    fields.push(`quantity = $${paramIndex++}`)
    values.push(data.quantity)
  }
  
  values.push(id)
  
  const result = await db.query(
    `UPDATE cards SET ${fields.join(', ')} WHERE id = $${paramIndex} RETURNING *`,
    values
  )
  
  const card = result.rows[0] as Card
  await queueChange('cards', 'UPDATE', id, card)
  
  return card
}

export async function deleteCard(id: string): Promise<void> {
  const db = getPGlite()
  
  await db.query('DELETE FROM cards WHERE id = $1', [id])
  await queueChange('cards', 'DELETE', id, null)
}

// ============================================================================
// HAND OPERATIONS
// ============================================================================

export async function createHand(data: Omit<Hand, 'id' | 'created_at'>): Promise<Hand> {
  const db = getPGlite()
  
  // Hands use auto-increment BIGINT and don't have deck_id
  const result = await db.query(
    `INSERT INTO hands (card_int_ids, size, hash64, canonical_key) 
     VALUES ($1, $2, $3, $4) 
     RETURNING *`,
    [
      data.card_int_ids || [],
      data.size || 0,
      data.hash64 || 0,
      data.canonical_key || ''
    ]
  )
  
  const hand = result.rows[0] as Hand
  await queueChange('hands', 'INSERT', String(hand.id), hand)
  
  return hand
}

export async function getHands(deckId?: string): Promise<Hand[]> {
  const db = getPGlite()
  
  if (deckId) {
    const result = await db.query('SELECT * FROM hands WHERE deck_id = $1 ORDER BY created_at DESC', [deckId])
    return result.rows as Hand[]
  } else {
    const result = await db.query('SELECT * FROM hands ORDER BY created_at DESC')
    return result.rows as Hand[]
  }
}

export async function deleteHand(id: string): Promise<void> {
  const db = getPGlite()
  
  await db.query('DELETE FROM hands WHERE id = $1', [id])
  await queueChange('hands', 'DELETE', id, null)
}

// ============================================================================
// HAND EVAL OPERATIONS
// ============================================================================

export async function createHandEval(data: Omit<HandEval, 'id' | 'created_at'>): Promise<HandEval> {
  const db = getPGlite()
  
  // HandEvals use auto-increment BIGINT
  const result = await db.query(
    `INSERT INTO hand_evals (hand1_id, hand2_id, preferred_hand, context_json) 
     VALUES ($1, $2, $3, $4) 
     RETURNING *`,
    [
      data.hand1_id, 
      data.hand2_id, 
      data.preferred_hand,
      data.context_json || {}
    ]
  )
  
  const handEval = result.rows[0] as HandEval
  await queueChange('handevals', 'INSERT', String(handEval.id), handEval)
  
  return handEval
}

export async function getHandEvals(handId?: string): Promise<HandEval[]> {
  const db = getPGlite()
  
  if (handId) {
    const result = await db.query('SELECT * FROM handevals WHERE hand_id = $1 ORDER BY created_at DESC', [handId])
    return result.rows as HandEval[]
  } else {
    const result = await db.query('SELECT * FROM handevals ORDER BY created_at DESC')
    return result.rows as HandEval[]
  }
}

export async function deleteHandEval(id: string): Promise<void> {
  const db = getPGlite()
  
  await db.query('DELETE FROM handevals WHERE id = $1', [id])
  await queueChange('handevals', 'DELETE', id, null)
}

