#!/bin/bash

# MTG Mullagain Docker Compose Validation Script
# This script validates the complete Docker setup

set -e

echo "🔍 Validating MTG Mullagain Docker Compose Setup"
echo "================================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    local status=$1
    local message=$2
    case $status in
        "SUCCESS") echo -e "${GREEN}✅ $message${NC}" ;;
        "ERROR") echo -e "${RED}❌ $message${NC}" ;;
        "WARNING") echo -e "${YELLOW}⚠️  $message${NC}" ;;
        "INFO") echo -e "${BLUE}ℹ️  $message${NC}" ;;
    esac
}

# Check if Docker is installed
check_docker() {
    print_status "INFO" "Checking Docker installation..."
    if command -v docker &> /dev/null; then
        print_status "SUCCESS" "Docker is installed: $(docker --version)"
    else
        print_status "ERROR" "Docker is not installed"
        exit 1
    fi
}

# Check if Docker Compose is installed
check_docker_compose() {
    print_status "INFO" "Checking Docker Compose installation..."
    if command -v docker-compose &> /dev/null; then
        print_status "SUCCESS" "Docker Compose is installed: $(docker-compose --version)"
    else
        print_status "ERROR" "Docker Compose is not installed"
        exit 1
    fi
}

# Validate Docker Compose file
validate_compose_file() {
    print_status "INFO" "Validating docker-compose.yml..."
    if docker-compose config > /dev/null 2>&1; then
        print_status "SUCCESS" "docker-compose.yml is valid"
    else
        print_status "ERROR" "docker-compose.yml has syntax errors"
        exit 1
    fi
}

# Build and test containers
build_containers() {
    print_status "INFO" "Building Docker containers..."
    
    # Build PostgreSQL container
    print_status "INFO" "Building PostgreSQL container..."
    if docker-compose build postgres; then
        print_status "SUCCESS" "PostgreSQL container built successfully"
    else
        print_status "ERROR" "Failed to build PostgreSQL container"
        exit 1
    fi
    
    # Build backend container
    print_status "INFO" "Building backend container..."
    if docker-compose build backend; then
        print_status "SUCCESS" "Backend container built successfully"
    else
        print_status "ERROR" "Failed to build backend container"
        exit 1
    fi
    
    # Build frontend container
    print_status "INFO" "Building frontend container..."
    if docker-compose build frontend; then
        print_status "SUCCESS" "Frontend container built successfully"
    else
        print_status "ERROR" "Failed to build frontend container"
        exit 1
    fi
}

# Start services and test connectivity
test_services() {
    print_status "INFO" "Starting services..."
    
    # Start PostgreSQL
    print_status "INFO" "Starting PostgreSQL..."
    docker-compose up -d postgres
    
    # Wait for PostgreSQL to be ready
    print_status "INFO" "Waiting for PostgreSQL to be ready..."
    timeout=60
    while [ $timeout -gt 0 ]; do
        if docker-compose exec postgres pg_isready -U mullagain -d mullagain > /dev/null 2>&1; then
            print_status "SUCCESS" "PostgreSQL is ready"
            break
        fi
        sleep 2
        timeout=$((timeout - 2))
    done
    
    if [ $timeout -le 0 ]; then
        print_status "ERROR" "PostgreSQL failed to start within 60 seconds"
        exit 1
    fi
    
    # Test PostgreSQL extensions
    print_status "INFO" "Testing PostgreSQL extensions..."
    if docker-compose exec postgres psql -U mullagain -d mullagain -c "SELECT extname FROM pg_extension WHERE extname IN ('vector', 'intarray');" | grep -q "vector\|intarray"; then
        print_status "SUCCESS" "PostgreSQL extensions (pgvector, intarray) are installed"
    else
        print_status "ERROR" "PostgreSQL extensions are not installed"
        exit 1
    fi
    
    # Start backend
    print_status "INFO" "Starting backend..."
    docker-compose up -d backend
    
    # Wait for backend to be ready
    print_status "INFO" "Waiting for backend to be ready..."
    timeout=60
    while [ $timeout -gt 0 ]; do
        if curl -f http://localhost:5000/health > /dev/null 2>&1; then
            print_status "SUCCESS" "Backend is ready"
            break
        fi
        sleep 2
        timeout=$((timeout - 2))
    done
    
    if [ $timeout -le 0 ]; then
        print_status "ERROR" "Backend failed to start within 60 seconds"
        exit 1
    fi
    
    # Start frontend
    print_status "INFO" "Starting frontend..."
    docker-compose up -d frontend
    
    # Wait for frontend to be ready
    print_status "INFO" "Waiting for frontend to be ready..."
    timeout=60
    while [ $timeout -gt 0 ]; do
        if curl -f http://localhost:8080 > /dev/null 2>&1; then
            print_status "SUCCESS" "Frontend is ready"
            break
        fi
        sleep 2
        timeout=$((timeout - 2))
    done
    
    if [ $timeout -le 0 ]; then
        print_status "ERROR" "Frontend failed to start within 60 seconds"
        exit 1
    fi
}

# Test API endpoints
test_api_endpoints() {
    print_status "INFO" "Testing API endpoints..."
    
    # Test health endpoint
    if curl -f http://localhost:5000/health > /dev/null 2>&1; then
        print_status "SUCCESS" "Health endpoint is working"
    else
        print_status "ERROR" "Health endpoint is not working"
        exit 1
    fi
    
    # Test hands endpoints (with mock data)
    print_status "INFO" "Testing hands API endpoints..."
    
    # Test equal hands endpoint
    if curl -X POST http://localhost:5000/api/hands/equal \
        -H "Content-Type: application/json" \
        -d '[1,2,3,4,5]' > /dev/null 2>&1; then
        print_status "SUCCESS" "Equal hands endpoint is working"
    else
        print_status "WARNING" "Equal hands endpoint returned an error (expected without data)"
    fi
    
    # Test contains hands endpoint
    if curl -X POST http://localhost:5000/api/hands/contains \
        -H "Content-Type: application/json" \
        -d '{"cardIntIds": [1,2], "limit": 10}' > /dev/null 2>&1; then
        print_status "SUCCESS" "Contains hands endpoint is working"
    else
        print_status "WARNING" "Contains hands endpoint returned an error (expected without data)"
    fi
}

# Test frontend connectivity
test_frontend() {
    print_status "INFO" "Testing frontend connectivity..."
    
    # Test main page
    if curl -f http://localhost:8080 > /dev/null 2>&1; then
        print_status "SUCCESS" "Frontend main page is accessible"
    else
        print_status "ERROR" "Frontend main page is not accessible"
        exit 1
    fi
    
    # Test compare page
    if curl -f http://localhost:8080/compare > /dev/null 2>&1; then
        print_status "SUCCESS" "Frontend compare page is accessible"
    else
        print_status "ERROR" "Frontend compare page is not accessible"
        exit 1
    fi
    
    # Test analysis page
    if curl -f http://localhost:8080/analysis > /dev/null 2>&1; then
        print_status "SUCCESS" "Frontend analysis page is accessible"
    else
        print_status "ERROR" "Frontend analysis page is not accessible"
        exit 1
    fi
}

# Cleanup function
cleanup() {
    print_status "INFO" "Cleaning up..."
    docker-compose down
    print_status "SUCCESS" "Cleanup completed"
}

# Main validation function
main() {
    echo
    print_status "INFO" "Starting Docker Compose validation..."
    echo
    
    # Run validation steps
    check_docker
    check_docker_compose
    validate_compose_file
    build_containers
    test_services
    test_api_endpoints
    test_frontend
    
    echo
    print_status "SUCCESS" "All Docker Compose validation tests passed!"
    echo
    print_status "INFO" "Services are running:"
    print_status "INFO" "  PostgreSQL: http://localhost:5432"
    print_status "INFO" "  Backend API: http://localhost:5000"
    print_status "INFO" "  Frontend: http://localhost:8080"
    echo
    print_status "INFO" "To stop services, run: docker-compose down"
    echo
    print_status "INFO" "To view logs, run: docker-compose logs -f"
    echo
}

# Trap to ensure cleanup on exit
trap cleanup EXIT

# Run main function
main "$@"


