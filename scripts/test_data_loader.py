#!/usr/bin/env python3
"""
Test script for Scryfall Data Loader
"""

import asyncio
import os
import asyncpg
import httpx

async def test_database_connection():
    """Test database connection and check loaded data"""
    print("Testing database connection...")
    
    database_url = os.getenv("DATABASE_URL", "postgresql://mullagain:mullagain@localhost/mullagain")
    
    try:
        conn = await asyncpg.connect(database_url)
        
        # Check cards table
        card_count = await conn.fetchval('SELECT COUNT(*) FROM "Cards"')
        print(f"Cards in database: {card_count}")
        
        # Check card indexes
        index_count = await conn.fetchval('SELECT COUNT(*) FROM "CardIndexes"')
        print(f"Card indexes: {index_count}")
        
        # Check card embeddings
        embedding_count = await conn.fetchval('SELECT COUNT(*) FROM "CardEmbeddings"')
        print(f"Card embeddings: {embedding_count}")
        
        # Show sample cards
        sample_cards = await conn.fetch('SELECT name, type, cmc FROM "Cards" LIMIT 5')
        print("\nSample cards:")
        for card in sample_cards:
            print(f"  - {card['name']} ({card['type']}) - CMC: {card['cmc']}")
        
        await conn.close()
        return True
        
    except Exception as e:
        print(f"Database connection failed: {e}")
        return False

async def test_ml_worker():
    """Test ML worker health"""
    print("\nTesting ML worker...")
    
    ml_worker_url = os.getenv("ML_WORKER_URL", "http://localhost:7000")
    
    try:
        async with httpx.AsyncClient() as client:
            response = await client.get(f"{ml_worker_url}/health")
            if response.status_code == 200:
                health_data = response.json()
                print(f"ML worker status: {health_data['status']}")
                print(f"Model loaded: {health_data['model_loaded']}")
                print(f"Database status: {health_data['database_status']}")
                return True
            else:
                print(f"ML worker health check failed: {response.status_code}")
                return False
    except Exception as e:
        print(f"ML worker connection failed: {e}")
        return False

async def test_scryfall_api():
    """Test Scryfall API connection"""
    print("\nTesting Scryfall API...")
    
    try:
        async with httpx.AsyncClient() as client:
            response = await client.get("https://api.scryfall.com/cards/random")
            if response.status_code == 200:
                card_data = response.json()
                print(f"Random card: {card_data['name']} ({card_data['type_line']})")
                return True
            else:
                print(f"Scryfall API failed: {response.status_code}")
                return False
    except Exception as e:
        print(f"Scryfall API connection failed: {e}")
        return False

async def main():
    """Run all tests"""
    print("Scryfall Data Loader Test Suite")
    print("=" * 40)
    
    tests = [
        ("Scryfall API", test_scryfall_api),
        ("ML Worker", test_ml_worker),
        ("Database", test_database_connection),
    ]
    
    results = {}
    
    for test_name, test_func in tests:
        try:
            result = await test_func()
            results[test_name] = result
            print(f"\n{test_name}: {'PASS' if result else 'FAIL'}")
        except Exception as e:
            print(f"\n{test_name}: ERROR - {e}")
            results[test_name] = False
    
    print("\n" + "=" * 40)
    print("Test Results Summary:")
    for test_name, result in results.items():
        status = "PASS" if result else "FAIL"
        print(f"  {test_name}: {status}")
    
    passed = sum(results.values())
    total = len(results)
    print(f"\nOverall: {passed}/{total} tests passed")
    
    if passed == total:
        print("All tests passed! 🎉")
    else:
        print("Some tests failed. Check the output above.")

if __name__ == "__main__":
    asyncio.run(main())


