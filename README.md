# MTG Mullagain

Compare Magic: The Gathering hands, analyze mulligans, and learn which keeps are best.

## Quick Start

```bash
# Build ML Worker with GPU support (first time - takes 30+ mins)
./build-ml-worker.sh

# Start all services
docker compose up

# Start without ML worker (recommended for development)
docker compose up --scale ml-worker=0
```

**GPU Support**: The ML Worker now supports NVIDIA GPU acceleration with CUDA 12.1. See [DOCKER_GPU_SETUP.md](./DOCKER_GPU_SETUP.md) for detailed setup instructions.

## Services

- **Backend** → http://localhost:5000
- **Frontend** → http://localhost:8080  
- **Database** → localhost:5432 (mullagain/mullagain)
- **ML Worker** → http://localhost:7000 (optional)

## Development

### Backend (.NET 8)
```bash
cd backend
dotnet restore
dotnet run --project src/MtgMullagain.Api
```

### Frontend (SvelteKit)
```bash
cd frontend
npm install
npm run dev
```

### ML Worker (Python with CUDA GPU Support)
```bash
cd ml-worker
pip install -r requirements.txt
python main.py
```

**Note**: The ML Worker uses PyTorch with CUDA 12.1 for GPU acceleration. For GPU setup in Docker, see [DOCKER_GPU_SETUP.md](./DOCKER_GPU_SETUP.md).

## Database Setup

To import card data:
```bash
curl -X POST http://localhost:5000/api/import/scryfall
```

## Project Structure

```
mtg-mullagain/
├── backend/                 # .NET 8 API
│   ├── src/
│   │   ├── MtgMullagain.Api/        # ASP.NET Core Minimal API
│   │   ├── MtgMullagain.Core/       # Entities / DTOs / Services
│   │   └── MtgMullagain.Infrastructure/ # EF Core Context
│   ├── tests/
│   │   └── MtgMullagain.Tests/
│   └── Dockerfile
├── frontend/                # SvelteKit + TypeScript + Tailwind
│   ├── src/
│   │   ├── routes/          # Pages (/import, /compare, /analysis)
│   │   └── lib/components/ # Reusable components
│   └── Dockerfile
├── ml-worker/               # Python FastAPI (optional)
│   ├── main.py
│   ├── requirements.txt
│   └── Dockerfile
├── docker-compose.yml
└── README.md
```

## Features

- **Deck Import**: Upload from CubeCobra, Moxfield, or manual entry
- **Hand Comparison**: Draw two hands and choose which to keep
- **Statistical Analysis**: See aggregated results and insights
- **Offline Support**: PWA with IndexedDB caching
- **Vector Similarity**: Find similar hands using pgvector embeddings

## Tech Stack

- **Backend**: .NET 8, EF Core, PostgreSQL + pgvector
- **Frontend**: SvelteKit, TypeScript, TailwindCSS, PWA
- **ML Worker**: Python, FastAPI, PyTorch (CUDA 12.1), sentence-transformers
- **Database**: PostgreSQL 16 with pgvector extension
- **Deployment**: Docker Compose with GPU support

## License

MIT License
