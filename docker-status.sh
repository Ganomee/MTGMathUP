#!/bin/bash
# Check Docker container status and health

set -e

echo "=============================================="
echo " Docker Container Status"
echo "=============================================="
echo ""

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo "Checking container status..."
echo ""

# Check if docker compose is running
if ! docker compose ps > /dev/null 2>&1; then
    echo -e "${RED}✗ Docker Compose is not running or not accessible${NC}"
    echo ""
    echo "Try running: docker compose up -d"
    exit 1
fi

# Get container status
echo "Container Status:"
echo "----------------------------------------"
docker compose ps --format "table {{.Name}}\t{{.Status}}\t{{.Ports}}"
echo ""

# Check individual services
echo "Service Health Status:"
echo "----------------------------------------"

# Function to check service health
check_service() {
    service=$1
    name=$2
    
    status=$(docker compose ps $service --format "{{.Status}}" 2>/dev/null || echo "not found")
    
    if echo "$status" | grep -q "Up"; then
        if echo "$status" | grep -q "healthy"; then
            echo -e "${GREEN}✓${NC} $name: ${GREEN}Healthy${NC}"
            return 0
        elif echo "$status" | grep -q "unhealthy"; then
            echo -e "${RED}✗${NC} $name: ${RED}Unhealthy${NC}"
            echo "  Check logs: docker compose logs $service"
            return 1
        elif echo "$status" | grep -q "health: starting"; then
            echo -e "${YELLOW}⚠${NC} $name: ${YELLOW}Starting (health check pending)${NC}"
            return 2
        else
            echo -e "${YELLOW}⚠${NC} $name: ${YELLOW}Running (no health check)${NC}"
            return 0
        fi
    elif echo "$status" | grep -q "Exit"; then
        exit_code=$(echo "$status" | grep -oP 'Exit \K\d+' || echo "unknown")
        echo -e "${RED}✗${NC} $name: ${RED}Exited (code: $exit_code)${NC}"
        echo "  Check logs: docker compose logs $service"
        return 1
    else
        echo -e "${RED}✗${NC} $name: ${RED}Not running${NC}"
        return 1
    fi
}

# Check each service
check_service "postgres" "PostgreSQL"
postgres_status=$?

check_service "backend" "Backend API"
backend_status=$?

check_service "frontend" "Frontend"
frontend_status=$?

check_service "ml-worker" "ML Worker"
ml_status=$?

echo ""

# Service URLs
echo "Service URLs:"
echo "----------------------------------------"
echo -e "${BLUE}Backend API:${NC}  http://localhost:5000"
echo -e "  Health:      http://localhost:5000/health"
echo -e "  Swagger:     http://localhost:5000/swagger"
echo ""
echo -e "${BLUE}Frontend:${NC}     http://localhost:8080"
echo ""
echo -e "${BLUE}ML Worker:${NC}    http://localhost:7000"
echo -e "  Health:      http://localhost:7000/health"
echo ""
echo -e "${BLUE}Database:${NC}     postgresql://mullagain:mullagain@localhost:5432/mullagain"
echo ""

# Summary
echo "=============================================="
echo " Quick Actions"
echo "=============================================="
echo ""
echo "View logs:"
echo "  docker compose logs -f [service]"
echo ""
echo "Restart a service:"
echo "  docker compose restart [service]"
echo ""
echo "Rebuild and restart:"
echo "  docker compose build [service] && docker compose up -d [service]"
echo ""
echo "Stop all services:"
echo "  docker compose down"
echo ""
echo "Start all services:"
echo "  docker compose up -d"
echo ""

# Check if any services are unhealthy
if [ $backend_status -eq 1 ] || [ $ml_status -eq 1 ]; then
    echo -e "${YELLOW}Note: Some services are unhealthy. This may be due to:${NC}"
    echo "  1. Services still starting up (wait 30 seconds)"
    echo "  2. Missing dependencies or configuration"
    echo "  3. Healthcheck command not working"
    echo ""
    echo "To fix backend healthcheck, rebuild:"
    echo "  docker compose build backend && docker compose up -d backend"
    echo ""
fi

# GPU status for ML worker
if [ $ml_status -eq 0 ] || [ $ml_status -eq 2 ]; then
    echo "Check ML Worker GPU status:"
    echo "  docker compose exec ml-worker python -c \"import torch; print(f'CUDA: {torch.cuda.is_available()}')\""
    echo ""
fi

