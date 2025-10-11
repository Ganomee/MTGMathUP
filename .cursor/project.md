# 🧠 MTG Mullagain — Full-Stack System Design Document

## 1️⃣ Overview
MTG Mullagain analyzes and compares Magic: The Gathering opening hands to study mulligan decisions.  
Users can import decks, generate random hands, compare them, and discover which hands tend to perform better.  

The app is designed to:
- Store hands as **order-independent multisets** of cards.
- Support **exact, partial, and similarity** queries:
  - Exact equality (deduplication)
  - Partial overlap (shared cards)
  - Vector similarity (embeddings)

---

## 2️⃣ Tech Stack

| Layer | Technology | Purpose |
|--------|-------------|----------|
| Frontend | SvelteKit + TypeScript + TailwindCSS | PWA frontend |
| Backend | .NET 8 (ASP.NET Core + EF Core) | REST API |
| Database | PostgreSQL 16 + pgvector + GIN | Storage and indexing |
| ML Worker (optional) | Python (FastAPI + sentence-transformers) | Embedding generation |
| ORM | EF Core 8 + Npgsql provider | ORM layer |
| Containerization | Docker Compose | Local + deployment |
| Testing | xUnit + Moq | Backend tests |

---

## 3️⃣ Repository Layout

```
mtg-mullagain/
├── backend/
│   ├── src/
│   │   ├── MtgMullagain.Api/
│   │   ├── MtgMullagain.Core/
│   │   └── MtgMullagain.Infrastructure/
│   ├── tests/
│   │   └── MtgMullagain.Tests/
│   └── Dockerfile
│
├── frontend/
│   ├── src/
│   ├── static/
│   └── Dockerfile
│
├── ml-worker/ (optional)
├── docker-compose.yml
└── PROJECT_SPEC.md
```

---

## 4️⃣ Backend Data Model (Updated)

| Table | Description |
|--------|--------------|
| **Card** | Functional card entry keyed by Scryfall `oracle_id`. |
| **CardIndex** | Maps `oracle_id` → small `int` surrogate for efficient hand storage. |
| **Deck** | User-defined deck. |
| **DeckCard** | Deck composition (`deck_id`, `card_id`, `count`). |
| **Hand** | Canonicalized, order-independent multiset of card IDs. |
| **HandEval** | Pairwise evaluations between hands. |
| **CardEmbedding** | 256D vector embeddings of cards. |
| **HandEmbedding** | 256D vector embeddings of hands. |

### 🔹 Hand Table Definition

```sql
CREATE TABLE hand (
  id bigserial PRIMARY KEY,
  card_int_ids int[] NOT NULL,       -- sorted ascending, duplicates allowed
  size smallint NOT NULL,
  hash64 bigint NOT NULL,            -- deterministic hash of sorted array
  canonical_key text GENERATED ALWAYS AS (array_to_string(card_int_ids, ',')) STORED,
  created_at timestamptz DEFAULT now(),
  UNIQUE (hash64, size, card_int_ids)
);
CREATE INDEX ON hand USING gin (card_int_ids);         -- partial overlap search
CREATE INDEX ON hand USING btree (hash64);             -- deduplication
```

### 🔹 HandEmbedding Table

```sql
CREATE TABLE hand_embedding (
  hand_id bigint PRIMARY KEY REFERENCES hand(id) ON DELETE CASCADE,
  vec vector(256),
  updated_at timestamptz DEFAULT now()
);
CREATE INDEX ON hand_embedding USING ivfflat (vec vector_cosine_ops) WITH (lists = 100);
```

---

## 5️⃣ Query Modes

| Query Type | Example | Backed By |
|-------------|----------|-----------|
| Exact Equality | `hash64 + card_int_ids` | B-tree + unique constraint |
| Contains Card(s) | `card_int_ids @> ARRAY[bolt_id]` | GIN index |
| Shared ≥ N Cards | `count_intersection(card_int_ids, ARRAY[x,y,z])` | SQL function |
| Similar Composition | `ORDER BY vec <-> target_vec` | pgvector |
| Semantic Similarity | via ML worker embeddings | pgvector |

---

## 6️⃣ API Endpoints

| Method | Path | Description |
|---------|------|-------------|
| `POST /api/import/scryfall` | Import functional card data from Scryfall bulk |
| `GET /api/cards` | List cards |
| `POST /api/decks` | Create deck |
| `GET /api/decks/{id}` | Get deck composition |
| `POST /api/hands/generate` | Generate random canonical hand |
| `GET /api/hands/{id}/similar` | Find similar hands (pgvector) |
| `GET /api/hands/{id}/overlap?n=3` | Find hands sharing ≥ N cards |
| `POST /api/hands/evaluate` | Submit pairwise evaluation |
| `GET /health` | Health check |

---

## 7️⃣ Indexing & Performance

| Column | Index Type | Use |
|---------|-------------|-----|
| `hash64` | B-tree | Deduplication |
| `card_int_ids` | GIN | Containment / overlap |
| `vec` | IVFFlat (pgvector) | Similarity search |

---

## 8️⃣ Workflow Overview

1. Import cards from Scryfall → seed `Card` + `CardIndex`.  
2. Build decks (array of `(card_id, count)`).  
3. Generate random hands → canonicalize, hash, insert or reuse existing.  
4. Evaluate and store comparisons.  
5. Optional: generate embeddings and store in `hand_embedding`.  
6. Query via equality, overlap, or vector similarity.

---

## 9️⃣ Frontend Summary

- `/import` → upload or import decklist (CubeCobra, Moxfield)  
- `/compare` → generate two hands, user chooses one  
- `/analysis` → visualize average “good hand” composition  
- Offline caching with IndexedDB via `localforage`  
- Service worker for offline use  

---

## 🔟 Indexing Logic and Helper Functions

Define SQL helper for card overlap:

```sql
CREATE FUNCTION count_intersection(a int[], b int[])
RETURNS integer AS $$
  SELECT count(*) FROM unnest(a) x WHERE x = ANY(b);
$$ LANGUAGE sql IMMUTABLE;
```

---

## 11️⃣ Implementation Roadmap (Cursor)

1. Scaffold backend projects and Postgres schema.  
2. Implement EF models (with array, hash64, and vector support).  
3. Add Scryfall importer.  
4. Implement hand generation logic.  
5. Add endpoints for equality, overlap, and similarity queries.  
6. Add frontend pages `/import`, `/compare`, `/analysis`.  
7. Integrate Docker Compose stack.  
8. Optional: connect Python worker for embeddings.  

---
