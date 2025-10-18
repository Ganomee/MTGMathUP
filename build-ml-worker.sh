#!/bin/bash
# Build ML Worker with CUDA GPU support

set -e
cd "$(dirname "$0")"

echo "=============================================="
echo " Building ML Worker (CUDA GPU Support)"
echo "=============================================="
echo ""
echo "This build includes CUDA 12.1 for GPU acceleration"
echo "The first build may take 30+ minutes due to large downloads"
echo "Subsequent builds will be much faster thanks to caching"
echo ""
echo "Requirements:"
echo "  - Docker with BuildKit enabled"
echo "  - NVIDIA Docker runtime (nvidia-container-toolkit)"
echo "  - NVIDIA GPU with compatible drivers"
echo ""
echo "Starting build with BuildKit caching..."
echo ""

# Enable BuildKit for better caching
export DOCKER_BUILDKIT=1
export COMPOSE_DOCKER_CLI_BUILD=1

# Build with no timeout and progress output
docker-compose build \
  --progress=plain \
  ml-worker

echo ""
echo "=============================================="
echo "✅ ML Worker built successfully!"
echo "=============================================="
echo ""
echo "GPU Configuration:"
echo "  - CUDA Version: 12.1"
echo "  - cuDNN: 8"
echo "  - GPU count: 1 (configurable in docker-compose.yml)"
echo ""
echo "To verify GPU access:"
echo "  docker-compose run --rm ml-worker python -c \"import torch; print(f'CUDA Available: {torch.cuda.is_available()}'); print(f'GPU: {torch.cuda.get_device_name(0) if torch.cuda.is_available() else \"N/A\"}')\""
echo ""
echo "Next steps:"
echo "  1. Start all services: docker-compose up -d"
echo "  2. Check status: docker-compose ps"
echo "  3. View ML worker logs: docker-compose logs -f ml-worker"
echo ""

