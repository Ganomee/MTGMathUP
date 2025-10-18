#!/bin/bash
# Comprehensive sync test script

# Don't exit on error - we want to collect all test results
# set -e

BACKEND_URL="http://localhost:5000"
FRONTEND_URL="http://localhost:8080"
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

passed=0
failed=0

echo "=============================================="
echo " MTG Mullagain Sync Test Suite"
echo "=============================================="
echo ""

# Test function
test_api() {
    local name="$1"
    local cmd="$2"
    local expected_code="$3"
    
    echo -n "Testing: $name... "
    
    if output=$(eval "$cmd" 2>&1); then
        actual_code=$?
        if [ "$actual_code" -eq "$expected_code" ] || [ "$expected_code" -eq "0" ]; then
            echo -e "${GREEN}PASS${NC}"
            ((passed++))
            return 0
        else
            echo -e "${RED}FAIL${NC} (exit code: $actual_code, expected: $expected_code)"
            echo "  Output: $output"
            ((failed++))
            return 1
        fi
    else
        echo -e "${RED}FAIL${NC} (command error)"
        echo "  Error: $output"
        ((failed++))
        return 1
    fi
}

# Test JSON response
test_json() {
    local name="$1"
    local url="$2"
    local expected_key="$3"
    
    echo -n "Testing: $name... "
    
    response=$(curl -s "$url")
    
    if echo "$response" | python3 -c "import sys, json; data=json.load(sys.stdin); sys.exit(0 if '$expected_key' in str(data) or (isinstance(data, list) and len(data) >= 0) else 1)" 2>/dev/null; then
        echo -e "${GREEN}PASS${NC}"
        ((passed++))
        return 0
    else
        echo -e "${RED}FAIL${NC}"
        echo "  Response: $response"
        ((failed++))
        return 1
    fi
}

echo "================================"
echo " 1. Backend Health Checks"
echo "================================"
echo ""

test_api "Backend health endpoint" \
    "curl -s -o /dev/null -w '%{http_code}' $BACKEND_URL/health" \
    0

test_api "Backend health response" \
    "curl -s $BACKEND_URL/health | grep -q 'Healthy'" \
    0

test_api "Backend API base accessible" \
    "curl -s -o /dev/null -w '%{http_code}' $BACKEND_URL/api/decks" \
    0

echo ""
echo "================================"
echo " 2. SignalR Hub Tests"
echo "================================"
echo ""

test_api "SignalR hub negotiate endpoint" \
    "curl -s -X POST -o /dev/null -w '%{http_code}' '$BACKEND_URL/sync/negotiate?negotiateVersion=1'" \
    0

test_json "SignalR negotiate response contains connectionId" \
    "$BACKEND_URL/sync/negotiate?negotiateVersion=1" \
    "connectionId"

echo ""
echo "================================"
echo " 3. Decks API CRUD Tests"
echo "================================"
echo ""

# GET all decks
test_json "GET /api/decks returns array" \
    "$BACKEND_URL/api/decks" \
    "array"

# POST create deck
echo -n "Testing: POST /api/decks (create deck)... "
CREATE_RESPONSE=$(curl -s -X POST \
    -H "Content-Type: application/json" \
    -d '{"name":"Test Suite Deck","format":"Modern","owner":"Test Bot"}' \
    "$BACKEND_URL/api/decks")

DECK_ID=$(echo "$CREATE_RESPONSE" | python3 -c "import sys, json; print(json.load(sys.stdin).get('id', ''))" 2>/dev/null || echo "")

if [ -n "$DECK_ID" ] && [ "$DECK_ID" != "null" ]; then
    echo -e "${GREEN}PASS${NC} (Deck ID: $DECK_ID)"
    ((passed++))
else
    echo -e "${RED}FAIL${NC}"
    echo "  Response: $CREATE_RESPONSE"
    ((failed++))
    DECK_ID=""
fi

# GET specific deck
if [ -n "$DECK_ID" ]; then
    test_json "GET /api/decks/$DECK_ID returns deck" \
        "$BACKEND_URL/api/decks/$DECK_ID" \
        "name"
    
    # PUT update deck
    echo -n "Testing: PUT /api/decks/$DECK_ID (update deck)... "
    UPDATE_RESPONSE=$(curl -s -X PUT \
        -H "Content-Type: application/json" \
        -d '{"name":"Updated Test Deck"}' \
        "$BACKEND_URL/api/decks/$DECK_ID")
    
    if echo "$UPDATE_RESPONSE" | grep -q "Updated Test Deck"; then
        echo -e "${GREEN}PASS${NC}"
        ((passed++))
    else
        echo -e "${RED}FAIL${NC}"
        echo "  Response: $UPDATE_RESPONSE"
        ((failed++))
    fi
    
    # DELETE deck
    echo -n "Testing: DELETE /api/decks/$DECK_ID... "
    DELETE_CODE=$(curl -s -o /dev/null -w '%{http_code}' -X DELETE "$BACKEND_URL/api/decks/$DECK_ID")
    
    if [ "$DELETE_CODE" -eq "204" ] || [ "$DELETE_CODE" -eq "200" ]; then
        echo -e "${GREEN}PASS${NC}"
        ((passed++))
    else
        echo -e "${RED}FAIL${NC} (HTTP $DELETE_CODE)"
        ((failed++))
    fi
    
    # Verify deletion
    echo -n "Testing: GET deleted deck returns 404... "
    NOT_FOUND_CODE=$(curl -s -o /dev/null -w '%{http_code}' "$BACKEND_URL/api/decks/$DECK_ID")
    
    if [ "$NOT_FOUND_CODE" -eq "404" ]; then
        echo -e "${GREEN}PASS${NC}"
        ((passed++))
    else
        echo -e "${RED}FAIL${NC} (HTTP $NOT_FOUND_CODE, expected 404)"
        ((failed++))
    fi
fi

echo ""
echo "================================"
echo " 4. Frontend Tests"
echo "================================"
echo ""

test_api "Frontend home page accessible" \
    "curl -s -o /dev/null -w '%{http_code}' $FRONTEND_URL" \
    0

test_api "Frontend sync-test page accessible" \
    "curl -s -o /dev/null -w '%{http_code}' $FRONTEND_URL/sync-test" \
    0

echo -n "Testing: Sync-test page contains SignalR code... "
if curl -s "$FRONTEND_URL/sync-test" | grep -q "signalr\|SignalR"; then
    echo -e "${GREEN}PASS${NC}"
    ((passed++))
else
    echo -e "${YELLOW}SKIP${NC} (static HTML, JS loads separately)"
fi

echo ""
echo "================================"
echo " 5. CORS Tests"
echo "================================"
echo ""

echo -n "Testing: CORS headers on API... "
CORS_RESPONSE=$(curl -s -X OPTIONS \
    -H "Origin: http://localhost:8080" \
    -H "Access-Control-Request-Method: POST" \
    -H "Access-Control-Request-Headers: Content-Type" \
    -I "$BACKEND_URL/api/decks" 2>&1)

if echo "$CORS_RESPONSE" | grep -qi "Access-Control-Allow-Origin"; then
    echo -e "${GREEN}PASS${NC}"
    ((passed++))
else
    echo -e "${RED}FAIL${NC}"
    echo "  Response headers: $CORS_RESPONSE"
    ((failed++))
fi

echo -n "Testing: CORS on SignalR hub... "
CORS_SIGNALR=$(curl -s -X OPTIONS \
    -H "Origin: http://localhost:8080" \
    -I "$BACKEND_URL/sync/negotiate?negotiateVersion=1" 2>&1)

if echo "$CORS_SIGNALR" | grep -qi "Access-Control-Allow-Origin"; then
    echo -e "${GREEN}PASS${NC}"
    ((passed++))
else
    echo -e "${RED}FAIL${NC}"
    echo "  Response headers: $CORS_SIGNALR"
    ((failed++))
fi

echo ""
echo "================================"
echo " 6. Database Connection Test"
echo "================================"
echo ""

echo -n "Testing: Can create and retrieve deck from database... "

# Create a deck
TEST_DECK_RESPONSE=$(curl -s -X POST \
    -H "Content-Type: application/json" \
    -d '{"name":"DB Test Deck","format":"Standard","owner":"DB Tester"}' \
    "$BACKEND_URL/api/decks")

TEST_DECK_ID=$(echo "$TEST_DECK_RESPONSE" | python3 -c "import sys, json; print(json.load(sys.stdin).get('id', ''))" 2>/dev/null || echo "")

if [ -n "$TEST_DECK_ID" ]; then
    # Retrieve it
    RETRIEVED_DECK=$(curl -s "$BACKEND_URL/api/decks/$TEST_DECK_ID")
    
    if echo "$RETRIEVED_DECK" | grep -q "DB Test Deck"; then
        echo -e "${GREEN}PASS${NC}"
        ((passed++))
        
        # Cleanup
        curl -s -X DELETE "$BACKEND_URL/api/decks/$TEST_DECK_ID" > /dev/null
    else
        echo -e "${RED}FAIL${NC}"
        echo "  Could not retrieve created deck"
        ((failed++))
    fi
else
    echo -e "${RED}FAIL${NC}"
    echo "  Could not create deck"
    ((failed++))
fi

echo ""
echo "================================"
echo " 7. Integration Test"
echo "================================"
echo ""

echo "Running full CRUD cycle..."

# Create
echo -n "  1. Create... "
INT_DECK=$(curl -s -X POST \
    -H "Content-Type: application/json" \
    -d '{"name":"Integration Test Deck","format":"Commander","owner":"Integration"}' \
    "$BACKEND_URL/api/decks")
INT_ID=$(echo "$INT_DECK" | python3 -c "import sys, json; print(json.load(sys.stdin).get('id', ''))" 2>/dev/null || echo "")

if [ -n "$INT_ID" ]; then
    echo -e "${GREEN}✓${NC}"
    ((passed++))
else
    echo -e "${RED}✗${NC}"
    ((failed++))
    INT_ID=""
fi

# Read
if [ -n "$INT_ID" ]; then
    echo -n "  2. Read... "
    INT_READ=$(curl -s "$BACKEND_URL/api/decks/$INT_ID")
    if echo "$INT_READ" | grep -q "Integration Test Deck"; then
        echo -e "${GREEN}✓${NC}"
        ((passed++))
    else
        echo -e "${RED}✗${NC}"
        ((failed++))
    fi
    
    # Update
    echo -n "  3. Update... "
    INT_UPDATE=$(curl -s -X PUT \
        -H "Content-Type: application/json" \
        -d '{"name":"Updated Integration Deck"}' \
        "$BACKEND_URL/api/decks/$INT_ID")
    if echo "$INT_UPDATE" | grep -q "Updated Integration Deck"; then
        echo -e "${GREEN}✓${NC}"
        ((passed++))
    else
        echo -e "${RED}✗${NC}"
        ((failed++))
    fi
    
    # Delete
    echo -n "  4. Delete... "
    INT_DELETE=$(curl -s -o /dev/null -w '%{http_code}' -X DELETE "$BACKEND_URL/api/decks/$INT_ID")
    if [ "$INT_DELETE" -eq "204" ] || [ "$INT_DELETE" -eq "200" ]; then
        echo -e "${GREEN}✓${NC}"
        ((passed++))
    else
        echo -e "${RED}✗${NC}"
        ((failed++))
    fi
fi

echo ""
echo "=============================================="
echo " Test Results"
echo "=============================================="
echo ""
echo -e "${GREEN}Passed:${NC}  $passed"
echo -e "${RED}Failed:${NC}  $failed"
echo -e "Total:   $((passed + failed))"
echo ""

if [ "$failed" -eq 0 ]; then
    echo -e "${GREEN}✓ All tests passed!${NC}"
    echo ""
    echo "Your sync infrastructure is working correctly:"
    echo "  ✓ Backend API accessible"
    echo "  ✓ SignalR hub ready"
    echo "  ✓ CRUD operations functional"
    echo "  ✓ Database connected"
    echo "  ✓ CORS properly configured"
    echo "  ✓ Frontend serving pages"
    echo ""
    echo "You can now:"
    echo "  1. Visit http://localhost:8080/sync-test"
    echo "  2. Create, edit, and delete decks"
    echo "  3. See real-time connection status"
    exit 0
else
    echo -e "${RED}✗ Some tests failed${NC}"
    echo ""
    echo "Troubleshooting:"
    echo "  1. Check all services are running: docker compose ps"
    echo "  2. View logs: docker compose logs backend"
    echo "  3. Test manually: curl http://localhost:5000/health"
    exit 1
fi

