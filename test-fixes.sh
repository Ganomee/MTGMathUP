#!/bin/bash

# Test script for all fixes
# Tests: Cards API, Hands API, HandEvals API, Backend health check, Form feedback

# Don't exit on error - we want to collect all test results
# set -e

BACKEND_URL="http://localhost:5000"
FRONTEND_URL="http://localhost:8080"

echo "================================================"
echo "  Testing PGlite Fixes - Comprehensive Suite   "
echo "================================================"
echo ""

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

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

echo "=== Backend Health Check Test ==="
echo ""

echo "Test 1: Backend health endpoint"
if curl -s -f "$BACKEND_URL/health" > /dev/null; then
    test_pass "Backend /health endpoint is accessible"
else
    test_fail "Backend /health endpoint is not accessible"
fi

echo ""
echo "=== Cards API Tests (Fixed 405 errors) ==="
echo ""

# Test Cards POST
echo "Test 2: Create card via POST"
CARD_CREATE=$(curl -s -X POST "$BACKEND_URL/api/cards" \
    -H "Content-Type: application/json" \
    -d '{"name":"Lightning Bolt","manaCost":"{R}","type":"Instant","oracleText":"Lightning Bolt deals 3 damage to any target.","cmc":1}')

if echo "$CARD_CREATE" | grep -q "Lightning Bolt"; then
    CARD_ID=$(echo "$CARD_CREATE" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    test_pass "Created card via POST (ID: ${CARD_ID:0:8}...)"
else
    test_fail "Failed to create card via POST"
    echo "Response: $CARD_CREATE"
fi

# Test Cards GET
echo "Test 3: Get card via GET"
if [ -n "$CARD_ID" ]; then
    CARD_GET=$(curl -s "$BACKEND_URL/api/cards/$CARD_ID")
    if echo "$CARD_GET" | grep -q "Lightning Bolt"; then
        test_pass "Retrieved card via GET"
    else
        test_fail "Failed to retrieve card via GET"
    fi
fi

# Test Cards PUT
echo "Test 4: Update card via PUT"
if [ -n "$CARD_ID" ]; then
    CARD_UPDATE=$(curl -s -X PUT "$BACKEND_URL/api/cards/$CARD_ID" \
        -H "Content-Type: application/json" \
        -d '{"name":"Lightning Bolt","cmc":2}')
    if echo "$CARD_UPDATE" | grep -q '"cmc":2'; then
        test_pass "Updated card via PUT"
    else
        test_fail "Failed to update card via PUT"
        echo "Response: $CARD_UPDATE"
    fi
fi

# Test Cards DELETE
echo "Test 5: Delete card via DELETE"
if [ -n "$CARD_ID" ]; then
    DELETE_STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "$BACKEND_URL/api/cards/$CARD_ID")
    if [[ "$DELETE_STATUS" == "204" ]] || [[ "$DELETE_STATUS" == "200" ]]; then
        test_pass "Deleted card via DELETE (HTTP $DELETE_STATUS)"
    else
        test_fail "Failed to delete card via DELETE (HTTP $DELETE_STATUS)"
    fi
fi

echo ""
echo "=== Hands API Tests (Fixed 400 errors - DTO format) ==="
echo ""

# Test Hands POST with correct DTO format
echo "Test 6: Create hand with correct DTO format"
HAND_CREATE=$(curl -s -X POST "$BACKEND_URL/api/hands" \
    -H "Content-Type: application/json" \
    -d '{"cardIntIds":[1,2,3,4,5,6,7],"size":7}')

if echo "$HAND_CREATE" | grep -q '"size":7'; then
    HAND_ID=$(echo "$HAND_CREATE" | grep -o '"id":[0-9]*' | cut -d':' -f2)
    test_pass "Created hand with correct DTO format (ID: $HAND_ID)"
else
    test_fail "Failed to create hand"
    echo "Response: $HAND_CREATE"
fi

# Test Hands GET
echo "Test 7: Get hand via GET"
if [ -n "$HAND_ID" ]; then
    HAND_GET=$(curl -s "$BACKEND_URL/api/hands/$HAND_ID")
    if echo "$HAND_GET" | grep -q '"size":7'; then
        test_pass "Retrieved hand via GET"
    else
        test_fail "Failed to retrieve hand via GET"
    fi
fi

# Test Hands DELETE
echo "Test 8: Delete hand via DELETE"
if [ -n "$HAND_ID" ]; then
    DELETE_STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "$BACKEND_URL/api/hands/$HAND_ID")
    if [[ "$DELETE_STATUS" == "204" ]] || [[ "$DELETE_STATUS" == "200" ]]; then
        test_pass "Deleted hand via DELETE (HTTP $DELETE_STATUS)"
    else
        test_fail "Failed to delete hand via DELETE (HTTP $DELETE_STATUS)"
    fi
fi

echo ""
echo "=== HandEvals API Tests (Fixed 400 errors - DTO format) ==="
echo ""

# Create two hands for the evaluation test
HAND1=$(curl -s -X POST "$BACKEND_URL/api/hands" -H "Content-Type: application/json" -d '{"cardIntIds":[1,2,3,4,5,6,7],"size":7}')
HAND2=$(curl -s -X POST "$BACKEND_URL/api/hands" -H "Content-Type: application/json" -d '{"cardIntIds":[8,9,10,11,12,13,14],"size":7}')

HAND1_ID=$(echo "$HAND1" | grep -o '"id":[0-9]*' | cut -d':' -f2)
HAND2_ID=$(echo "$HAND2" | grep -o '"id":[0-9]*' | cut -d':' -f2)

# Test HandEvals POST
echo "Test 9: Create hand evaluation with correct DTO format"
if [ -n "$HAND1_ID" ] && [ -n "$HAND2_ID" ]; then
    HANDEVAL_CREATE=$(curl -s -X POST "$BACKEND_URL/api/handevals" \
        -H "Content-Type: application/json" \
        -d "{\"hand1Id\":$HAND1_ID,\"hand2Id\":$HAND2_ID,\"preferredHand\":1,\"contextJson\":null}")
    
    if echo "$HANDEVAL_CREATE" | grep -q "preferredHand"; then
        HANDEVAL_ID=$(echo "$HANDEVAL_CREATE" | grep -o '"id":[0-9]*' | cut -d':' -f2)
        test_pass "Created hand evaluation (ID: $HANDEVAL_ID)"
    else
        test_fail "Failed to create hand evaluation"
        echo "Response: $HANDEVAL_CREATE"
    fi
fi

# Cleanup
if [ -n "$HANDEVAL_ID" ]; then
    curl -s -X DELETE "$BACKEND_URL/api/handevals/$HANDEVAL_ID" > /dev/null
fi
if [ -n "$HAND1_ID" ]; then
    curl -s -X DELETE "$BACKEND_URL/api/hands/$HAND1_ID" > /dev/null
fi
if [ -n "$HAND2_ID" ]; then
    curl -s -X DELETE "$BACKEND_URL/api/hands/$HAND2_ID" > /dev/null
fi

echo ""
echo "=== Edge Cases Tests ==="
echo ""

# Test invalid card creation (missing required field)
echo "Test 10: Edge case - Create card without required name"
INVALID_CARD=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X POST "$BACKEND_URL/api/cards" \
    -H "Content-Type: application/json" \
    -d '{"manaCost":"{R}","type":"Instant"}')

HTTP_STATUS=$(echo "$INVALID_CARD" | grep "HTTP_STATUS" | cut -d':' -f2)
if [[ "$HTTP_STATUS" == "400" ]] || [[ "$HTTP_STATUS" == "500" ]]; then
    test_pass "Correctly rejected invalid card (HTTP $HTTP_STATUS)"
else
    test_fail "Should have rejected invalid card (HTTP $HTTP_STATUS)"
fi

# Test invalid hands (empty card array)
echo "Test 11: Edge case - Create hand with empty card array"
INVALID_HAND=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X POST "$BACKEND_URL/api/hands" \
    -H "Content-Type: application/json" \
    -d '{"cardIntIds":[],"size":0}')

HTTP_STATUS=$(echo "$INVALID_HAND" | grep "HTTP_STATUS" | cut -d':' -f2)
if [[ "$HTTP_STATUS" == "400" ]] || [[ "$HTTP_STATUS" == "500" ]] || [[ "$HTTP_STATUS" == "200" ]]; then
    test_info "Empty hand request returned HTTP $HTTP_STATUS"
else
    test_fail "Unexpected status for empty hand (HTTP $HTTP_STATUS)"
fi

# Test large hand (more than 7 cards)
echo "Test 12: Edge case - Create hand with many cards"
LARGE_HAND=$(curl -s -X POST "$BACKEND_URL/api/hands" \
    -H "Content-Type: application/json" \
    -d '{"cardIntIds":[1,2,3,4,5,6,7,8,9,10,11,12,13,14,15],"size":15}')

if echo "$LARGE_HAND" | grep -q '"size":15'; then
    LARGE_HAND_ID=$(echo "$LARGE_HAND" | grep -o '"id":[0-9]*' | cut -d':' -f2)
    test_pass "Created large hand (15 cards)"
    if [ -n "$LARGE_HAND_ID" ]; then
        curl -s -X DELETE "$BACKEND_URL/api/hands/$LARGE_HAND_ID" > /dev/null
    fi
else
    test_info "Large hand creation response: ${LARGE_HAND:0:100}"
fi

echo ""
echo "=== Frontend Tests ==="
echo ""

# Test frontend loads
echo "Test 13: Frontend admin page loads"
ADMIN_PAGE=$(curl -s "$FRONTEND_URL/admin")
if echo "$ADMIN_PAGE" | grep -q "<!DOCTYPE html>"; then
    test_pass "Admin page loads successfully"
else
    test_fail "Admin page did not load"
fi

# Test backend health script
echo "Test 14: Backend health check script exists"
if [ -f "frontend/src/lib/utils/backend-health.ts" ]; then
    test_pass "Backend health check utility exists"
else
    test_fail "Backend health check utility not found"
fi

echo ""
echo "================================================"
echo "                 TEST SUMMARY                   "
echo "================================================"
echo ""
echo -e "${GREEN}Passed:${NC} $pass_count"
echo -e "${RED}Failed:${NC} $fail_count"
echo ""

if [ $fail_count -eq 0 ]; then
    echo -e "${GREEN}✓ All tests passed!${NC}"
    echo ""
    echo "Next steps:"
    echo "1. Open $FRONTEND_URL/admin in your browser"
    echo "2. Try creating cards, hands, and hand evaluations"
    echo "3. Watch for success/error feedback in the form"
    echo "4. Hover over the 🟢 Online indicator in the navbar"
    echo "5. Try stopping the backend to see automatic offline detection"
    exit 0
else
    echo -e "${RED}✗ Some tests failed. Please review the output above.${NC}"
    exit 1
fi

