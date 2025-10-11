#!/bin/bash

# Scryfall Data Loader Runner
# This script runs the data loader to fetch all cards from Scryfall and generate embeddings

set -e

echo "MTG Mullagain Scryfall Data Loader"
echo "=================================="

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "ERROR: Docker is not running. Please start Docker Desktop."
    exit 1
fi

# Check if Docker Compose is available
if ! docker compose version > /dev/null 2>&1; then
    echo "ERROR: Docker Compose is not available."
    exit 1
fi

echo "Starting services..."
# Start PostgreSQL and ML worker
docker compose up -d postgres ml-worker

echo "Waiting for services to be ready..."
# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL..."
timeout=60
while [ $timeout -gt 0 ]; do
    if docker compose exec postgres pg_isready -U mullagain -d mullagain > /dev/null 2>&1; then
        echo "PostgreSQL is ready"
        break
    fi
    sleep 2
    timeout=$((timeout - 2))
done

if [ $timeout -le 0 ]; then
    echo "ERROR: PostgreSQL failed to start within 60 seconds"
    exit 1
fi

# Wait for ML worker to be ready
echo "Waiting for ML worker..."
timeout=60
while [ $timeout -gt 0 ]; do
    if curl -f http://localhost:7000/health > /dev/null 2>&1; then
        echo "ML worker is ready"
        break
    fi
    sleep 2
    timeout=$((timeout - 2))
done

if [ $timeout -le 0 ]; then
    echo "ERROR: ML worker failed to start within 60 seconds"
    exit 1
fi

echo "Running data loader..."
# Run the data loader
docker compose --profile data-load run --rm data-loader

echo "Data loading completed!"
echo ""
echo "To view the loaded data:"
echo "  PostgreSQL: localhost:5432"
echo "  Database: mullagain"
echo "  User: mullagain"
echo "  Password: mullagain"
echo ""
echo "To stop services: docker compose down"


