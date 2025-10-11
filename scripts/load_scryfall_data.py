#!/usr/bin/env python3
"""
Scryfall Data Loader for MTG Mullagain

This script loads card data from Scryfall API and generates embeddings.
It fetches all cards, inserts them into PostgreSQL, and generates embeddings using the ML worker.
"""

import asyncio
import json
import logging
import os
import sys
from datetime import datetime
from typing import Dict, List, Any, Optional
import asyncpg
import httpx
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

# Scryfall API configuration
SCRYFALL_BASE_URL = "https://api.scryfall.com"
SCRYFALL_CARDS_URL = f"{SCRYFALL_BASE_URL}/cards"
SCRYFALL_SETS_URL = f"{SCRYFALL_BASE_URL}/sets"

class ScryfallLoader:
    def __init__(self, database_url: str, ml_worker_url: str = "http://localhost:7000"):
        self.database_url = database_url
        self.ml_worker_url = ml_worker_url
        self.db_pool: Optional[asyncpg.Pool] = None
        self.http_client: Optional[httpx.AsyncClient] = None
        
    async def __aenter__(self):
        """Async context manager entry"""
        # Initialize database connection pool
        self.db_pool = await asyncpg.create_pool(
            self.database_url,
            min_size=1,
            max_size=10,
            command_timeout=60
        )
        
        # Initialize HTTP client
        self.http_client = httpx.AsyncClient(
            timeout=30.0,
            limits=httpx.Limits(max_keepalive_connections=20, max_connections=100)
        )
        
        return self
    
    async def __aexit__(self, exc_type, exc_val, exc_tb):
        """Async context manager exit"""
        if self.db_pool:
            await self.db_pool.close()
        if self.http_client:
            await self.http_client.aclose()
    
    async def get_db_connection(self):
        """Get database connection from pool"""
        if not self.db_pool:
            raise RuntimeError("Database pool not initialized")
        return self.db_pool
    
    async def fetch_all_cards(self) -> List[Dict[str, Any]]:
        """Fetch all cards from Scryfall API"""
        logger.info("Starting to fetch all cards from Scryfall...")
        
        all_cards = []
        next_url = SCRYFALL_CARDS_URL
        page = 1
        
        while next_url:
            try:
                logger.info("Fetching page", page=page, url=next_url)
                response = await self.http_client.get(next_url)
                response.raise_for_status()
                
                data = response.json()
                cards = data.get('data', [])
                all_cards.extend(cards)
                
                logger.info("Fetched page", page=page, cards_count=len(cards), total_cards=len(all_cards))
                
                # Check if there's a next page
                if data.get('has_more'):
                    next_url = data.get('next_page')
                    page += 1
                    # Rate limiting - Scryfall allows 50-100 requests per second
                    await asyncio.sleep(0.1)
                else:
                    next_url = None
                    
            except httpx.HTTPError as e:
                logger.error("HTTP error fetching cards", error=str(e), page=page)
                raise
            except Exception as e:
                logger.error("Error fetching cards", error=str(e), page=page)
                raise
        
        logger.info("Finished fetching all cards", total_cards=len(all_cards))
        return all_cards
    
    def normalize_card_data(self, card: Dict[str, Any]) -> Dict[str, Any]:
        """Normalize card data for database insertion"""
        # Extract basic information
        card_id = card.get('id')
        name = card.get('name', '')
        oracle_text = card.get('oracle_text', '')
        card_type = card.get('type_line', '')
        mana_cost = card.get('mana_cost', '')
        cmc = card.get('cmc', 0)
        power = card.get('power')
        toughness = card.get('toughness')
        colors = card.get('colors', [])
        
        # Handle power/toughness
        power_toughness = None
        if power is not None and toughness is not None:
            power_toughness = f"{power}/{toughness}"
        elif power is not None:
            power_toughness = str(power)
        elif toughness is not None:
            power_toughness = str(toughness)
        
        # Handle colors
        color_array = colors if colors else []
        
        return {
            'id': card_id,
            'name': name,
            'mana_cost': mana_cost,
            'oracle_text': oracle_text,
            'type': card_type,
            'power_toughness': power_toughness,
            'cmc': cmc,
            'colors': color_array,
            'created_at': datetime.utcnow(),
            'updated_at': datetime.utcnow()
        }
    
    async def insert_cards(self, cards: List[Dict[str, Any]]) -> int:
        """Insert cards into the database"""
        logger.info("Starting to insert cards into database...")
        
        conn = await self.get_db_connection()
        inserted_count = 0
        
        # Prepare the insert statement
        insert_stmt = """
            INSERT INTO "Cards" (id, name, mana_cost, oracle_text, type, power_toughness, cmc, colors, created_at, updated_at)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)
            ON CONFLICT (id) DO UPDATE SET
                name = EXCLUDED.name,
                mana_cost = EXCLUDED.mana_cost,
                oracle_text = EXCLUDED.oracle_text,
                type = EXCLUDED.type,
                power_toughness = EXCLUDED.power_toughness,
                cmc = EXCLUDED.cmc,
                colors = EXCLUDED.colors,
                updated_at = EXCLUDED.updated_at
        """
        
        # Insert cards in batches
        batch_size = 100
        for i in range(0, len(cards), batch_size):
            batch = cards[i:i + batch_size]
            
            try:
                # Prepare batch data
                batch_data = []
                for card in batch:
                    normalized = self.normalize_card_data(card)
                    batch_data.append((
                        normalized['id'],
                        normalized['name'],
                        normalized['mana_cost'],
                        normalized['oracle_text'],
                        normalized['type'],
                        normalized['power_toughness'],
                        normalized['cmc'],
                        normalized['colors'],
                        normalized['created_at'],
                        normalized['updated_at']
                    ))
                
                # Execute batch insert
                await conn.executemany(insert_stmt, batch_data)
                inserted_count += len(batch)
                
                logger.info("Inserted batch", batch_start=i, batch_size=len(batch), total_inserted=inserted_count)
                
            except Exception as e:
                logger.error("Error inserting batch", batch_start=i, error=str(e))
                raise
        
        logger.info("Finished inserting cards", total_inserted=inserted_count)
        return inserted_count
    
    async def create_card_indexes(self) -> int:
        """Create card indexes for efficient lookups"""
        logger.info("Creating card indexes...")
        
        conn = await self.get_db_connection()
        
        # Get all cards and create indexes
        cards = await conn.fetch('SELECT id FROM "Cards" ORDER BY created_at')
        
        insert_stmt = """
            INSERT INTO "CardIndexes" (id, card_id, created_at)
            VALUES ($1, $2, $3)
            ON CONFLICT (id) DO NOTHING
        """
        
        inserted_count = 0
        for idx, card in enumerate(cards, 1):
            try:
                await conn.execute(insert_stmt, idx, card['id'], datetime.utcnow())
                inserted_count += 1
            except Exception as e:
                logger.error("Error creating card index", card_id=card['id'], error=str(e))
        
        logger.info("Finished creating card indexes", total_inserted=inserted_count)
        return inserted_count
    
    async def generate_embeddings(self) -> Dict[str, int]:
        """Generate embeddings for all cards using ML worker"""
        logger.info("Starting embedding generation...")
        
        try:
            # Call ML worker recompute endpoint
            response = await self.http_client.post(f"{self.ml_worker_url}/recompute/all")
            response.raise_for_status()
            
            result = response.json()
            logger.info("Embedding generation completed", 
                       cards_processed=result['cards_processed'],
                       hands_processed=result['hands_processed'],
                       errors=len(result['errors']),
                       duration_seconds=result['duration_seconds'])
            
            if result['errors']:
                logger.warning("Some errors occurred during embedding generation", error_count=len(result['errors']))
                for error in result['errors'][:10]:  # Log first 10 errors
                    logger.warning("Embedding error", error=error)
            
            return {
                'cards_processed': result['cards_processed'],
                'hands_processed': result['hands_processed'],
                'errors': len(result['errors'])
            }
            
        except httpx.HTTPError as e:
            logger.error("HTTP error generating embeddings", error=str(e))
            raise
        except Exception as e:
            logger.error("Error generating embeddings", error=str(e))
            raise
    
    async def load_all_data(self) -> Dict[str, Any]:
        """Load all data from Scryfall and generate embeddings"""
        start_time = datetime.utcnow()
        
        try:
            # Step 1: Fetch all cards from Scryfall
            cards = await self.fetch_all_cards()
            
            # Step 2: Insert cards into database
            inserted_cards = await self.insert_cards(cards)
            
            # Step 3: Create card indexes
            inserted_indexes = await self.create_card_indexes()
            
            # Step 4: Generate embeddings
            embedding_results = await self.generate_embeddings()
            
            duration = (datetime.utcnow() - start_time).total_seconds()
            
            result = {
                'status': 'completed',
                'cards_fetched': len(cards),
                'cards_inserted': inserted_cards,
                'indexes_created': inserted_indexes,
                'embedding_results': embedding_results,
                'duration_seconds': duration
            }
            
            logger.info("Data loading completed", **result)
            return result
            
        except Exception as e:
            logger.error("Data loading failed", error=str(e))
            raise

async def main():
    """Main function"""
    # Get configuration from environment
    database_url = os.getenv("DATABASE_URL", "postgresql://mullagain:mullagain@localhost/mullagain")
    ml_worker_url = os.getenv("ML_WORKER_URL", "http://localhost:7000")
    
    logger.info("Starting Scryfall data loader", 
               database_url=database_url, 
               ml_worker_url=ml_worker_url)
    
    try:
        async with ScryfallLoader(database_url, ml_worker_url) as loader:
            result = await loader.load_all_data()
            
            print("\n" + "="*50)
            print("SCRYFALL DATA LOADING COMPLETED")
            print("="*50)
            print(f"Cards fetched: {result['cards_fetched']}")
            print(f"Cards inserted: {result['cards_inserted']}")
            print(f"Indexes created: {result['indexes_created']}")
            print(f"Embeddings generated: {result['embedding_results']['cards_processed']}")
            print(f"Duration: {result['duration_seconds']:.2f} seconds")
            print("="*50)
            
    except Exception as e:
        logger.error("Fatal error", error=str(e))
        sys.exit(1)

if __name__ == "__main__":
    asyncio.run(main())
