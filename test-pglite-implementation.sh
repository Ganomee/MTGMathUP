#!/bin/bash

# PGlite Implementation Test Script
# Tests the local-first architecture with bi-directional sync

set -e  # Exit on error

BACKEND_URL="http://localhost:5000"
FRONTEND_URL="http://localhost:8080"

echo "================================================"
echo "  PGlite Implementation - Integration Tests    "
echo "================================================"
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

pass_count=0
fail_count=0

function test_pass() {
    echo -e "${GREEN}✓ PASS${NC}: $1"
    ((pass_count++))
}

function test_fail() {
    echo -e "${RED}✗ FAIL${NC}: $1"
    ((fail_count++))
}

function test_info() {
    echo -e "${YELLOW}ℹ INFO${NC}: $1"
}

echo "=== Pre-flight Checks ==="
echo ""

# Check if backend is running
echo "Testing backend connection..."
if curl -s -f "$BACKEND_URL/health" > /dev/null; then
    test_pass "Backend is running and healthy"
else
    test_fail "Backend is not accessible at $BACKEND_URL/health"
    exit 1
fi

# Check if frontend is running
echo "Testing frontend connection..."
if curl -s -f "$FRONTEND_URL" > /dev/null; then
    test_pass "Frontend is running and accessible"
else
    test_fail "Frontend is not accessible at $FRONTEND_URL"
    exit 1
fi

echo ""
echo "=== Backend API Tests ==="
echo ""

# Test 1: Create a deck via REST API
echo "Test 1: Create deck via REST API"
CREATE_RESPONSE=$(curl -s -X POST "$BACKEND_URL/api/decks" \
    -H "Content-Type: application/json" \
    -d '{"name":"Test Deck API","format":"Standard","owner":"Test User"}')

if echo "$CREATE_RESPONSE" | grep -q "Test Deck API"; then
    DECK_ID=$(echo "$CREATE_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    test_pass "Created deck via API (ID: ${DECK_ID:0:8}...)"
else
    test_fail "Failed to create deck via API"
    echo "Response: $CREATE_RESPONSE"
fi

# Test 2: Retrieve the deck
echo "Test 2: Retrieve deck via REST API"
if [ -n "$DECK_ID" ]; then
    GET_RESPONSE=$(curl -s "$BACKEND_URL/api/decks/$DECK_ID")
    if echo "$GET_RESPONSE" | grep -q "Test Deck API"; then
        test_pass "Retrieved deck via API"
    else
        test_fail "Failed to retrieve deck via API"
    fi
fi

# Test 3: List all decks
echo "Test 3: List all decks"
LIST_RESPONSE=$(curl -s "$BACKEND_URL/api/decks")
if echo "$LIST_RESPONSE" | grep -q "\["; then
    DECK_COUNT=$(echo "$LIST_RESPONSE" | grep -o '"id"' | wc -l)
    test_pass "Listed decks (found $DECK_COUNT deck(s))"
else
    test_fail "Failed to list decks"
fi

# Test 4: Update the deck
echo "Test 4: Update deck via REST API"
if [ -n "$DECK_ID" ]; then
    UPDATE_RESPONSE=$(curl -s -X PUT "$BACKEND_URL/api/decks/$DECK_ID" \
        -H "Content-Type: application/json" \
        -d '{"name":"Updated Test Deck","format":"Modern","owner":"Updated User"}')
    if echo "$UPDATE_RESPONSE" | grep -q "Updated Test Deck"; then
        test_pass "Updated deck via API"
    else
        test_fail "Failed to update deck via API"
    fi
fi

# Test 5: Delete the deck
echo "Test 5: Delete deck via REST API"
if [ -n "$DECK_ID" ]; then
    DELETE_RESPONSE=$(curl -s -X DELETE "$BACKEND_URL/api/decks/$DECK_ID" -w "%{http_code}")
    if [[ "$DELETE_RESPONSE" == *"200"* ]] || [[ "$DELETE_RESPONSE" == *"204"* ]]; then
        test_pass "Deleted deck via API"
    else
        test_fail "Failed to delete deck via API (HTTP ${DELETE_RESPONSE: -3})"
    fi
fi

echo ""
echo "=== Cards API Tests ==="
echo ""

# Test 6: Create a card
echo "Test 6: Create card via REST API"
# First create a deck for the card
DECK_RESPONSE=$(curl -s -X POST "$BACKEND_URL/api/decks" \
    -H "Content-Type: application/json" \
    -d '{"name":"Card Test Deck","format":"Commander","owner":"Test"}')
CARD_DECK_ID=$(echo "$DECK_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

if [ -n "$CARD_DECK_ID" ]; then
    CARD_RESPONSE=$(curl -s -X POST "$BACKEND_URL/api/cards" \
        -H "Content-Type: application/json" \
        -d "{\"name\":\"Lightning Bolt\",\"deckId\":\"$CARD_DECK_ID\",\"manaCost\":\"{R}\",\"type\":\"Instant\",\"cmc\":1}")
    
    if echo "$CARD_RESPONSE" | grep -q "Lightning Bolt"; then
        CARD_ID=$(echo "$CARD_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
        test_pass "Created card via API"
    else
        test_fail "Failed to create card via API"
        echo "Response: $CARD_RESPONSE"
    fi
fi

# Test 7: List cards
echo "Test 7: List all cards"
CARDS_LIST=$(curl -s "$BACKEND_URL/api/cards")
if echo "$CARDS_LIST" | grep -q "\["; then
    CARD_COUNT=$(echo "$CARDS_LIST" | grep -o '"id"' | wc -l)
    test_pass "Listed cards (found $CARD_COUNT card(s))"
else
    test_fail "Failed to list cards"
fi

# Clean up
if [ -n "$CARD_DECK_ID" ]; then
    curl -s -X DELETE "$BACKEND_URL/api/decks/$CARD_DECK_ID" > /dev/null
    test_info "Cleaned up test deck and card"
fi

echo ""
echo "=== Frontend Tests ==="
echo ""

# Test 8: Check if admin page loads
echo "Test 8: Admin page accessibility"
ADMIN_RESPONSE=$(curl -s "$FRONTEND_URL/admin")
if echo "$ADMIN_RESPONSE" | grep -q "Database Admin Panel"; then
    test_pass "Admin panel page loads"
else
    test_fail "Admin panel page does not load"
fi

# Test 9: Check for PGlite in build
echo "Test 9: PGlite library included in build"
if curl -s "$FRONTEND_URL/admin" | grep -qi "pglite\|PGlite"; then
    test_pass "PGlite appears to be loaded"
else
    test_info "PGlite reference not found in HTML (may be in bundled JS)"
fi

# Test 10: Check for required UI elements
echo "Test 10: Check for sync controls in admin UI"
if echo "$ADMIN_RESPONSE" | grep -q "PGlite Sync Manager\|Sync Manager"; then
    test_pass "Sync manager UI elements found"
else
    test_fail "Sync manager UI elements not found"
fi

echo ""
echo "=== Manual Test Instructions ==="
echo ""
test_info "Please open your browser and navigate to: $FRONTEND_URL/admin"
test_info ""
test_info "Manual tests to perform:"
test_info "1. Create a deck - should appear in Local PGlite view"
test_info "2. Wait 5 seconds for auto-sync"
test_info "3. Enable 'Show Server Comparison' checkbox"
test_info "4. Click 'Refresh Server' - deck should appear in server view"
test_info "5. Click 'Pause Sync' button"
test_info "6. Create another deck - should show '1 Pending'"
test_info "7. Click 'Resume Sync' - pending should go to 0"
test_info "8. Toggle between 'Local PGlite' and 'Server Direct' views"
test_info "9. Check Activity Log for sync events"
test_info "10. Try creating, editing, and deleting items"

echo ""
echo "================================================"
echo "                 TEST SUMMARY                   "
echo "================================================"
echo ""
echo -e "${GREEN}Passed:${NC} $pass_count"
echo -e "${RED}Failed:${NC} $fail_count"
echo ""

if [ $fail_count -eq 0 ]; then
    echo -e "${GREEN}✓ All automated tests passed!${NC}"
    echo ""
    echo "Next steps:"
    echo "1. Open $FRONTEND_URL/admin in your browser"
    echo "2. Perform the manual tests listed above"
    echo "3. Monitor the Activity Log for sync status"
    exit 0
else
    echo -e "${RED}✗ Some tests failed. Please review the output above.${NC}"
    exit 1
fi

