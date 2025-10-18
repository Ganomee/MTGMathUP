#!/bin/bash

# Test script to verify real offline mode is working
# This tests that API calls are actually blocked when offline

set -e

echo "======================================"
echo " Offline Mode Test Suite"
echo "======================================"
echo ""

BACKEND_URL="http://localhost:5000"
FRONTEND_URL="http://localhost:8080"

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo "Step 1: Verify backend is running..."
if curl -s -f "$BACKEND_URL/health" > /dev/null 2>&1; then
    echo -e "${GREEN}✓ Backend is online${NC}"
else
    echo -e "${RED}✗ Backend is not accessible${NC}"
    exit 1
fi

echo ""
echo "Step 2: Verify frontend is running..."
if curl -s -f "$FRONTEND_URL/admin" > /dev/null 2>&1; then
    echo -e "${GREEN}✓ Frontend is online${NC}"
else
    echo -e "${RED}✗ Frontend is not accessible${NC}"
    exit 1
fi

echo ""
echo "Step 3: Get current deck count..."
INITIAL_COUNT=$(curl -s "$BACKEND_URL/api/decks" | jq '. | length')
echo "Current decks in database: $INITIAL_COUNT"

echo ""
echo "======================================"
echo " Manual Testing Instructions"
echo "======================================"
echo ""
echo "1. Open the admin panel in your browser:"
echo "   $FRONTEND_URL/admin"
echo ""
echo "2. Click on the 'Decks' tab"
echo ""
echo "3. Click the '📡 Go Offline' button"
echo "   - Status should change to '🔴 OFFLINE'"
echo "   - Buttons should show '(Offline)' text"
echo ""
echo "4. Click '✨ Add New (Offline)'"
echo "   - Fill in:"
echo "     Name: Test Offline Deck"
echo "     Format: Standard"
echo "     Owner: Test User"
echo "   - Click '✨ Create (Offline)'"
echo ""
echo "5. Check the activity log at the bottom:"
echo "   - Should say: '🔌 Offline: Creating decks locally (no server call)'"
echo "   - Should say: '💾 Saved locally - 1 pending changes'"
echo ""
echo "6. Verify the new deck appears in the table"
echo "   - It should have a temporary ID"
echo ""
echo "7. Open a new tab and go to:"
echo "   $BACKEND_URL/api/decks"
echo "   - The new deck should NOT be there yet!"
echo "   - Count should still be: $INITIAL_COUNT"
echo ""
echo "8. Go back to the admin panel"
echo "   - Click '🔌 Go Online'"
echo "   - Watch the activity log:"
echo "     - Should say: '🌐 Going online - syncing pending changes...'"
echo "     - Should say: 'Syncing 1 pending changes...'"
echo "     - Should say: '✅ Synced create: decks item'"
echo "     - Should say: '✅ All pending changes synced'"
echo ""
echo "9. Refresh the backend API tab:"
echo "   $BACKEND_URL/api/decks"
echo "   - The new deck SHOULD be there now!"
echo "   - Count should be: $((INITIAL_COUNT + 1))"
echo ""
echo "======================================"
echo " Automated Verification (Run After)"
echo "======================================"
echo ""
echo "After completing the manual steps, run:"
echo "  curl -s $BACKEND_URL/api/decks | jq '. | length'"
echo ""
echo "Expected count: $((INITIAL_COUNT + 1))"
echo ""
echo "======================================"
echo " Test Complete"
echo "======================================"

