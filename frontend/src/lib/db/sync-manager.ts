import { getPGlite } from './pglite-client'
import { writable, get } from 'svelte/store'

// ============================================================================
// SYNC STATUS STORE
// ============================================================================

export interface SyncStatus {
  isEnabled: boolean
  isSyncing: boolean
  lastSync: Date | null
  pendingChanges: number
  error: string | null
}

export const syncStatus = writable<SyncStatus>({
  isEnabled: false,
  isSyncing: false,
  lastSync: null,
  pendingChanges: 0,
  error: null
})

// ============================================================================
// TABLE SCHEMA DEFINITIONS
// ============================================================================

// Define which columns belong to each table (filters out navigation properties)
const TABLE_SCHEMAS: Record<string, string[]> = {
  decks: ['id', 'name', 'format', 'owner', 'created_at', 'updated_at'],
  cards: ['id', 'name', 'mana_cost', 'oracle_text', 'type', 'power_toughness', 'cmc', 'colors', 'created_at', 'updated_at'],
  hands: ['id', 'card_int_ids', 'size', 'hash64', 'canonical_key', 'created_at'],
  handevals: ['id', 'hand1_id', 'hand2_id', 'preferred_hand', 'context_json', 'created_at']
}

// ============================================================================
// SYNC MANAGER
// ============================================================================

export class SyncManager {
  private syncInterval: number | null = null
  private readonly API_BASE = 'http://localhost:5000/api'
  
  /**
   * Start the sync manager with periodic syncing
   */
  async startSync(intervalMs: number = 5000): Promise<void> {
    console.log('🔄 Starting sync manager...')
    
    syncStatus.update(s => ({ ...s, isEnabled: true, error: null }))
    
    // Initial sync
    await this.performSync()
    
    // Periodic sync
    if (this.syncInterval === null) {
      this.syncInterval = window.setInterval(() => {
        this.performSync()
      }, intervalMs)
    }
  }
  
  /**
   * Stop the sync manager
   */
  stopSync(): void {
    console.log('⏸️  Stopping sync manager...')
    
    if (this.syncInterval !== null) {
      clearInterval(this.syncInterval)
      this.syncInterval = null
    }
    
    syncStatus.update(s => ({ ...s, isEnabled: false }))
  }
  
  /**
   * Perform a single sync cycle (push then pull)
   */
  async performSync(): Promise<void> {
    const status = get(syncStatus)
    
    if (!status.isEnabled) {
      console.log('⏸️  Sync is disabled, skipping...')
      return
    }
    
    if (status.isSyncing) {
      console.log('⏳ Sync already in progress, skipping...')
      return
    }
    
    try {
      syncStatus.update(s => ({ ...s, isSyncing: true, error: null }))
      
      // Step 1: Push local changes to server
      await this.pushChanges()
      
      // Step 2: Pull server changes to local
      await this.pullChanges()
      
      // Update pending changes count
      const pendingChanges = await this.getPendingChanges()
      
      syncStatus.update(s => ({ 
        ...s, 
        isSyncing: false, 
        lastSync: new Date(),
        pendingChanges
      }))
      
      console.log('✅ Sync completed successfully')
      
    } catch (error: any) {
      console.error('❌ Sync failed:', error)
      syncStatus.update(s => ({ 
        ...s, 
        isSyncing: false, 
        error: error.message || 'Unknown sync error'
      }))
    }
  }
  
  /**
   * Push local changes to the server
   */
  private async pushChanges(): Promise<void> {
    const db = getPGlite()
    
    // Get unsynced changes
    const result = await db.query(
      'SELECT * FROM sync_queue WHERE synced = FALSE ORDER BY timestamp ASC'
    )
    
    const changes = result.rows
    
    if (changes.length === 0) {
      console.log('📤 No changes to push')
      return
    }
    
    console.log(`📤 Pushing ${changes.length} change(s) to server...`)
    
    for (const change of changes) {
      try {
        await this.syncChangeToServer(change)
        
        // Mark as synced
        await db.query(
          'UPDATE sync_queue SET synced = TRUE WHERE id = $1',
          [change.id]
        )
        
        console.log(`✅ Synced ${change.table_name} ${change.operation} ${change.record_id}`)
        
      } catch (error: any) {
        console.error(`❌ Failed to sync change ${change.id}:`, error.message)
        // Continue with other changes instead of stopping
      }
    }
  }
  
  /**
   * Sync a single change to the server
   */
  private async syncChangeToServer(change: any): Promise<void> {
    const { table_name, operation, record_id, data } = change
    const parsedData = typeof data === 'string' ? JSON.parse(data) : data
    
    let endpoint = `${this.API_BASE}/${table_name}`
    let method = 'POST'
    let body: string | null = null
    
    switch (operation) {
      case 'INSERT':
        method = 'POST'
        body = JSON.stringify(parsedData)
        break
        
      case 'UPDATE':
        endpoint = `${endpoint}/${record_id}`
        method = 'PUT'
        body = JSON.stringify(parsedData)
        break
        
      case 'DELETE':
        endpoint = `${endpoint}/${record_id}`
        method = 'DELETE'
        break
    }
    
    const response = await fetch(endpoint, {
      method,
      headers: { 'Content-Type': 'application/json' },
      body: body || undefined
    })
    
    if (!response.ok) {
      const errorText = await response.text()
      throw new Error(`HTTP ${response.status}: ${errorText}`)
    }
  }
  
  /**
   * Pull changes from the server
   */
  private async pullChanges(): Promise<void> {
    console.log('📥 Pulling changes from server...')
    
    // Pull each table
    await this.pullTable('decks')
    await this.pullTable('cards')
    await this.pullTable('hands')
    await this.pullTable('handevals')
  }
  
  /**
   * Pull a single table from the server
   */
  private async pullTable(tableName: string): Promise<void> {
    const db = getPGlite()
    
    try {
      const response = await fetch(`${this.API_BASE}/${tableName}`)
      
      if (!response.ok) {
        console.warn(`⚠️  Failed to pull ${tableName}: ${response.status}`)
        return
      }
      
      const serverRecords = await response.json()
      
      if (!Array.isArray(serverRecords)) {
        console.warn(`⚠️  Invalid response for ${tableName}`)
        return
      }
      
      console.log(`📥 Pulled ${serverRecords.length} record(s) from ${tableName}`)
      
      // Get the allowed columns for this table
      const allowedColumns = TABLE_SCHEMAS[tableName]
      if (!allowedColumns) {
        console.warn(`⚠️  No schema defined for table ${tableName}`)
        return
      }
      
      // Simple merge strategy: server wins for conflicts
      // TODO: Implement proper conflict resolution with timestamps
      
      for (const record of serverRecords) {
        // Filter out navigation properties and only keep allowed columns
        const filteredRecord: Record<string, any> = {}
        for (const col of allowedColumns) {
          if (col in record) {
            filteredRecord[col] = record[col]
          }
        }
        
        const existing = await db.query(
          `SELECT id FROM ${tableName} WHERE id = $1`,
          [filteredRecord.id]
        )
        
        if (existing.rows.length === 0) {
          // Insert new record from server
          const columns = Object.keys(filteredRecord).join(', ')
          const placeholders = Object.keys(filteredRecord).map((_, i) => `$${i + 1}`).join(', ')
          const values = Object.values(filteredRecord)
          
          await db.query(
            `INSERT INTO ${tableName} (${columns}) VALUES (${placeholders})`,
            values
          )
        } else {
          // Update existing record (server wins)
          const updates = Object.keys(filteredRecord)
            .filter(key => key !== 'id')
            .map((key, i) => `${key} = $${i + 1}`)
            .join(', ')
          
          if (updates) {
            const values = Object.keys(filteredRecord)
              .filter(key => key !== 'id')
              .map(key => filteredRecord[key])
            values.push(filteredRecord.id)
            
            await db.query(
              `UPDATE ${tableName} SET ${updates} WHERE id = $${values.length}`,
              values
            )
          }
        }
      }
      
    } catch (error: any) {
      console.error(`❌ Error pulling ${tableName}:`, error.message)
    }
  }
  
  /**
   * Get the count of pending changes
   */
  async getPendingChanges(): Promise<number> {
    const db = getPGlite()
    const result = await db.query('SELECT COUNT(*) as count FROM sync_queue WHERE synced = FALSE')
    return parseInt(result.rows[0].count) || 0
  }
  
  /**
   * Clear all synced changes from the queue
   */
  async clearSyncedChanges(): Promise<void> {
    const db = getPGlite()
    await db.query('DELETE FROM sync_queue WHERE synced = TRUE')
    console.log('🧹 Cleared synced changes from queue')
  }
}

// ============================================================================
// SINGLETON INSTANCE
// ============================================================================

export const syncManager = new SyncManager()

