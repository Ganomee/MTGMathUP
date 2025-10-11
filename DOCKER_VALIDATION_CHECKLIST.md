# Docker Compose Validation Checklist

## Prerequisites
- [ ] Docker Desktop installed and running
- [ ] Docker Compose V2 available (`docker compose` command)
- [ ] Ports 5000, 5432, 8080 available on localhost

## Configuration Validation

### 1. Docker Compose File (`docker-compose.yml`)
- [x] Uses Docker Compose V2 format (no version field)
- [x] PostgreSQL service configured with pgvector extension
- [x] Backend service configured with .NET 8
- [x] Frontend service configured with SvelteKit + Nginx
- [x] Optional ML worker service with profile
- [x] Network configuration for service communication
- [x] Health checks for all services
- [x] Volume mounts for development

### 2. PostgreSQL Setup
- [x] PostgreSQL 16 Alpine image
- [x] Database: `mullagain`, User: `mullagain`, Password: `mullagain`
- [x] Port mapping: `5432:5432`
- [x] Volume for data persistence: `./data/postgres:/var/lib/postgresql/data`
- [x] Initialization scripts: `./backend/init:/docker-entrypoint-initdb.d`
- [x] Health check: `pg_isready -U mullagain -d mullagain`

### 3. Backend Setup
- [x] Multi-stage Dockerfile with .NET 8 SDK and runtime
- [x] Build context: `./backend`
- [x] Environment variables configured
- [x] Connection string: `Host=postgres;Database=mullagain;Username=mullagain;Password=mullagain`
- [x] Port mapping: `5000:5000`
- [x] Health check: `curl -f http://localhost:5000/health`
- [x] Depends on PostgreSQL health check

### 4. Frontend Setup
- [x] Multi-stage Dockerfile with Node.js 22 Alpine and Nginx Alpine
- [x] Build context: `./frontend`
- [x] SvelteKit build with static adapter
- [x] Nginx configuration for SPA routing
- [x] Port mapping: `8080:80`
- [x] Health check: `curl -f http://localhost:80`
- [x] Depends on backend service

### 5. PostgreSQL Extensions
- [x] `backend/init/01-extensions.sql` enables pgvector and intarray
- [x] `backend/init/02-custom-functions.sql` creates custom functions:
  - `count_intersection(arr1 integer[], arr2 integer[])`
  - `jaccard_similarity(arr1 integer[], arr2 integer[])`
  - `overlap_percentage(arr1 integer[], arr2 integer[])`

### 6. ML Worker Setup
- [x] Python 3.12 with sentence-transformers
- [x] FastAPI service with async database connections
- [x] Card embedding generation from name, oracle text, type, mana cost
- [x] Hand embedding generation from card names
- [x] Batch recomputation endpoint `/recompute/all`
- [x] Health check endpoint with model and database status
- [x] Structured logging with JSON output

## Validation Commands

### 1. Validate Docker Compose Configuration
```bash
docker compose config
```

### 2. Build All Containers
```bash
docker compose build
```

### 3. Start Services
```bash
docker compose up -d
```

### 4. Check Service Status
```bash
docker compose ps
```

### 5. View Service Logs
```bash
docker compose logs -f
```

### 6. Test PostgreSQL Extensions
```bash
docker compose exec postgres psql -U mullagain -d mullagain -c "SELECT extname FROM pg_extension WHERE extname IN ('vector', 'intarray');"
```

### 7. Test Backend Health
```bash
curl http://localhost:5000/health
```

### 8. Test Frontend
```bash
curl http://localhost:8080
```

### 9. Test API Endpoints
```bash
# Test equal hands endpoint
curl -X POST http://localhost:5000/api/hands/equal \
  -H "Content-Type: application/json" \
  -d '[1,2,3,4,5]'

# Test contains hands endpoint
curl -X POST http://localhost:5000/api/hands/contains \
  -H "Content-Type: application/json" \
  -d '{"cardIntIds": [1,2], "limit": 10}'
```

### 10. Test ML Worker
```bash
# Test ML worker health
curl http://localhost:7000/health

# Test card embedding
curl -X POST http://localhost:7000/embed/card \
  -H "Content-Type: application/json" \
  -d '{"card_id": "test", "name": "Lightning Bolt", "oracle_text": "Deal 3 damage", "type": "Instant"}'

# Test hand embedding
curl -X POST http://localhost:7000/embed/hand \
  -H "Content-Type: application/json" \
  -d '{"hand_id": 1, "card_ids": [1,2,3], "card_names": ["Lightning Bolt", "Counterspell", "Brainstorm"]}'

# Test recompute all embeddings
curl -X POST http://localhost:7000/recompute/all
```

### 11. Stop Services
```bash
docker compose down
```

## Expected Results

### PostgreSQL
- Extensions `vector` and `intarray` installed
- Custom functions `count_intersection`, `jaccard_similarity`, `overlap_percentage` available
- Database `mullagain` accessible with user `mullagain`

### Backend
- Health endpoint returns "Healthy"
- API endpoints respond (may return empty results without data)
- EF Core migrations can be applied
- Connection to PostgreSQL successful

### Frontend
- Main page loads at http://localhost:8080
- Compare page loads at http://localhost:8080/compare
- Analysis page loads at http://localhost:8080/analysis
- API calls to backend succeed

### ML Worker
- Health endpoint returns model and database status
- Card embedding generation works with 384-dimensional vectors
- Hand embedding generation works with card names
- Recompute all endpoint processes cards and hands
- Database connections successful

## Troubleshooting

### Common Issues
1. **Port conflicts**: Ensure ports 5000, 5432, 8080 are available
2. **Docker not running**: Start Docker Desktop
3. **Build failures**: Check Dockerfile syntax and dependencies
4. **Database connection**: Verify PostgreSQL is healthy before backend starts
5. **Frontend build**: Ensure Node.js dependencies are installed

### Debug Commands
```bash
# Check Docker version
docker --version
docker compose version

# Check running containers
docker ps

# Check service logs
docker compose logs postgres
docker compose logs backend
docker compose logs frontend

# Check network connectivity
docker compose exec backend ping postgres
docker compose exec frontend ping backend
```

## Performance Considerations
- PostgreSQL data persisted in `./data/postgres`
- Frontend assets cached by Nginx
- Backend runs in production mode
- Health checks prevent cascading failures

## Security Notes
- Database credentials in environment variables
- Frontend served over HTTP (HTTPS recommended for production)
- Backend CORS configured for frontend origins
- Nginx security headers enabled
