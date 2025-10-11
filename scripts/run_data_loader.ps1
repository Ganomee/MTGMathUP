# Scryfall Data Loader Runner (PowerShell)
# This script runs the data loader to fetch all cards from Scryfall and generate embeddings

param(
    [switch]$SkipBuild,
    [switch]$SkipServices
)

$ErrorActionPreference = "Stop"

Write-Host "MTG Mullagain Scryfall Data Loader" -ForegroundColor Blue
Write-Host "==================================" -ForegroundColor Blue

# Check if Docker is running
try {
    docker info | Out-Null
    Write-Host "Docker is running" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check if Docker Compose is available
try {
    docker compose version | Out-Null
    Write-Host "Docker Compose is available" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Docker Compose is not available." -ForegroundColor Red
    exit 1
}

if (-not $SkipServices) {
    Write-Host "Starting services..." -ForegroundColor Yellow
    # Start PostgreSQL and ML worker
    docker compose up -d postgres ml-worker

    Write-Host "Waiting for services to be ready..." -ForegroundColor Yellow
    
    # Wait for PostgreSQL to be ready
    Write-Host "Waiting for PostgreSQL..." -ForegroundColor Cyan
    $timeout = 60
    $postgresReady = $false
    
    while ($timeout -gt 0 -and -not $postgresReady) {
        try {
            docker compose exec postgres pg_isready -U mullagain -d mullagain | Out-Null
            $postgresReady = $true
            Write-Host "PostgreSQL is ready" -ForegroundColor Green
        }
        catch {
            Start-Sleep -Seconds 2
            $timeout -= 2
        }
    }
    
    if (-not $postgresReady) {
        Write-Host "ERROR: PostgreSQL failed to start within 60 seconds" -ForegroundColor Red
        exit 1
    }
    
    # Wait for ML worker to be ready
    Write-Host "Waiting for ML worker..." -ForegroundColor Cyan
    $timeout = 60
    $mlWorkerReady = $false
    
    while ($timeout -gt 0 -and -not $mlWorkerReady) {
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:7000/health" -Method GET -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                $mlWorkerReady = $true
                Write-Host "ML worker is ready" -ForegroundColor Green
            }
        }
        catch {
            Start-Sleep -Seconds 2
            $timeout -= 2
        }
    }
    
    if (-not $mlWorkerReady) {
        Write-Host "ERROR: ML worker failed to start within 60 seconds" -ForegroundColor Red
        exit 1
    }
}

Write-Host "Running data loader..." -ForegroundColor Yellow

# Build the data loader if not skipped
if (-not $SkipBuild) {
    Write-Host "Building data loader..." -ForegroundColor Cyan
    docker compose build data-loader
}

# Run the data loader
try {
    docker compose --profile data-load run --rm data-loader
    Write-Host "Data loading completed!" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Data loading failed" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "To view the loaded data:" -ForegroundColor Cyan
Write-Host "  PostgreSQL: localhost:5432" -ForegroundColor White
Write-Host "  Database: mullagain" -ForegroundColor White
Write-Host "  User: mullagain" -ForegroundColor White
Write-Host "  Password: mullagain" -ForegroundColor White
Write-Host ""
Write-Host "To stop services: docker compose down" -ForegroundColor Cyan


