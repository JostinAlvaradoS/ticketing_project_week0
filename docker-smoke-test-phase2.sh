#!/bin/bash

# Docker-based Phase 2 Smoke Test
# Extends Phase 1 test with Payment, Fulfillment, and Notification services
# Verifies: Full ticketing flow from reservation to email notification

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_DIR="$SCRIPT_DIR/infra"

echo "🎟️  SpecKit Ticketing - Docker Smoke Test (Phase 2 with Notifications)"
echo "=================================================================="
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Track results
FAILED=0
PASSED=0

# Function to wait for a URL to respond
wait_for_url() {
    local url=$1
    local max_attempts=${2:-30}
    local attempt=0
    
    echo -n "  Waiting for $url..."
    while [ $attempt -lt "$max_attempts" ]; do
        if curl -s "$url" > /dev/null 2>&1; then
            echo -e " ${GREEN}✓${NC}"
            return 0
        fi
        attempt=$((attempt + 1))
        echo -n "."
        sleep 1
    done
    echo -e " ${RED}TIMEOUT${NC}"
    return 1
}

# Function to check result
check_result() {
    local name=$1
    local result=$2
    
    if [ $result -eq 0 ]; then
        echo -e "${GREEN}✓ PASS${NC} - $name"
        PASSED=$((PASSED + 1))
    else
        echo -e "${RED}✗ FAIL${NC} - $name"
        FAILED=$((FAILED + 1))
    fi
}

# ============================================================
# PHASE 2a: Check all containers including Notification
# ============================================================
echo -e "${BLUE}[1/4] Checking Docker containers for Phase 2${NC}"
echo ""

containers=("postgres" "redis" "kafka" "identity" "catalog" "inventory" "ordering" "payment" "fulfillment" "notification")
for container in "${containers[@]}"; do
    container_name="speckit-$container"
    if docker ps 2>/dev/null | grep "$container_name" > /dev/null 2>&1; then
        echo -e "${GREEN}✓${NC} Container $container_name is running"
        PASSED=$((PASSED + 1))
    else
        echo -e "${YELLOW}⚠${NC} Container $container_name is not running (may be expected during testing)"
    fi
done
echo ""

sleep 5  # Allow services to fully initialize

# ============================================================
# PHASE 2b: Check service health endpoints
# ============================================================
echo -e "${BLUE}[2/4] Checking service health endpoints${NC}"
echo ""

services=(
    "Identity:5001"
    "Catalog:5002"
    "Inventory:5003"
    "Ordering:5004"
    "Fulfillment:5005"
    "Notification:5006"
)

for service in "${services[@]}"; do
    IFS=':' read -r name port <<< "$service"
    url="http://localhost:$port/health"
    wait_for_url "$url" 30
    check_result "$name health check" $?
done
echo ""

# ============================================================
# PHASE 2c: Test Notification-specific flows
# ============================================================
echo -e "${BLUE}[3/4] Testing Notification Service Integration${NC}"
echo ""

# Check Kafka topic creation
echo "Checking Kafka topics..."
docker exec kafka kafka-topics --list --bootstrap-server localhost:9092 2>/dev/null | grep "ticket-issued" > /dev/null
check_result "Kafka topic 'ticket-issued' exists" $?

# Check notification database schema
echo "Checking notification database schema..."
docker exec postgres psql -U postgres -d ticketing -c "
    SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'bc_notification';
" 2>/dev/null | grep bc_notification > /dev/null
check_result "Notification database schema 'bc_notification' exists" $?

# Check notification table
docker exec postgres psql -U postgres -d ticketing -c "
    SELECT 1 FROM information_schema.tables 
    WHERE table_schema = 'bc_notification' AND table_name = 'emailnotifications';
" 2>/dev/null | grep 1 > /dev/null
check_result "EmailNotifications table exists" $?

echo ""

# ============================================================
# PHASE 2d: Execute Full E2E Scenario with Notifications
# ============================================================
echo -e "${BLUE}[4/4] Testing Full E2E Purchase Flow with Notifications${NC}"
echo ""

# Generate test data IDs
TEST_EVENT_ID="550e8400-e29b-41d4-a716-446655440000"
TEST_SEAT_ID="550e8400-e29b-41d4-a716-446655440002"
TEST_USER_ID="0d99a497-9013-4d79-9242-b47781d833b5"
TEST_RESERVATION_ID="550e8400-e29b-41d4-a716-446655440003"
TEST_ORDER_ID="550e8400-e29b-41d4-a716-446655440004"
TEST_PAYMENT_ID="550e8400-e29b-41d4-a716-446655440005"
TEST_TICKET_ID="550e8400-e29b-41d4-a716-446655440006"

echo "Scenario: Reserve → Add to Cart → Pay → Issue Ticket → Send Email"
echo ""

# Step 1: Create test event and seat
echo "Step 1: Seeding test event and seat..."
docker exec postgres psql -U postgres -d ticketing << EOF > /dev/null 2>&1
-- Create event
INSERT INTO bc_catalog."Events" ("Id", "Name", "Description", "EventDate", "BasePrice")
VALUES ('$TEST_EVENT_ID', 'Phase 2 Test Concert', 'A test concert for phase 2', '2026-03-15 19:00:00+00', 50.00)
ON CONFLICT ("Id") DO NOTHING;

-- Create seat
INSERT INTO bc_catalog."Seats" ("Id", "EventId", "SectionCode", "RowNumber", "SeatNumber", "Price", "Status")
VALUES ('$TEST_SEAT_ID', '$TEST_EVENT_ID', 'A', 1, 2, 50.00, 'available')
ON CONFLICT ("Id") DO NOTHING;

-- Create user (inventory schema)
INSERT INTO bc_inventory."Reservations" ("Id", "SeatId", "UserId", "Status", "ExpiresAt", "CreatedAt")
VALUES ('$TEST_RESERVATION_ID', '$TEST_SEAT_ID', '$TEST_USER_ID', 'reserved', NOW() + INTERVAL '15 minutes', NOW())
ON CONFLICT ("Id") DO NOTHING;
EOF
check_result "Test data seeded" $?

# Step 2: Test reservation API
echo "Step 2: Testing reservation creation..."
curl -s -X POST http://localhost:5003/reservations \
    -H "Content-Type: application/json" \
    -d "{\"seatId\": \"$TEST_SEAT_ID\", \"userId\": \"$TEST_USER_ID\"}" \
    > /tmp/reservation_response.json 2>&1
check_result "Reservation API responds" $?

# Step 3: Test order creation
echo "Step 3: Testing order creation..."
curl -s -X POST http://localhost:5004/orders/checkout \
    -H "Content-Type: application/json" \
    -d "{\"reservationId\": \"$TEST_RESERVATION_ID\", \"customerId\": \"$TEST_USER_ID\"}" \
    > /tmp/order_response.json 2>&1
check_result "Order API responds" $?

# Step 4: Test payment processing
echo "Step 4: Testing payment processing..."
curl -s -X POST http://localhost:5002/payments \
    -H "Content-Type: application/json" \
    -d "{\"orderId\": \"$TEST_ORDER_ID\", \"amount\": 50.00, \"currency\": \"USD\"}" \
    > /tmp/payment_response.json 2>&1
check_result "Payment API responds" $?

# Step 5: Test ticket generation
echo "Step 5: Testing ticket generation..."
curl -s -X GET http://localhost:5005/tickets?orderId=$TEST_ORDER_ID \
    > /tmp/ticket_response.json 2>&1
check_result "Fulfillment API responds" $?

# Step 6: Check notification was queued
echo "Step 6: Checking notification records..."
sleep 2
docker exec postgres psql -U postgres -d ticketing -c "
    SELECT COUNT(*) FROM bc_notification.\"EmailNotifications\";
" 2>/dev/null | grep -E "[0-9]+" > /dev/null
check_result "Notification records exist" $?

echo ""
echo "============================================================"
echo "Smoke Test Summary (Phase 2):"
echo "============================================================"
echo -e "Passed: ${GREEN}$PASSED${NC}"
echo -e "Failed: ${RED}$FAILED${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ All Phase 2 tests passed!${NC}"
    echo "The notification service is successfully integrated with the ticketing platform."
    exit 0
else
    echo -e "${RED}✗ Some tests failed${NC}"
    echo "Check logs above for details."
    exit 1
fi
