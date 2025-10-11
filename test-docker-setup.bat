@echo off
echo MTG Mullagain Docker Compose Validation
echo ======================================

echo.
echo Checking Docker installation...
docker --version
if %errorlevel% neq 0 (
    echo ERROR: Docker is not installed or not in PATH
    exit /b 1
)

echo.
echo Checking Docker Compose...
docker compose version
if %errorlevel% neq 0 (
    echo ERROR: Docker Compose is not available
    exit /b 1
)

echo.
echo Validating docker-compose.yml...
docker compose config
if %errorlevel% neq 0 (
    echo ERROR: docker-compose.yml has syntax errors
    exit /b 1
)

echo.
echo Building containers...
docker compose build
if %errorlevel% neq 0 (
    echo ERROR: Failed to build containers
    exit /b 1
)

echo.
echo Starting services...
docker compose up -d
if %errorlevel% neq 0 (
    echo ERROR: Failed to start services
    exit /b 1
)

echo.
echo Waiting for services to be ready...
timeout /t 10 /nobreak >nul

echo.
echo Checking service status...
docker compose ps

echo.
echo Testing PostgreSQL...
docker compose exec postgres pg_isready -U mullagain -d mullagain
if %errorlevel% neq 0 (
    echo WARNING: PostgreSQL may not be ready yet
)

echo.
echo Testing Backend...
curl -f http://localhost:5000/health
if %errorlevel% neq 0 (
    echo WARNING: Backend may not be ready yet
)

echo.
echo Testing Frontend...
curl -f http://localhost:8080
if %errorlevel% neq 0 (
    echo WARNING: Frontend may not be ready yet
)

echo.
echo Validation complete!
echo.
echo Services running:
echo   PostgreSQL: localhost:5432
echo   Backend API: http://localhost:5000
echo   Frontend: http://localhost:8080
echo.
echo To stop services: docker compose down
echo To view logs: docker compose logs -f
echo.
pause


