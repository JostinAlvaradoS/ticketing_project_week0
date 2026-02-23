#!/bin/bash

# Smoke Test Script for SpecKit Ticketing Infrastructure
# Verifies that all required services are running and healthy

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_DIR="$SCRIPT_DIR/infra"

echo "🔍 SpecKit Ticketing - Infrastructure Smoke Test"
echo "=================================================="
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Track failures
FAILED=0

# Function to check service health
check_service() {
    local name=$1
    local command=$2
    local description=$3

    echo -n "Checking $name... "
    
    if eval "$command" > /dev/null 2>&1; then
        echo -e "${GREEN}✓ PASS${NC}"
        return 0
    else
        echo -e "${RED}✗ FAIL${NC} - $description"
        FAILED=$((FAILED + 1))
        return 1
    fi
}

# 1. Check PostgreSQL
check_service "PostgreSQL" \
    "docker exec speckit-postgres pg_isready -U postgres -d ticketing" \
    "PostgreSQL is not responding. Run: docker-compose up -d postgres"

# 2. Check Redis
check_service "Redis" \
    "docker exec speckit-redis redis-cli ping | grep -q PONG" \
    "Redis is not responding. Run: docker-compose up -d redis"

# 3. Check Kafka
check_service "Kafka" \
    "docker exec speckit-kafka kafka-broker-api-versions --bootstrap-server localhost:9092" \
    "Kafka broker is not responding. Run: docker-compose up -d kafka"

# 4. Check Identity Service Health (requires it to be running)
echo -n "Checking Identity Service health endpoint... "
if curl -s http://localhost:5000/health > /dev/null 2>&1; then
    echo -e "${GREEN}✓ PASS${NC}"
else
    echo -e "${YELLOW}⚠ SKIP${NC} - Identity Service not running. Start with: dotnet run"
    echo "  (This is OK if you haven't started the Identity service yet)"
fi

echo ""
echo "=================================================="

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ All infrastructure checks passed!${NC}"
    echo ""
    echo "Next steps:"
    echo "1. Start Identity Service: cd services/identity && dotnet run"
    echo "2. The seed user will be created automatically:"
    echo "   Email: test@example.com"
    echo "   Password: Password123!"
    echo ""
    exit 0
else
    echo -e "${RED}✗ $FAILED check(s) failed${NC}"
    echo ""
    echo "To start all services:"
    echo "  cd infra && docker-compose up -d"
    echo ""
    exit 1
fi
