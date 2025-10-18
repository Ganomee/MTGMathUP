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
- **Work fully offline** using ElectricSQL for local PostgreSQL sync
- **Graceful degradation** when ML features are unavailable offline

---

## 2️⃣ Tech Stack

| Layer | Technology | Purpose |
|--------|-------------|----------|
| Frontend | SvelteKit + TypeScript + TailwindCSS | PWA frontend |
| Frontend DB | ElectricSQL + PostgreSQL | Local sync & offline-first |
| Backend | .NET 8 (ASP.NET Core + EF Core) | REST API & ML features |
| Database | PostgreSQL 16 + pgvector + GIN | Storage and indexing |
| ML Worker (optional) | Python (FastAPI + sentence-transformers) | Embedding generation |
| ORM | EF Core 8 + Npgsql provider | Backend ORM layer |
| Sync Layer | ElectricSQL | Real-time sync & conflict resolution |
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
│   │   ├── lib/
│   │   │   ├── electric/          # ElectricSQL integration
│   │   │   │   ├── components/
│   │   │   │   └── api/
│   │   │   └── routes/
│   │   ├── static/
│   │   └── electric/              # ElectricSQL schema & migrations
│   └── Dockerfile
│
├── ml-worker/ (optional)
├── docker-compose.yml
└── PROJECT_SPEC.md
```

---

## 4️⃣ Data Model & Sync Architecture

### 🔹 Core Tables (Synced via ElectricSQL)

| Table | Description | Sync Status |
|--------|--------------|-------------|
| **Card** | Functional card entry keyed by Scryfall `oracle_id`. | ✅ Full sync |
| **CardIndex** | Maps `oracle_id` → small `int` surrogate for efficient hand storage. | ✅ Full sync |
| **Deck** | User-defined deck. | ✅ Full sync |
| **DeckCard** | Deck composition (`deck_id`, `card_id`, `count`). | ✅ Full sync |
| **Hand** | Canonicalized, order-independent multiset of card IDs. | ✅ Full sync |
| **HandEval** | Pairwise evaluations between hands. | ✅ Full sync |

### 🔹 ML-Enhanced Tables (Backend Only)

| Table | Description | Sync Status |
|--------|--------------|-------------|
| **CardEmbedding** | 256D vector embeddings of cards. | ❌ Backend only |
| **HandEmbedding** | 256D vector embeddings of hands. | ❌ Backend only |

### 🔹 ElectricSQL Schema Considerations

- **Conflict Resolution**: Last-write-wins for user-generated data
- **Partial Sync**: ML tables excluded from sync to reduce bandwidth
- **Offline Indicators**: UI shows when ML features are unavailable
- **Sync Status**: Real-time sync status in UI

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

## 5️⃣ Query Modes & Offline Capabilities

### 🔹 Offline Queries (ElectricSQL)

| Query Type | Example | Backed By | Offline Status |
|-------------|----------|-----------|----------------|
| Exact Equality | `hash64 + card_int_ids` | B-tree + unique constraint | ✅ Full |
| Contains Card(s) | `card_int_ids @> ARRAY[bolt_id]` | GIN index | ✅ Full |
| Shared ≥ N Cards | `count_intersection(card_int_ids, ARRAY[x,y,z])` | SQL function | ✅ Full |
| Deck Management | CRUD operations on decks | Local PostgreSQL | ✅ Full |
| Hand Generation | Random hand from deck | Local PostgreSQL | ✅ Full |
| Hand Evaluation | Store pairwise comparisons | Local PostgreSQL | ✅ Full |

### 🔹 Online-Only Queries (Backend API)

| Query Type | Example | Backed By | Offline Status |
|-------------|----------|-----------|----------------|
| Similar Composition | `ORDER BY vec <-> target_vec` | pgvector | ❌ Requires backend |
| Semantic Similarity | via ML worker embeddings | pgvector | ❌ Requires backend |
| Advanced Analytics | Complex aggregations | Backend processing | ❌ Requires backend |

---

## 6️⃣ API Endpoints & Sync Strategy

### 🔹 ElectricSQL Sync Endpoints (Offline-First)

| Method | Path | Description | Sync Status |
|---------|------|-------------|-------------|
| `POST /api/import/scryfall` | Import functional card data from Scryfall bulk | ✅ Auto-sync |
| `GET /api/cards` | List cards | ✅ Auto-sync |
| `POST /api/decks` | Create deck | ✅ Auto-sync |
| `GET /api/decks/{id}` | Get deck composition | ✅ Auto-sync |
| `POST /api/hands/generate` | Generate random canonical hand | ✅ Auto-sync |
| `GET /api/hands/{id}/overlap?n=3` | Find hands sharing ≥ N cards | ✅ Auto-sync |
| `POST /api/hands/evaluate` | Submit pairwise evaluation | ✅ Auto-sync |

### 🔹 Backend-Only Endpoints (Online Required)

| Method | Path | Description | Sync Status |
|---------|------|-------------|-------------|
| `GET /api/hands/{id}/similar` | Find similar hands (pgvector) | ❌ Backend only |
| `POST /api/ml/generate-embeddings` | Generate hand embeddings | ❌ Backend only |
| `GET /api/analytics/trends` | Advanced analytics | ❌ Backend only |
| `GET /health` | Health check | ❌ Backend only |

### 🔹 ElectricSQL Configuration

- **Sync Rules**: Core tables sync bidirectionally
- **Conflict Resolution**: Last-write-wins for user data
- **Offline Queue**: Changes queued when offline, sync on reconnect
- **Partial Sync**: ML tables excluded from sync

---

## 7️⃣ Indexing & Performance

| Column | Index Type | Use |
|---------|-------------|-----|
| `hash64` | B-tree | Deduplication |
| `card_int_ids` | GIN | Containment / overlap |
| `vec` | IVFFlat (pgvector) | Similarity search |

---

## 8️⃣ Workflow Overview & Offline Strategy

### 🔹 Online Workflow (Full Features)

1. **Initial Setup**: Import cards from Scryfall → seed `Card` + `CardIndex` (synced via ElectricSQL)
2. **Deck Management**: Build decks (array of `(card_id, count)`) → synced to all clients
3. **Hand Generation**: Generate random hands → canonicalize, hash, insert or reuse existing
4. **Evaluation**: Store pairwise comparisons → synced across devices
5. **ML Enhancement**: Generate embeddings and store in `hand_embedding` (backend only)
6. **Advanced Queries**: Query via equality, overlap, or vector similarity

### 🔹 Offline Workflow (Core Features Only)

1. **Local Access**: All synced data available immediately
2. **Deck Management**: Create/edit decks locally (queued for sync)
3. **Hand Generation**: Generate random hands from local deck data
4. **Evaluation**: Store pairwise comparisons locally (queued for sync)
5. **Basic Queries**: Query via equality and overlap (no vector similarity)
6. **Sync on Reconnect**: Queued changes sync automatically

### 🔹 Sync Behavior

- **Bidirectional Sync**: Changes sync both ways when online
- **Conflict Resolution**: Last-write-wins for user-generated data
- **Offline Queue**: Changes stored locally and synced on reconnect
- **ML Features**: Gracefully disabled when offline with clear UI indicators

---

## 9️⃣ Frontend Summary & Offline-First Design

### 🔹 Core Pages (Always Available)

- **`/import`** → upload or import decklist (CubeCobra, Moxfield)  
- **`/compare`** → generate two hands, user chooses one  
- **`/analysis`** → visualize average "good hand" composition  

### 🔹 Offline-First Features

- **ElectricSQL Integration**: Local PostgreSQL with real-time sync
- **Offline Indicators**: Clear UI showing sync status and ML availability
- **Graceful Degradation**: ML features disabled with helpful messaging
- **Service Worker**: PWA capabilities for app-like experience
- **Conflict Resolution**: Automatic handling of concurrent edits

### 🔹 Sync Status UI

- **Connection Status**: Online/offline indicator
- **Sync Progress**: Real-time sync status
- **ML Availability**: Clear indication when ML features are unavailable
- **Offline Queue**: Show pending changes when offline

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

## 11️⃣ Implementation Roadmap (Updated for ElectricSQL)

### 🔹 Phase 1: Core Infrastructure
1. **Backend Setup**: Scaffold .NET projects and Postgres schema
2. **ElectricSQL Integration**: Set up sync layer and local PostgreSQL
3. **EF Models**: Implement models with array, hash64, and vector support
4. **Sync Configuration**: Configure ElectricSQL for core tables

### 🔹 Phase 2: Data Management
5. **Scryfall Importer**: Import card data with sync to all clients
6. **Deck Management**: CRUD operations with real-time sync
7. **Hand Generation**: Local hand generation with sync

### 🔹 Phase 3: Core Features
8. **Hand Comparison**: Pairwise evaluation with offline support
9. **Basic Queries**: Equality and overlap queries (offline-capable)
10. **Frontend Pages**: `/import`, `/compare`, `/analysis` with offline indicators

### 🔹 Phase 4: Advanced Features
11. **ML Integration**: Backend-only similarity queries
12. **Analytics**: Advanced aggregations (online-only)
13. **Docker Compose**: Full stack deployment
14. **Optional**: Python worker for embeddings

### 🔹 Phase 5: Polish & Optimization
15. **Conflict Resolution**: Robust handling of concurrent edits
16. **Performance**: Optimize sync performance and bandwidth
17. **Testing**: Comprehensive offline/online testing
18. **Documentation**: User guides for offline usage

---
