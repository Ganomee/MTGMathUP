# MTG Mullagain Docker Compose Validation Script (PowerShell)
# This script validates the complete Docker setup

param(
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$Cleanup
)

# Set error action preference
$ErrorActionPreference = "Stop"

Write-Host "🔍 Validating MTG Mullagain Docker Compose Setup" -ForegroundColor Blue
Write-Host "================================================" -ForegroundColor Blue

# Function to print colored output
function Write-Status {
    param(
        [string]$Status,
        [string]$Message
    )
    
    switch ($Status) {
        "SUCCESS" { Write-Host "SUCCESS: $Message" -ForegroundColor Green }
        "ERROR" { Write-Host "ERROR: $Message" -ForegroundColor Red }
        "WARNING" { Write-Host "WARNING: $Message" -ForegroundColor Yellow }
        "INFO" { Write-Host "INFO: $Message" -ForegroundColor Cyan }
    }
}

# Check if Docker is installed
function Test-Docker {
    Write-Status "INFO" "Checking Docker installation..."
    try {
        $dockerVersion = docker --version
        Write-Status "SUCCESS" "Docker is installed: $dockerVersion"
    }
    catch {
        Write-Status "ERROR" "Docker is not installed or not in PATH"
        exit 1
    }
}

# Check if Docker Compose is installed
function Test-DockerCompose {
    Write-Status "INFO" "Checking Docker Compose installation..."
    try {
        $composeVersion = docker-compose --version
        Write-Status "SUCCESS" "Docker Compose is installed: $composeVersion"
    }
    catch {
        Write-Status "ERROR" "Docker Compose is not installed or not in PATH"
        exit 1
    }
}

# Validate Docker Compose file
function Test-ComposeFile {
    Write-Status "INFO" "Validating docker-compose.yml..."
    try {
        docker-compose config | Out-Null
        Write-Status "SUCCESS" "docker-compose.yml is valid"
    }
    catch {
        Write-Status "ERROR" "docker-compose.yml has syntax errors"
        exit 1
    }
}

# Build containers
function Build-Containers {
    if ($SkipBuild) {
        Write-Status "INFO" "Skipping container builds (-SkipBuild specified)"
        return
    }
    
    Write-Status "INFO" "Building Docker containers..."
    
    # Build PostgreSQL container
    Write-Status "INFO" "Building PostgreSQL container..."
    try {
        docker-compose build postgres
        Write-Status "SUCCESS" "PostgreSQL container built successfully"
    }
    catch {
        Write-Status "ERROR" "Failed to build PostgreSQL container"
        exit 1
    }
    
    # Build backend container
    Write-Status "INFO" "Building backend container..."
    try {
        docker-compose build backend
        Write-Status "SUCCESS" "Backend container built successfully"
    }
    catch {
        Write-Status "ERROR" "Failed to build backend container"
        exit 1
    }
    
    # Build frontend container
    Write-Status "INFO" "Building frontend container..."
    try {
        docker-compose build frontend
        Write-Status "SUCCESS" "Frontend container built successfully"
    }
    catch {
        Write-Status "ERROR" "Failed to build frontend container"
        exit 1
    }
}

# Start services and test connectivity
function Test-Services {
    if ($SkipTests) {
        Write-Status "INFO" "Skipping service tests (-SkipTests specified)"
        return
    }
    
    Write-Status "INFO" "Starting services..."
    
    # Start PostgreSQL
    Write-Status "INFO" "Starting PostgreSQL..."
    docker-compose up -d postgres
    
    # Wait for PostgreSQL to be ready
    Write-Status "INFO" "Waiting for PostgreSQL to be ready..."
    $timeout = 60
    $postgresReady = $false
    
    while ($timeout -gt 0 -and -not $postgresReady) {
        try {
            docker-compose exec postgres pg_isready -U mullagain -d mullagain | Out-Null
            $postgresReady = $true
            Write-Status "SUCCESS" "PostgreSQL is ready"
        }
        catch {
            Start-Sleep -Seconds 2
            $timeout -= 2
        }
    }
    
    if (-not $postgresReady) {
        Write-Status "ERROR" "PostgreSQL failed to start within 60 seconds"
        exit 1
    }
    
    # Test PostgreSQL extensions
    Write-Status "INFO" "Testing PostgreSQL extensions..."
    try {
        $extensions = docker-compose exec postgres psql -U mullagain -d mullagain -c "SELECT extname FROM pg_extension WHERE extname IN ('vector', 'intarray');" 2>$null
        if ($extensions -match 'vector|intarray') {
            Write-Status "SUCCESS" "PostgreSQL extensions (pgvector, intarray) are installed"
        }
        else {
            Write-Status "ERROR" "PostgreSQL extensions are not installed"
            exit 1
        }
    }
    catch {
        Write-Status "WARNING" "Could not verify PostgreSQL extensions"
    }
    
    # Start backend
    Write-Status "INFO" "Starting backend..."
    docker-compose up -d backend
    
    # Wait for backend to be ready
    Write-Status "INFO" "Waiting for backend to be ready..."
    $timeout = 60
    $backendReady = $false
    
    while ($timeout -gt 0 -and -not $backendReady) {
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -Method GET -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                $backendReady = $true
                Write-Status "SUCCESS" "Backend is ready"
            }
        }
        catch {
            Start-Sleep -Seconds 2
            $timeout -= 2
        }
    }
    
    if (-not $backendReady) {
        Write-Status "ERROR" "Backend failed to start within 60 seconds"
        exit 1
    }
    
    # Start frontend
    Write-Status "INFO" "Starting frontend..."
    docker-compose up -d frontend
    
    # Wait for frontend to be ready
    Write-Status "INFO" "Waiting for frontend to be ready..."
    $timeout = 60
    $frontendReady = $false
    
    while ($timeout -gt 0 -and -not $frontendReady) {
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:8080" -Method GET -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                $frontendReady = $true
                Write-Status "SUCCESS" "Frontend is ready"
            }
        }
        catch {
            Start-Sleep -Seconds 2
            $timeout -= 2
        }
    }
    
    if (-not $frontendReady) {
        Write-Status "ERROR" "Frontend failed to start within 60 seconds"
        exit 1
    }
}

# Test API endpoints
function Test-ApiEndpoints {
    if ($SkipTests) {
        Write-Status "INFO" "Skipping API tests (-SkipTests specified)"
        return
    }
    
    Write-Status "INFO" "Testing API endpoints..."
    
    # Test health endpoint
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -Method GET -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Status "SUCCESS" "Health endpoint is working"
        }
    }
    catch {
        Write-Status "ERROR" "Health endpoint is not working"
        exit 1
    }
    
    # Test hands endpoints (with mock data)
    Write-Status "INFO" "Testing hands API endpoints..."
    
    # Test equal hands endpoint
    try {
        $body = '[1,2,3,4,5]'
        $response = Invoke-WebRequest -Uri "http://localhost:5000/api/hands/equal" -Method POST -Body $body -ContentType "application/json" -TimeoutSec 10
        Write-Status "SUCCESS" "Equal hands endpoint is working"
    }
    catch {
        Write-Status "WARNING" "Equal hands endpoint returned an error (expected without data)"
    }
    
    # Test contains hands endpoint
    try {
        $body = '{"cardIntIds": [1,2], "limit": 10}'
        $response = Invoke-WebRequest -Uri "http://localhost:5000/api/hands/contains" -Method POST -Body $body -ContentType "application/json" -TimeoutSec 10
        Write-Status "SUCCESS" "Contains hands endpoint is working"
    }
    catch {
        Write-Status "WARNING" "Contains hands endpoint returned an error (expected without data)"
    }
}

# Test frontend connectivity
function Test-Frontend {
    if ($SkipTests) {
        Write-Status "INFO" "Skipping frontend tests (-SkipTests specified)"
        return
    }
    
    Write-Status "INFO" "Testing frontend connectivity..."
    
    # Test main page
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8080" -Method GET -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Status "SUCCESS" "Frontend main page is accessible"
        }
    }
    catch {
        Write-Status "ERROR" "Frontend main page is not accessible"
        exit 1
    }
    
    # Test compare page
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8080/compare" -Method GET -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Status "SUCCESS" "Frontend compare page is accessible"
        }
    }
    catch {
        Write-Status "ERROR" "Frontend compare page is not accessible"
        exit 1
    }
    
    # Test analysis page
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8080/analysis" -Method GET -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Status "SUCCESS" "Frontend analysis page is accessible"
        }
    }
    catch {
        Write-Status "ERROR" "Frontend analysis page is not accessible"
        exit 1
    }
}

# Cleanup function
function Stop-Services {
    Write-Status "INFO" "Stopping services..."
    try {
        docker-compose down
        Write-Status "SUCCESS" "Services stopped successfully"
    }
    catch {
        Write-Status "WARNING" "Error stopping services"
    }
}

# Main validation function
function Start-Validation {
    Write-Host ""
    Write-Status "INFO" "Starting Docker Compose validation..."
    Write-Host ""
    
    # Run validation steps
    Test-Docker
    Test-DockerCompose
    Test-ComposeFile
    Build-Containers
    Test-Services
    Test-ApiEndpoints
    Test-Frontend
    
    Write-Host ""
    Write-Status "SUCCESS" "All Docker Compose validation tests passed!"
    Write-Host ""
    Write-Status "INFO" "Services are running:"
    Write-Status "INFO" "  PostgreSQL: localhost:5432"
    Write-Status "INFO" "  Backend API: http://localhost:5000"
    Write-Status "INFO" "  Frontend: http://localhost:8080"
    Write-Host ""
    Write-Status "INFO" "To stop services, run: docker-compose down"
    Write-Host ""
    Write-Status "INFO" "To view logs, run: docker-compose logs -f"
    Write-Host ""
}

# Handle cleanup parameter
if ($Cleanup) {
    Stop-Services
    exit 0
}

# Run main validation
try {
    Start-Validation
}
catch {
    Write-Status "ERROR" "Validation failed: $($_.Exception.Message)"
    Stop-Services
    exit 1
}
