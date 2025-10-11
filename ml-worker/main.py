"""
MTG Mullagain ML Worker

This service provides machine learning capabilities for the MTG Mullagain application,
including card and hand embedding generation using sentence-transformers.

Endpoints:
- POST /embed/card: Generate embedding for a card
- POST /embed/hand: Generate embedding for a hand
- POST /recompute/all: Recompute all embeddings
- GET /health: Health check
"""

import asyncio
import logging
import os
from contextlib import asynccontextmanager
from typing import List, Dict, Any, Optional
from datetime import datetime

import uvicorn
from fastapi import FastAPI, HTTPException, BackgroundTasks
from pydantic import BaseModel, Field
import numpy as np
from sentence_transformers import SentenceTransformer
import asyncpg
import structlog

# Configure structured logging
structlog.configure(
    processors=[
        structlog.stdlib.filter_by_level,
        structlog.stdlib.add_logger_name,
        structlog.stdlib.add_log_level,
        structlog.stdlib.PositionalArgumentsFormatter(),
        structlog.processors.TimeStamper(fmt="iso"),
        structlog.processors.StackInfoRenderer(),
        structlog.processors.format_exc_info,
        structlog.processors.UnicodeDecoder(),
        structlog.processors.JSONRenderer()
    ],
    context_class=dict,
    logger_factory=structlog.stdlib.LoggerFactory(),
    wrapper_class=structlog.stdlib.BoundLogger,
    cache_logger_on_first_use=True,
)

logger = structlog.get_logger()

# Global model instance
model: Optional[SentenceTransformer] = None
db_pool: Optional[asyncpg.Pool] = None

class CardEmbeddingRequest(BaseModel):
    """Request model for card embedding generation"""
    card_id: str
    name: str
    oracle_text: str
    type: str
    mana_cost: Optional[str] = None

class HandEmbeddingRequest(BaseModel):
    """Request model for hand embedding generation"""
    hand_id: int
    card_ids: List[int]
    card_names: List[str]

class RecomputeResponse(BaseModel):
    """Response model for recompute operation"""
    status: str
    cards_processed: int
    hands_processed: int
    errors: List[str]
    duration_seconds: float

class EmbeddingResponse(BaseModel):
    """Response model for embedding generation"""
    embedding: List[float]
    model_version: str

@asynccontextmanager
async def lifespan(app: FastAPI):
    """Application lifespan manager"""
    global model, db_pool
    
    # Startup
    logger.info("Starting ML worker...")
    
    # Load sentence transformer model
    logger.info("Loading sentence transformer model...")
    try:
        model = SentenceTransformer('all-MiniLM-L6-v2')
        logger.info("Model loaded successfully", model_name="all-MiniLM-L6-v2")
    except Exception as e:
        logger.error("Failed to load model", error=str(e))
        raise
    
    # Initialize database connection pool
    logger.info("Initializing database connection pool...")
    try:
        database_url = os.getenv("DATABASE_URL", "postgresql://mullagain:mullagain@postgres/mullagain")
        db_pool = await asyncpg.create_pool(
            database_url,
            min_size=1,
            max_size=10,
            command_timeout=60
        )
        logger.info("Database connection pool initialized")
    except Exception as e:
        logger.error("Failed to initialize database connection pool", error=str(e))
        raise
    
    yield
    
    # Shutdown
    logger.info("Shutting down ML worker...")
    if db_pool:
        await db_pool.close()

# Create FastAPI app
app = FastAPI(
    title="MTG Mullagain ML Worker",
    description="Machine learning service for MTG hand analysis",
    version="1.0.0",
    lifespan=lifespan
)

async def get_db_connection():
    """Get database connection from pool"""
    if not db_pool:
        raise HTTPException(status_code=503, detail="Database not available")
    return db_pool

def create_card_text(card_data: Dict[str, Any]) -> str:
    """Create text representation of a card for embedding"""
    parts = []
    
    # Add name
    if card_data.get('name'):
        parts.append(card_data['name'])
    
    # Add oracle text
    if card_data.get('oracle_text'):
        parts.append(card_data['oracle_text'])
    
    # Add type
    if card_data.get('type'):
        parts.append(f"[{card_data['type']}]")
    
    # Add mana cost
    if card_data.get('mana_cost'):
        parts.append(f"Cost: {card_data['mana_cost']}")
    
    return " - ".join(parts)

def create_hand_text(hand_data: Dict[str, Any]) -> str:
    """Create text representation of a hand for embedding"""
    card_names = hand_data.get('card_names', [])
    if not card_names:
        return "Empty hand"
    
    return f"Hand containing: {', '.join(card_names)}"

async def generate_card_embedding(card_data: Dict[str, Any]) -> List[float]:
    """Generate embedding for a card"""
    if model is None:
        raise ValueError("Model not loaded")
    
    card_text = create_card_text(card_data)
    embedding = model.encode(card_text)
    return embedding.tolist()

async def generate_hand_embedding(hand_data: Dict[str, Any]) -> List[float]:
    """Generate embedding for a hand"""
    if model is None:
        raise ValueError("Model not loaded")
    
    hand_text = create_hand_text(hand_data)
    embedding = model.encode(hand_text)
    return embedding.tolist()

@app.get("/health")
async def health_check():
    """Health check endpoint"""
    db_status = "connected" if db_pool else "disconnected"
    model_status = "loaded" if model else "not_loaded"
    
    return {
        "status": "healthy",
        "model_loaded": model is not None,
        "model_version": "all-MiniLM-L6-v2",
        "database_status": db_status,
        "timestamp": datetime.utcnow().isoformat()
    }

@app.post("/embed/card", response_model=EmbeddingResponse)
async def embed_card(request: CardEmbeddingRequest):
    """Generate embedding for a card"""
    if model is None:
        raise HTTPException(status_code=503, detail="Model not loaded")
    
    try:
        card_data = {
            'name': request.name,
            'oracle_text': request.oracle_text,
            'type': request.type,
            'mana_cost': request.mana_cost
        }
        
        embedding = await generate_card_embedding(card_data)
        
        return EmbeddingResponse(
            embedding=embedding,
            model_version="all-MiniLM-L6-v2"
        )
    except Exception as e:
        logger.error("Error generating card embedding", error=str(e), card_id=request.card_id)
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/embed/hand", response_model=EmbeddingResponse)
async def embed_hand(request: HandEmbeddingRequest):
    """Generate embedding for a hand"""
    if model is None:
        raise HTTPException(status_code=503, detail="Model not loaded")
    
    try:
        hand_data = {
            'card_names': request.card_names
        }
        
        embedding = await generate_hand_embedding(hand_data)
        
        return EmbeddingResponse(
            embedding=embedding,
            model_version="all-MiniLM-L6-v2"
        )
    except Exception as e:
        logger.error("Error generating hand embedding", error=str(e), hand_id=request.hand_id)
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/recompute/all", response_model=RecomputeResponse)
async def recompute_all_embeddings(background_tasks: BackgroundTasks):
    """Recompute all card and hand embeddings"""
    if model is None:
        raise HTTPException(status_code=503, detail="Model not loaded")
    
    start_time = datetime.utcnow()
    cards_processed = 0
    hands_processed = 0
    errors = []
    
    try:
        conn = await get_db_connection()
        
        # Process cards
        logger.info("Starting card embedding recomputation...")
        card_query = """
            SELECT c.id, c.name, c.oracle_text, c.type, c.mana_cost
            FROM "Cards" c
            ORDER BY c.created_at
        """
        
        cards = await conn.fetch(card_query)
        
        for card in cards:
            try:
                card_data = dict(card)
                embedding = await generate_card_embedding(card_data)
                
                # Update card embedding
                await conn.execute("""
                    INSERT INTO "CardEmbeddings" (card_id, embedding, model_version, created_at, updated_at)
                    VALUES ($1, $2, $3, $4, $5)
                    ON CONFLICT (card_id) 
                    DO UPDATE SET 
                        embedding = EXCLUDED.embedding,
                        model_version = EXCLUDED.model_version,
                        updated_at = EXCLUDED.updated_at
                """, card['id'], embedding, "all-MiniLM-L6-v2", datetime.utcnow(), datetime.utcnow())
                
                cards_processed += 1
                
                if cards_processed % 100 == 0:
                    logger.info("Processed cards", count=cards_processed)
                    
            except Exception as e:
                error_msg = f"Card {card['id']}: {str(e)}"
                errors.append(error_msg)
                logger.error("Error processing card", card_id=card['id'], error=str(e))
        
        # Process hands
        logger.info("Starting hand embedding recomputation...")
        hand_query = """
            SELECT h.id, h.card_int_ids, ci.card_id, c.name
            FROM "Hands" h
            LEFT JOIN unnest(h.card_int_ids) AS card_int_id ON true
            LEFT JOIN "CardIndexes" ci ON ci.id = card_int_id
            LEFT JOIN "Cards" c ON c.id = ci.card_id
            ORDER BY h.created_at
        """
        
        hands_data = await conn.fetch(hand_query)
        
        # Group by hand ID
        hands_by_id = {}
        for row in hands_data:
            hand_id = row['id']
            if hand_id not in hands_by_id:
                hands_by_id[hand_id] = {
                    'id': hand_id,
                    'card_names': []
                }
            if row['name']:
                hands_by_id[hand_id]['card_names'].append(row['name'])
        
        for hand_id, hand_data in hands_by_id.items():
            try:
                embedding = await generate_hand_embedding(hand_data)
                
                # Update hand embedding
                await conn.execute("""
                    INSERT INTO "HandEmbeddings" (hand_id, embedding, updated_at)
                    VALUES ($1, $2, $3)
                    ON CONFLICT (hand_id) 
                    DO UPDATE SET 
                        embedding = EXCLUDED.embedding,
                        updated_at = EXCLUDED.updated_at
                """, hand_id, embedding, datetime.utcnow())
                
                hands_processed += 1
                
                if hands_processed % 100 == 0:
                    logger.info("Processed hands", count=hands_processed)
                    
            except Exception as e:
                error_msg = f"Hand {hand_id}: {str(e)}"
                errors.append(error_msg)
                logger.error("Error processing hand", hand_id=hand_id, error=str(e))
        
        duration = (datetime.utcnow() - start_time).total_seconds()
        
        logger.info("Recomputation completed", 
                   cards_processed=cards_processed, 
                   hands_processed=hands_processed,
                   errors=len(errors),
                   duration_seconds=duration)
        
        return RecomputeResponse(
            status="completed",
            cards_processed=cards_processed,
            hands_processed=hands_processed,
            errors=errors,
            duration_seconds=duration
        )
        
    except Exception as e:
        logger.error("Error during recomputation", error=str(e))
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    uvicorn.run(
        "main:app",
        host="0.0.0.0",
        port=7000,
        reload=True,
        log_level="info"
    )