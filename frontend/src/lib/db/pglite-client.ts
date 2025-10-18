import { PGlite } from '@electric-sql/pglite'
import generatedSchema from './generated/schema.sql?raw'

let db: PGlite | null = null

/**
 * Initialize PGlite database with schema matching PostgreSQL backend
 */
export async function initPGlite(): Promise<PGlite> {
  if (db) {
    console.log('♻️  PGlite already initialized')
    return db
  }
  
  console.log('🚀 Initializing PGlite database...')
  
  db = new PGlite()
  
  // Load auto-generated schema from PostgreSQL
  // Remove index creation that references public schema (PGlite doesn't support it)
  // Also fix ALTER TABLE statements to work with PGlite
  const cleanedSchema = generatedSchema
    .split('\n')
    .filter(line => !line.includes('ON public.'))
    .map(line => {
      // Replace ALTER TABLE...SET DEFAULT with inline DEFAULT in CREATE TABLE
      // We'll handle this by not executing ALTER TABLE in PGlite
      if (line.includes('ALTER TABLE') && line.includes('SET DEFAULT nextval')) {
        return `-- ${line}`; // Comment it out
      }
      // Replace SELECT setval with a deferred version
      if (line.includes('SELECT setval')) {
        return `-- ${line}`; // Comment it out for now
      }
      return line;
    })
    .join('\n')
    .replace(/ON public\./g, 'ON ')
  
  await db.exec(cleanedSchema)
  
  // Manually set default values for identity columns after tables are created
  // Execute each ALTER TABLE separately for PGlite compatibility
  try {
    await db.exec(`ALTER TABLE decks ALTER COLUMN id SET DEFAULT nextval('decks_id_seq');`);
    await db.exec(`ALTER TABLE hands ALTER COLUMN id SET DEFAULT nextval('hands_id_seq');`);
    await db.exec(`ALTER TABLE hand_evals ALTER COLUMN id SET DEFAULT nextval('hand_evals_id_seq');`);
    console.log('✅ Auto-increment configured for decks, hands, and hand_evals');
  } catch (error) {
    console.error('⚠️  Error configuring auto-increment:', error);
  }
  
  // Add sync queue table for tracking local changes
  await db.exec(`
    CREATE TABLE IF NOT EXISTS sync_queue (
      id SERIAL PRIMARY KEY,
      table_name TEXT NOT NULL,
      operation TEXT NOT NULL,
      record_id TEXT NOT NULL,
      data JSONB,
      timestamp TIMESTAMP DEFAULT NOW(),
      synced BOOLEAN DEFAULT FALSE
    );
    
    CREATE INDEX IF NOT EXISTS idx_sync_queue_synced ON sync_queue(synced);
  `)
  
  console.log('✅ PGlite database initialized with schema')
  
  return db
}

/**
 * Get the PGlite instance (must be initialized first)
 */
export function getPGlite(): PGlite {
  if (!db) {
    throw new Error('PGlite not initialized. Call initPGlite() first.')
  }
  return db
}

/**
 * Close the database connection
 */
export async function closePGlite(): Promise<void> {
  if (db) {
    await db.close()
    db = null
    console.log('🔒 PGlite database closed')
  }
}

