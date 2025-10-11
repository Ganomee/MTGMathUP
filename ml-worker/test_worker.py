#!/usr/bin/env python3
"""
Test script for MTG Mullagain ML Worker
"""

import asyncio
import httpx
import json
from typing import Dict, Any

BASE_URL = "http://localhost:7000"

async def test_health():
    """Test health endpoint"""
    print("Testing health endpoint...")
    async with httpx.AsyncClient() as client:
        response = await client.get(f"{BASE_URL}/health")
        print(f"Health check: {response.status_code}")
        print(f"Response: {response.json()}")
        return response.status_code == 200

async def test_card_embedding():
    """Test card embedding generation"""
    print("\nTesting card embedding...")
    
    card_data = {
        "card_id": "test-card-1",
        "name": "Lightning Bolt",
        "oracle_text": "Lightning Bolt deals 3 damage to any target.",
        "type": "Instant",
        "mana_cost": "{R}"
    }
    
    async with httpx.AsyncClient() as client:
        response = await client.post(
            f"{BASE_URL}/embed/card",
            json=card_data
        )
        print(f"Card embedding: {response.status_code}")
        if response.status_code == 200:
            result = response.json()
            print(f"Embedding dimension: {len(result['embedding'])}")
            print(f"Model version: {result['model_version']}")
        else:
            print(f"Error: {response.text}")
        return response.status_code == 200

async def test_hand_embedding():
    """Test hand embedding generation"""
    print("\nTesting hand embedding...")
    
    hand_data = {
        "hand_id": 1,
        "card_ids": [1, 2, 3, 4, 5],
        "card_names": ["Lightning Bolt", "Counterspell", "Brainstorm", "Ponder", "Island"]
    }
    
    async with httpx.AsyncClient() as client:
        response = await client.post(
            f"{BASE_URL}/embed/hand",
            json=hand_data
        )
        print(f"Hand embedding: {response.status_code}")
        if response.status_code == 200:
            result = response.json()
            print(f"Embedding dimension: {len(result['embedding'])}")
            print(f"Model version: {result['model_version']}")
        else:
            print(f"Error: {response.text}")
        return response.status_code == 200

async def test_recompute_all():
    """Test recompute all embeddings endpoint"""
    print("\nTesting recompute all embeddings...")
    
    async with httpx.AsyncClient(timeout=300.0) as client:  # 5 minute timeout
        response = await client.post(f"{BASE_URL}/recompute/all")
        print(f"Recompute all: {response.status_code}")
        if response.status_code == 200:
            result = response.json()
            print(f"Status: {result['status']}")
            print(f"Cards processed: {result['cards_processed']}")
            print(f"Hands processed: {result['hands_processed']}")
            print(f"Errors: {len(result['errors'])}")
            print(f"Duration: {result['duration_seconds']:.2f} seconds")
            if result['errors']:
                print("Errors:")
                for error in result['errors'][:5]:  # Show first 5 errors
                    print(f"  - {error}")
        else:
            print(f"Error: {response.text}")
        return response.status_code == 200

async def main():
    """Run all tests"""
    print("MTG Mullagain ML Worker Test Suite")
    print("=" * 40)
    
    tests = [
        ("Health Check", test_health),
        ("Card Embedding", test_card_embedding),
        ("Hand Embedding", test_hand_embedding),
        ("Recompute All", test_recompute_all),
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


