# MTG Mullagain ML Worker

This service provides machine learning capabilities for the MTG Mullagain application, including card and hand embedding generation using sentence-transformers.

## Features

- **Card Embeddings**: Generate 384-dimensional embeddings for MTG cards using their name, oracle text, type, and mana cost
- **Hand Embeddings**: Generate embeddings for hands based on the cards they contain
- **Batch Processing**: Recompute all embeddings in the database
- **Vector Storage**: Store embeddings in PostgreSQL with pgvector extension

## API Endpoints

### Health Check
```
GET /health
```
Returns the health status of the service, including model and database status.

### Card Embedding
```
POST /embed/card
```
Generate embedding for a single card.

**Request Body:**
```json
{
  "card_id": "string",
  "name": "string",
  "oracle_text": "string",
  "type": "string",
  "mana_cost": "string (optional)"
}
```

**Response:**
```json
{
  "embedding": [0.1, 0.2, ...],
  "model_version": "all-MiniLM-L6-v2"
}
```

### Hand Embedding
```
POST /embed/hand
```
Generate embedding for a hand based on card names.

**Request Body:**
```json
{
  "hand_id": 1,
  "card_ids": [1, 2, 3],
  "card_names": ["Lightning Bolt", "Counterspell", "Brainstorm"]
}
```

**Response:**
```json
{
  "embedding": [0.1, 0.2, ...],
  "model_version": "all-MiniLM-L6-v2"
}
```

### Recompute All Embeddings
```
POST /recompute/all
```
Recompute embeddings for all cards and hands in the database.

**Response:**
```json
{
  "status": "completed",
  "cards_processed": 1000,
  "hands_processed": 500,
  "errors": [],
  "duration_seconds": 45.2
}
```

## Model Details

- **Model**: `all-MiniLM-L6-v2` from sentence-transformers
- **Dimensions**: 384
- **Language**: English
- **Use Case**: General-purpose text embeddings

## Text Representation

### Cards
Cards are represented as:
```
{name} - {oracle_text} - [{type}] - Cost: {mana_cost}
```

Example:
```
Lightning Bolt - Lightning Bolt deals 3 damage to any target. - [Instant] - Cost: {R}
```

### Hands
Hands are represented as:
```
Hand containing: {card_name_1}, {card_name_2}, ...
```

Example:
```
Hand containing: Lightning Bolt, Counterspell, Brainstorm, Ponder, Island
```

## Database Schema

The service reads from and writes to the following tables:

### Input Tables
- `Cards`: Card information (name, oracle_text, type, mana_cost)
- `Hands`: Hand information with card_int_ids array
- `CardIndexes`: Mapping between card_int_ids and card IDs

### Output Tables
- `CardEmbeddings`: Stores card embeddings with model version
- `HandEmbeddings`: Stores hand embeddings

## Environment Variables

- `DATABASE_URL`: PostgreSQL connection string (default: `postgresql://mullagain:mullagain@postgres/mullagain`)
- `MODEL_NAME`: Sentence transformer model name (default: `all-MiniLM-L6-v2`)
- `EMBEDDING_DIMENSION`: Expected embedding dimension (default: 384)
- `API_HOST`: API host (default: `0.0.0.0`)
- `API_PORT`: API port (default: `7000`)
- `LOG_LEVEL`: Logging level (default: `INFO`)

## Development

### Prerequisites
- Python 3.12+
- PostgreSQL with pgvector extension
- Docker (optional)

### Local Development
```bash
# Install dependencies
pip install -r requirements.txt

# Set environment variables
export DATABASE_URL="postgresql://mullagain:mullagain@localhost/mullagain"

# Run the service
uvicorn main:app --host 0.0.0.0 --port 7000 --reload
```

### Docker Development
```bash
# Build the image
docker build -t mtg-mullagain-ml-worker .

# Run the container
docker run -p 7000:7000 \
  -e DATABASE_URL="postgresql://mullagain:mullagain@host.docker.internal/mullagain" \
  mtg-mullagain-ml-worker
```

### Testing
```bash
# Run the test suite
python test_worker.py
```

## Performance Considerations

- **Model Loading**: The sentence transformer model is loaded once at startup
- **Batch Processing**: The `/recompute/all` endpoint processes cards and hands in batches
- **Database Connections**: Uses connection pooling for efficient database access
- **Memory Usage**: The model requires ~400MB of RAM

## Monitoring

The service provides structured logging with the following information:
- Request/response logging
- Error tracking
- Performance metrics
- Database connection status

## Troubleshooting

### Common Issues

1. **Model Loading Failures**
   - Ensure sufficient memory (4GB+ recommended)
   - Check internet connection for model download

2. **Database Connection Issues**
   - Verify DATABASE_URL is correct
   - Ensure PostgreSQL is running and accessible
   - Check pgvector extension is installed

3. **Embedding Generation Errors**
   - Verify input data format
   - Check for empty or null values
   - Monitor memory usage during batch processing

### Debug Commands
```bash
# Check service health
curl http://localhost:7000/health

# Test card embedding
curl -X POST http://localhost:7000/embed/card \
  -H "Content-Type: application/json" \
  -d '{"card_id": "test", "name": "Lightning Bolt", "oracle_text": "Deal 3 damage", "type": "Instant"}'

# Check logs
docker logs <container_id>
```

## Integration

The ML worker integrates with the main MTG Mullagain application through:

1. **Backend API**: The backend calls the ML worker to generate embeddings
2. **Database**: Shared PostgreSQL database with pgvector extension
3. **Docker Compose**: Orchestrated together in the main docker-compose.yml

## Future Enhancements

- [ ] Support for multiple embedding models
- [ ] Caching of embeddings
- [ ] Incremental updates
- [ ] Similarity search endpoints
- [ ] Model versioning
- [ ] Performance optimization


