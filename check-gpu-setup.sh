#!/bin/bash
# Check if system is ready for GPU-enabled Docker builds

set -e

echo "=============================================="
echo " GPU Docker Setup Checker"
echo "=============================================="
echo ""

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

check_passed=0
check_failed=0
check_warning=0

# Function to print status
print_status() {
    if [ "$1" = "PASS" ]; then
        echo -e "${GREEN}✓${NC} $2"
        ((check_passed++))
    elif [ "$1" = "FAIL" ]; then
        echo -e "${RED}✗${NC} $2"
        ((check_failed++))
    elif [ "$1" = "WARN" ]; then
        echo -e "${YELLOW}⚠${NC} $2"
        ((check_warning++))
    else
        echo "  $2"
    fi
}

echo "Checking prerequisites..."
echo ""

# Check 1: NVIDIA GPU and drivers
echo "[1/6] Checking NVIDIA GPU and drivers..."
if command -v nvidia-smi &> /dev/null; then
    if nvidia-smi &> /dev/null; then
        GPU_NAME=$(nvidia-smi --query-gpu=name --format=csv,noheader | head -n1)
        DRIVER_VERSION=$(nvidia-smi --query-gpu=driver_version --format=csv,noheader | head -n1)
        print_status "PASS" "NVIDIA GPU detected: $GPU_NAME"
        print_status "PASS" "Driver version: $DRIVER_VERSION"
    else
        print_status "FAIL" "nvidia-smi command failed"
        echo "       → Make sure NVIDIA drivers are properly installed"
    fi
else
    print_status "FAIL" "nvidia-smi not found"
    echo "       → Install NVIDIA drivers: https://wiki.archlinux.org/title/NVIDIA"
fi
echo ""

# Check 2: Docker installed
echo "[2/6] Checking Docker..."
if command -v docker &> /dev/null; then
    DOCKER_VERSION=$(docker --version | awk '{print $3}' | sed 's/,//')
    print_status "PASS" "Docker installed: $DOCKER_VERSION"
    
    # Check if user can run docker without sudo
    if docker ps &> /dev/null; then
        print_status "PASS" "Docker accessible without sudo"
    else
        print_status "WARN" "Docker requires sudo (add user to docker group)"
        echo "       → Run: sudo usermod -aG docker \$USER && newgrp docker"
    fi
else
    print_status "FAIL" "Docker not installed"
    echo "       → Install Docker: https://docs.docker.com/engine/install/"
fi
echo ""

# Check 3: NVIDIA Container Toolkit
echo "[3/6] Checking NVIDIA Container Toolkit..."
if command -v nvidia-ctk &> /dev/null; then
    print_status "PASS" "nvidia-ctk found"
    
    # Check if runtime is configured
    if docker info 2>/dev/null | grep -q "nvidia"; then
        print_status "PASS" "NVIDIA runtime configured in Docker"
    else
        print_status "WARN" "NVIDIA runtime not configured"
        echo "       → Run: sudo nvidia-ctk runtime configure --runtime=docker"
        echo "       → Then: sudo systemctl restart docker"
    fi
else
    print_status "FAIL" "NVIDIA Container Toolkit not installed"
    echo "       → Arch/Manjaro: yay -S nvidia-container-toolkit"
    echo "       → Ubuntu/Debian: See DOCKER_GPU_SETUP.md"
fi
echo ""

# Check 4: Docker BuildKit
echo "[4/6] Checking Docker BuildKit..."
if docker info 2>/dev/null | grep -q "BuildKit"; then
    print_status "PASS" "BuildKit enabled globally"
else
    print_status "WARN" "BuildKit not enabled globally (will be enabled by build script)"
    echo "       → Optional: Add to /etc/docker/daemon.json:"
    echo "         {\"features\": {\"buildkit\": true}}"
fi
echo ""

# Check 5: Test GPU access in Docker
echo "[5/6] Testing GPU access in Docker container..."
if command -v docker &> /dev/null && command -v nvidia-smi &> /dev/null; then
    echo "  Pulling test image..."
    if docker pull nvidia/cuda:12.1.1-base-ubuntu22.04 &> /dev/null; then
        echo "  Running GPU test container..."
        if docker run --rm --gpus all nvidia/cuda:12.1.1-base-ubuntu22.04 nvidia-smi &> /tmp/gpu-test.log; then
            print_status "PASS" "GPU accessible in Docker container"
            GPU_IN_DOCKER=$(grep "NVIDIA" /tmp/gpu-test.log | head -n1 | awk '{print $3, $4, $5}')
            echo "       → GPU: $GPU_IN_DOCKER"
        else
            print_status "FAIL" "Cannot access GPU in Docker container"
            echo "       → Check: sudo systemctl restart docker"
            echo "       → Check: nvidia-ctk runtime configure --runtime=docker"
            cat /tmp/gpu-test.log
        fi
        rm -f /tmp/gpu-test.log
    else
        print_status "FAIL" "Cannot pull CUDA test image (check internet connection)"
    fi
else
    print_status "WARN" "Skipping (Docker or NVIDIA driver not available)"
fi
echo ""

# Check 6: Disk space
echo "[6/6] Checking disk space..."
AVAILABLE_GB=$(df -BG . | tail -1 | awk '{print $4}' | sed 's/G//')
if [ "$AVAILABLE_GB" -gt 10 ]; then
    print_status "PASS" "Sufficient disk space: ${AVAILABLE_GB}GB available"
else
    print_status "WARN" "Low disk space: ${AVAILABLE_GB}GB available (recommend 10GB+ for CUDA builds)"
fi
echo ""

# Summary
echo "=============================================="
echo " Summary"
echo "=============================================="
echo -e "${GREEN}Passed:${NC}  $check_passed"
echo -e "${YELLOW}Warnings:${NC} $check_warning"
echo -e "${RED}Failed:${NC}  $check_failed"
echo ""

if [ "$check_failed" -eq 0 ]; then
    echo -e "${GREEN}✓ System is ready for GPU-enabled Docker builds!${NC}"
    echo ""
    echo "Next steps:"
    echo "  1. Build ML Worker: ./build-ml-worker.sh"
    echo "  2. Start services: docker-compose up -d"
    echo "  3. Verify GPU: See QUICK_REFERENCE.md"
    exit 0
else
    echo -e "${RED}✗ System is NOT ready. Please fix the failed checks above.${NC}"
    echo ""
    echo "See DOCKER_GPU_SETUP.md for detailed setup instructions"
    exit 1
fi

