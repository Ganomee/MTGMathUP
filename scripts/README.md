# Scryfall Data Loader

This script loads all MTG card data from the Scryfall API and generates embeddings using the ML worker.

## Features

- **Complete Card Data**: Fetches all cards from Scryfall API
- **Database Integration**: Inserts cards into PostgreSQL with proper indexing
- **Embedding Generation**: Generates 384-dimensional embeddings for all cards
- **Batch Processing**: Efficient batch processing with progress logging
- **Error Handling**: Comprehensive error handling and logging

## Usage

### Using Docker Compose (Recommended)

```bash
# Run the data loader
./scripts/run_data_loader.sh

# Or on Windows
powershell -ExecutionPolicy Bypass -File scripts/run_data_loader.ps1
```

### Manual Execution

```bash
# Install dependencies
pip install -r scripts/requirements.txt

# Set environment variables
export DATABASE_URL="postgresql://mullagain:mullagain@localhost/mullagain"
export ML_WORKER_URL="http://localhost:7000"

# Run the loader
python scripts/load_scryfall_data.py
```

## Process Flow

1. **Fetch Cards**: Downloads all cards from Scryfall API
2. **Insert Cards**: Inserts cards into PostgreSQL database
3. **Create Indexes**: Creates card indexes for efficient lookups
4. **Generate Embeddings**: Calls ML worker to generate embeddings

## Data Structure

### Cards Table
- `id`: Scryfall card ID
- `name`: Card name
- `mana_cost`: Mana cost string
- `oracle_text`: Oracle text
- `type`: Type line
- `power_toughness`: Power/toughness (if applicable)
- `cmc`: Converted mana cost
- `colors`: Color array
- `created_at`: Creation timestamp
- `updated_at`: Update timestamp

### CardIndexes Table
- `id`: Sequential integer ID
- `card_id`: Reference to Cards table
- `created_at`: Creation timestamp

### CardEmbeddings Table
- `card_id`: Reference to Cards table
- `embedding`: 384-dimensional vector
- `model_version`: Model version used
- `created_at`: Creation timestamp
- `updated_at`: Update timestamp

## Configuration

### Environment Variables

- `DATABASE_URL`: PostgreSQL connection string
- `ML_WORKER_URL`: ML worker service URL
- `LOG_LEVEL`: Logging level (default: INFO)

### Scryfall API

- **Rate Limiting**: 50-100 requests per second
- **Pagination**: Automatic pagination handling
- **Error Handling**: Retry logic for failed requests

## Performance

- **Cards**: ~50,000+ cards from Scryfall
- **Processing Time**: 10-30 minutes depending on network
- **Memory Usage**: ~500MB during processing
- **Database Size**: ~100MB for cards + embeddings

## Testing

```bash
# Run test suite
python scripts/test_data_loader.py
```

Tests include:
- Scryfall API connectivity
- ML worker health
- Database connection and data verification

## Troubleshooting

### Common Issues

1. **Network Timeouts**
   - Check internet connection
   - Verify Scryfall API is accessible
   - Increase timeout values if needed

2. **Database Connection**
   - Verify PostgreSQL is running
   - Check connection string
   - Ensure database exists

3. **ML Worker Issues**
   - Verify ML worker is running
   - Check model loading
   - Monitor memory usage

### Debug Commands

```bash
# Check service status
docker compose ps

# View logs
docker compose logs data-loader
docker compose logs ml-worker
docker compose logs postgres

# Test database connection
docker compose exec postgres psql -U mullagain -d mullagain -c "SELECT COUNT(*) FROM \"Cards\";"

# Test ML worker
curl http://localhost:7000/health
```

## Monitoring

The script provides structured logging with:
- Progress updates
- Error tracking
- Performance metrics
- Batch processing status

## Integration

The data loader integrates with:
- **PostgreSQL**: Stores card data and embeddings
- **ML Worker**: Generates embeddings
- **Docker Compose**: Orchestrated deployment
- **Scryfall API**: Source of card data

## Future Enhancements

- [ ] Incremental updates
- [ ] Set-specific loading
- [ ] Image downloading
- [ ] Price data integration
- [ ] Performance optimization


