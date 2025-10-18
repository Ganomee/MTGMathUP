import fs from 'fs';
import path from 'path';
import { Client } from 'pg';

interface ColumnInfo {
  table_name: string;
  column_name: string;
  data_type: string;
  udt_name: string;
  is_nullable: 'YES' | 'NO';
  column_default: string | null;
  is_identity: 'YES' | 'NO';
}

interface IndexInfo {
  tablename: string;
  indexname: string;
  indexdef: string;
}

type ConnectionSource = 'env' | 'file';

type ConnectionConfig = {
  raw: string;
  source: ConnectionSource;
};

function parseConnectionString(raw: string): Client {
  if (raw.startsWith('postgres://') || raw.startsWith('postgresql://')) {
    return new Client({ connectionString: raw });
  }

  const entries = raw.split(';').map((part) => part.trim()).filter(Boolean);
  const map = new Map<string, string>();
  for (const entry of entries) {
    const [key, value] = entry.split('=').map((s) => s.trim());
    if (key && value) {
      map.set(key.toLowerCase(), value);
    }
  }

  const host = map.get('host') ?? map.get('server');
  const database = map.get('database') ?? map.get('dbname');
  const user = map.get('username') ?? map.get('user') ?? map.get('uid');
  const password = map.get('password') ?? map.get('pwd');
  const port = map.get('port') ? Number(map.get('port')) : undefined;

  if (!host || !database || !user) {
    throw new Error('Invalid PostgreSQL connection string. Expect host, database, username.');
  }

  return new Client({ host, database, user, password, port });
}

function resolveConnectionString(): ConnectionConfig | null {
  if (process.env.PG_CONNECTION_STRING) {
    return { raw: process.env.PG_CONNECTION_STRING, source: 'env' };
  }

  const connectionFile = process.env.PG_CONNECTION_FILE;
  if (connectionFile && fs.existsSync(connectionFile)) {
    const raw = fs.readFileSync(connectionFile, 'utf-8').trim();
    if (raw) {
      return { raw, source: 'file' };
    }
  }

  return null;
}

function mapType(dataType: string, udtName: string, columnName: string): string {
  const normalizedType = dataType.toLowerCase();
  const normalizedUdt = udtName.toLowerCase();
  
  // Handle array types using udt_name (e.g., _text, _int4)
  if (normalizedType === 'array' && normalizedUdt.startsWith('_')) {
    const baseUdt = normalizedUdt.substring(1); // Remove the leading underscore
    const baseType = mapType(baseUdt, baseUdt, columnName);
    return `${baseType}[]`;
  }
  
  switch (normalizedType) {
    case 'uuid':
      return 'TEXT';
    case 'text':
    case 'character varying':
    case 'citext':
      return 'TEXT';
    case 'integer':
    case 'int4':
      return 'INTEGER';
    case 'bigint':
    case 'int8':
      return 'BIGINT';
    case 'smallint':
    case 'int2':
      return 'SMALLINT';
    case 'numeric':
    case 'double precision':
      return 'REAL';
    case 'boolean':
      return 'BOOLEAN';
    case 'timestamp without time zone':
    case 'timestamp with time zone':
      return 'TIMESTAMP';
    case 'json':
    case 'jsonb':
      return 'JSONB';
    default:
      if (normalizedType.endsWith('[]')) {
        const baseType = normalizedType.replace('[]', '');
        return `${mapType(baseType, udtName, columnName)}[]`;
      }
      console.warn(`⚠️  Unknown data type '${dataType}' (udt: '${udtName}') for column '${columnName}', defaulting to TEXT.`);
      return 'TEXT';
  }
}

function normalizeDefault(columnDefault: string | null): string | null {
  if (!columnDefault) {
    return null;
  }

  const trimmed = columnDefault.trim();

  if (trimmed.startsWith('nextval(')) {
    return null;
  }

  if (trimmed === 'now()' || trimmed === 'CURRENT_TIMESTAMP') {
    return 'CURRENT_TIMESTAMP';
  }

  if (/^'[^']*'$/.test(trimmed)) {
    return trimmed;
  }

  return trimmed;
}

function ensureIfNotExists(indexDef: string): string {
  const createRegex = /^CREATE(\s+UNIQUE)?\s+INDEX/i;
  return indexDef.replace(createRegex, (match) => `${match} IF NOT EXISTS`);
}

async function generateSchema() {
  const resolved = resolveConnectionString();
  if (!resolved) {
    console.warn('ℹ️  No PostgreSQL connection string provided. Set PG_CONNECTION_STRING or PG_CONNECTION_FILE. Skipping schema generation.');
    process.exit(0);
  }

  const client = parseConnectionString(resolved.raw);
  await client.connect();

  const columnsResult = await client.query<ColumnInfo>(`
    SELECT 
      table_name, 
      column_name, 
      data_type, 
      udt_name, 
      is_nullable, 
      column_default,
      is_identity
    FROM information_schema.columns
    WHERE table_schema = 'public'
    ORDER BY table_name, ordinal_position;
  `);

  const indexesResult = await client.query<IndexInfo>(`
    SELECT tablename, indexname, indexdef
    FROM pg_indexes
    WHERE schemaname = 'public';
  `);

  await client.end();

  const tables = new Map<string, ColumnInfo[]>();
  for (const row of columnsResult.rows) {
    if (!tables.has(row.table_name)) {
      tables.set(row.table_name, []);
    }
    tables.get(row.table_name)!.push(row);
  }

  let sourceLabel = resolved.source === 'env' ? 'PG_CONNECTION_STRING' : `file ${process.env.PG_CONNECTION_FILE}`;
  let sql = '-- Auto-generated from PostgreSQL schema\n\n';
  sql += `-- Source: ${sourceLabel}\n\n`;

  for (const [tableName, columns] of tables) {
    sql += `CREATE TABLE IF NOT EXISTS ${tableName} (\n`;
    const columnDefs = columns.map(column => {
      const pgType = mapType(column.data_type, column.udt_name, column.column_name);
      let defaultValue = normalizeDefault(column.column_default);
      const notNull = column.is_nullable === 'NO';
      const isIdentity = column.is_identity === 'YES';

      const fragments = [`  ${column.column_name} ${pgType}`];

      // Handle identity columns (auto-increment)
      if (isIdentity && pgType === 'BIGINT') {
        // PGlite doesn't support GENERATED...AS IDENTITY, but we can use DEFAULT nextval
        // We'll create a sequence later
        defaultValue = null; // Don't add DEFAULT here, we'll handle it with a sequence
      }

      if (defaultValue) {
        fragments.push(`DEFAULT ${defaultValue}`);
      }

      if (notNull) {
        fragments.push('NOT NULL');
      }

      return fragments.join(' ');
    });
    sql += columnDefs.join(',\n');
    sql += '\n);\n\n';
    
    // Create sequences for identity columns
    for (const column of columns) {
      if (column.is_identity === 'YES' && mapType(column.data_type, column.udt_name, column.column_name) === 'BIGINT') {
        sql += `CREATE SEQUENCE IF NOT EXISTS ${tableName}_${column.column_name}_seq;\n`;
        sql += `ALTER TABLE ${tableName} ALTER COLUMN ${column.column_name} SET DEFAULT nextval('${tableName}_${column.column_name}_seq');\n`;
        sql += `SELECT setval('${tableName}_${column.column_name}_seq', COALESCE((SELECT MAX(${column.column_name}) FROM ${tableName}), 0) + 1, false);\n\n`;
      }
    }
  }

  if (indexesResult.rows.length > 0) {
    sql += '-- Indexes\n\n';
    for (const indexRow of indexesResult.rows) {
      const cleaned = ensureIfNotExists(indexRow.indexdef);
      sql += `${cleaned};\n`;
    }
  }

  const outDir = path.resolve('src/lib/db/generated');
  fs.mkdirSync(outDir, { recursive: true });
  const outFile = path.join(outDir, 'schema.sql');
  fs.writeFileSync(outFile, sql, 'utf-8');
  console.log(`✅ Generated PGlite schema at ${outFile}`);
}

generateSchema().catch(err => {
  console.error('❌ Failed to generate PGlite schema', err);
  process.exit(1);
});

