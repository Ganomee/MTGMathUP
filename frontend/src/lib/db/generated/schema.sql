-- Auto-generated from PostgreSQL schema

-- Source: file .pg-connection

CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
  MigrationId TEXT NOT NULL,
  ProductVersion TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS card_indexes (
  id INTEGER NOT NULL,
  card_id TEXT NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS cards (
  id TEXT NOT NULL,
  name TEXT NOT NULL,
  mana_cost TEXT NOT NULL,
  oracle_text TEXT NOT NULL,
  type TEXT NOT NULL,
  power_toughness TEXT,
  cmc INTEGER NOT NULL,
  colors TEXT[] NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS deck_cards (
  deck_id BIGINT NOT NULL,
  card_id TEXT NOT NULL,
  count INTEGER NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS decks (
  id BIGINT NOT NULL,
  name TEXT NOT NULL,
  format TEXT NOT NULL,
  owner TEXT,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL
);

CREATE SEQUENCE IF NOT EXISTS decks_id_seq;
ALTER TABLE decks ALTER COLUMN id SET DEFAULT nextval('decks_id_seq');
SELECT setval('decks_id_seq', COALESCE((SELECT MAX(id) FROM decks), 0) + 1, false);

CREATE TABLE IF NOT EXISTS hand_evals (
  id BIGINT NOT NULL,
  hand1_id BIGINT NOT NULL,
  hand2_id BIGINT NOT NULL,
  preferred_hand INTEGER NOT NULL,
  context_json JSONB NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
  HandId BIGINT
);

CREATE SEQUENCE IF NOT EXISTS hand_evals_id_seq;
ALTER TABLE hand_evals ALTER COLUMN id SET DEFAULT nextval('hand_evals_id_seq');
SELECT setval('hand_evals_id_seq', COALESCE((SELECT MAX(id) FROM hand_evals), 0) + 1, false);

CREATE TABLE IF NOT EXISTS hands (
  id BIGINT NOT NULL,
  card_int_ids INTEGER[] NOT NULL,
  size SMALLINT NOT NULL,
  hash64 BIGINT NOT NULL,
  canonical_key TEXT NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL
);

CREATE SEQUENCE IF NOT EXISTS hands_id_seq;
ALTER TABLE hands ALTER COLUMN id SET DEFAULT nextval('hands_id_seq');
SELECT setval('hands_id_seq', COALESCE((SELECT MAX(id) FROM hands), 0) + 1, false);

CREATE TABLE IF NOT EXISTS import_metadata (
  id TEXT NOT NULL,
  type TEXT NOT NULL,
  updated_at TIMESTAMP DEFAULT '-infinity'::timestamp with time zone NOT NULL,
  imported_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
  download_uri TEXT
);

-- Indexes

CREATE UNIQUE INDEX IF NOT EXISTS "PK___EFMigrationsHistory" ON public."__EFMigrationsHistory" USING btree ("MigrationId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_CardIndexes_CardId" ON public.card_indexes USING btree (card_id);
CREATE INDEX IF NOT EXISTS "IX_Cards_Cmc" ON public.cards USING btree (cmc);
CREATE INDEX IF NOT EXISTS "IX_Cards_Name" ON public.cards USING btree (name);
CREATE INDEX IF NOT EXISTS "IX_Cards_Type" ON public.cards USING btree (type);
CREATE INDEX IF NOT EXISTS "IX_deck_cards_card_id" ON public.deck_cards USING btree (card_id);
CREATE INDEX IF NOT EXISTS "IX_Decks_Format" ON public.decks USING btree (format);
CREATE INDEX IF NOT EXISTS "IX_Decks_Name" ON public.decks USING btree (name);
CREATE INDEX IF NOT EXISTS "IX_Decks_Owner" ON public.decks USING btree (owner);
CREATE INDEX IF NOT EXISTS "IX_HandEvals_CreatedAt" ON public.hand_evals USING btree (created_at);
CREATE INDEX IF NOT EXISTS "IX_HandEvals_Hand1Id" ON public.hand_evals USING btree (hand1_id);
CREATE INDEX IF NOT EXISTS "IX_HandEvals_Hand2Id" ON public.hand_evals USING btree (hand2_id);
CREATE INDEX IF NOT EXISTS "IX_hand_evals_HandId" ON public.hand_evals USING btree ("HandId");
CREATE INDEX IF NOT EXISTS "IX_HandEvals_PreferredHand" ON public.hand_evals USING btree (preferred_hand);
CREATE INDEX IF NOT EXISTS "IX_Hands_CardIntIds_GIN" ON public.hands USING gin (card_int_ids);
CREATE INDEX IF NOT EXISTS "IX_Hands_CreatedAt" ON public.hands USING btree (created_at);
CREATE INDEX IF NOT EXISTS "IX_Hands_Hash64" ON public.hands USING btree (hash64);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Hands_Hash64_Size_CardIntIds_Unique" ON public.hands USING btree (hash64, size, card_int_ids);
CREATE INDEX IF NOT EXISTS "IX_ImportMetadata_ImportedAt" ON public.import_metadata USING btree (imported_at);
CREATE INDEX IF NOT EXISTS "IX_ImportMetadata_Type" ON public.import_metadata USING btree (type);
CREATE UNIQUE INDEX IF NOT EXISTS p_k_hands ON public.hands USING btree (id);
CREATE UNIQUE INDEX IF NOT EXISTS p_k_decks ON public.decks USING btree (id);
CREATE UNIQUE INDEX IF NOT EXISTS p_k_cards ON public.cards USING btree (id);
CREATE UNIQUE INDEX IF NOT EXISTS p_k_import_metadata ON public.import_metadata USING btree (id);
CREATE UNIQUE INDEX IF NOT EXISTS p_k_hand_evals ON public.hand_evals USING btree (id);
CREATE UNIQUE INDEX IF NOT EXISTS "PK_deck_cards" ON public.deck_cards USING btree (deck_id, card_id);
CREATE UNIQUE INDEX IF NOT EXISTS p_k_card_indexes ON public.card_indexes USING btree (id);
