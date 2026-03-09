#!/bin/bash

# ==============================================================================
# SPECKIT TICKETING - E2E SYSTEM INTEGRATION TEST
# ==============================================================================
# Descripción: Valida el flujo crítico de Negocio (HU-P1) sobre Docker Compose.
# Rol: QA Senior Automation
# ==============================================================================

set -e

# Colores para el reporte
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color
BLUE='\033[0;34m'

echo -e "${BLUE}====================================================${NC}"
echo -e "${BLUE}INICIANDO TEST DE INTEGRACIÓN DE SISTEMA (E2E)${NC}"
echo -e "${BLUE}====================================================${NC}"

# 1. Puertos Definidos en docker-compose.yml
CATALOG_URL="http://localhost:50001"
INVENTORY_URL="http://localhost:50002"
ORDERING_URL="http://localhost:5003"
PAYMENT_URL="http://localhost:5004"
IDENTITY_URL="http://localhost:50000"
FULFILLMENT_URL="http://localhost:50004"
NOTIFICATION_URL="http://localhost:50005"

# ==============================================================================
# ESCENARIO 1: Verificación de Disponibilidad de Servicios (Health Check)
# ==============================================================================
echo -e "--- Verificando conectividad de Microservicios ---"

check_service() {
    local url=$1
    local name=$2
    echo -n "Checking $name ($url)... "
    if curl -s --head  --request GET "$url/health" | grep "200 OK" > /dev/null; then
        echo -e "${GREEN}UP${NC}"
    else
        # Reintento con Swagger / si no hay health endpoint
        if curl -s "$url/swagger/index.html" > /dev/null; then
             echo -e "${GREEN}UP (via Swagger)${NC}"
        else
            echo -e "${RED}DOWN${NC}"
            exit 1
        fi
    fi
}

check_service $CATALOG_URL "Catalog"
check_service $INVENTORY_URL "Inventory"
check_service $ORDERING_URL "Ordering"
check_service $PAYMENT_URL "Payment"

# ==============================================================================
# CARGA DE DATOS INICIALES (Seed Data)
# ==============================================================================
echo -e "\n--- Cargando Datos Iniciales (Seed Data) ---"
# Obtener el directorio donde se encuentra este script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
SEED_FILE="$SCRIPT_DIR/infra/db/seed-data.sql"

if [ -f "$SEED_FILE" ]; then
    echo "Ejecutando seed-data.sql en la base de datos PostgreSQL..."
    docker exec -i speckit-postgres psql -U postgres -d ticketing < "$SEED_FILE" > /dev/null
    echo -e "${GREEN}Datos cargados exitosamente.${NC}"
else
    echo -e "${RED}Error: No se encontró el archivo $SEED_FILE${NC}"
    exit 1
fi

# ==============================================================================
# ESCENARIO 2: Flujo Crítico de Compra (HU-P1)
# ==============================================================================

echo -e "\n--- Iniciando Flujo HU-P1: Compra de Boleto ---"

# Generar un GUID aleatorio para cada ejecución del test
if [[ "$OSTYPE" == "darwin"* ]]; then
    TEST_CUSTOMER_ID=$(uuidgen | tr '[:upper:]' '[:lower:]')
else
    TEST_CUSTOMER_ID=$(cat /proc/sys/kernel/random/uuid 2>/dev/null || echo "f47ac10b-58cc-4372-a567-0e02b2c3d479")
fi
echo "Usando CustomerId: $TEST_CUSTOMER_ID"

# Paso 2.1: Consultar Eventos y Asientos (Catalog)
echo -e "1. [Catalog] Consultando eventos..."
# El endpoint es /Events, no /api/v1/events según el controlador
EVENT_ID=$(curl -s "$CATALOG_URL/Events" | grep -o '"id":"[^"]*' | head -1 | cut -d'"' -f4)
if [ -z "$EVENT_ID" ]; then
    echo -e "${RED}Error: No se encontraron eventos en Catalog.${NC}"
    exit 1
fi
echo -e "   Event ID encontrado: $EVENT_ID"

# Paso 2.2: Reservar Asiento (Inventory)
echo -e "2. [Inventory] Realizando reserva temporal (TTL 15 min)..."
# Buscamos un asiento disponible para el evento usando /seatmap
SEATMAP_JSON=$(curl -s "$CATALOG_URL/Events/$EVENT_ID/seatmap")
# El patrón busca el ID que precede inmediatamente a un "status":"available"
SEAT_INFO=$(echo $SEATMAP_JSON | grep -o '{"id":"[0-9a-f-]*"[^}]*"price":[0-9.]*,"status":"available"' | head -1)
SEAT_ID=$(echo $SEAT_INFO | grep -o '"id":"[0-9a-f-]*"' | cut -d'"' -f4)
SEAT_PRICE=$(echo $SEAT_INFO | grep -o '"price":[0-9.]*' | cut -d':' -f2)

if [ -z "$SEAT_ID" ]; then
    echo -e "${RED}Error: No hay asientos disponibles para el evento $EVENT_ID.${NC}"
    exit 1
fi
echo -e "   Seat ID seleccionado: $SEAT_ID (Precio: $SEAT_PRICE)"

RESERVATION_RESPONSE=$(curl -s -X POST "$INVENTORY_URL/Reservations" \
    -H "Content-Type: application/json" \
    -d "{ \"eventId\": \"$EVENT_ID\", \"seatId\": \"$SEAT_ID\", \"customerId\": \"$TEST_CUSTOMER_ID\" }")

RESERVATION_ID=$(echo $RESERVATION_RESPONSE | grep -o '"reservationId":"[^"]*' | cut -d'"' -f4)
if [ -z "$RESERVATION_ID" ]; then
    echo -e "${RED}Error: No se pudo crear la reserva en Inventory.${NC}"
    echo "Response: $RESERVATION_RESPONSE"
    exit 1
fi
echo -e "   Reserva Exitosa. ID: $RESERVATION_ID"

# Paso 2.3: Agregar al Carrito (Ordering)
echo -e "3. [Ordering] Agregando reserva al carrito (Draft Order)..."
ORDER_RESPONSE=$(curl -s -X POST "$ORDERING_URL/Cart/add" \
    -H "Content-Type: application/json" \
    -d "{ \"reservationId\": \"$RESERVATION_ID\", \"seatId\": \"$SEAT_ID\", \"price\": $SEAT_PRICE, \"userId\": \"$TEST_CUSTOMER_ID\" }")

ORDER_ID=$(echo $ORDER_RESPONSE | grep -o '{"id":"[^"]*' | head -1 | cut -d'"' -f4)
if [ -z "$ORDER_ID" ]; then
    ORDER_ID=$(echo $ORDER_RESPONSE | grep -o '"orderId":"[^"]*' | head -1 | cut -d'"' -f4)
fi

if [ -z "$ORDER_ID" ]; then
    echo -e "${RED}Error: No se pudo crear la orden en Ordering.${NC}"
    echo "Response: $ORDER_RESPONSE"
    exit 1
fi
echo -e "   Orden Creada (Draft). ID: $ORDER_ID"

# Paso 2.4: Checkout de la Orden (Ordering)
echo -e "4. [Ordering] Realizando Checkout..."
# Forzar minúsculas en el userId para mayor compatibilidad
LOW_CUSTOMER_ID=$(echo "$TEST_CUSTOMER_ID" | tr '[:upper:]' '[:lower:]')
CHECKOUT_RESPONSE=$(curl -s -v -X POST "$ORDERING_URL/Orders/checkout" \
    -H "Content-Type: application/json" \
    -d "{ \"orderId\": \"$ORDER_ID\", \"userId\": \"$LOW_CUSTOMER_ID\" }" 2>&1)

echo "DEBUG: Checkout Response: $CHECKOUT_RESPONSE"

if [[ "$CHECKOUT_RESPONSE" == *"400 Bad Request"* ]] || [[ "$CHECKOUT_RESPONSE" == *"404 Not Found"* ]]; then
    echo -e "${RED}Error: Falló el checkout en Ordering (Status != 200).${NC}"
    exit 1
fi
echo -e "   Checkout Exitoso."

# Paso 2.5: Procesar Pago Simulado (Payment)
echo -e "5. [Payment] Procesando pago (Idempotencia check)..."
PAYMENT_RESPONSE=$(curl -s -X POST "$PAYMENT_URL/Payments" \
    -H "Content-Type: application/json" \
    -d "{ \"orderId\": \"$ORDER_ID\", \"customerId\": \"$LOW_CUSTOMER_ID\", \"reservationId\": \"$RESERVATION_ID\", \"amount\": $SEAT_PRICE, \"paymentMethod\": \"CreditCard\" }")

PAY_STATUS=$(echo $PAYMENT_RESPONSE | grep -o '"status":"[^"]*' | head -1 | cut -d'"' -f4)
# En el API de Payment, puede ser que el status esté dentro del objeto 'payment'
if [ -z "$PAY_STATUS" ]; then
    PAY_STATUS=$(echo $PAYMENT_RESPONSE | grep -o '"status":"[^"]*' | tail -n 1 | cut -d'"' -f4)
fi

echo -e "   Estado del Pago: $PAY_STATUS"
if [[ "$PAYMENT_RESPONSE" == *"Success\":true"* ]] || [[ "$PAY_STATUS" == "succeeded" ]] || [[ "$PAY_STATUS" == "Succeeded" ]]; then
     echo -e "${GREEN}FLUJO HU-P1 COMPLETADO EXITOSAMENTE${NC}"
else
    echo -e "${RED}Error en el pago.${NC}"
    echo "Response: $PAYMENT_RESPONSE"
    exit 1
fi

# ==============================================================================
# ESCENARIO 3: Validación de Integración vía Kafka (Coreografía)
# ==============================================================================
echo -e "\n5. [System] Validando coreografía de eventos..."
echo -e "   Esperando 5s para que el evento 'payment-succeeded' viaje por Kafka..."
sleep 5

# Verificamos si la orden cambió a estado 'Paid' en Ordering tras recibir el evento de Kafka
# Normalizamos a minúsculas. Consultamos directo a DB para evitar limitaciones de los DTOs de lectura.
FINAL_ORDER_STATUS=$(docker exec -i speckit-postgres psql -U postgres -d ticketing -t -c "SELECT \"State\" FROM bc_ordering.\"Orders\" WHERE \"Id\" = '$ORDER_ID';" | xargs | tr '[:upper:]' '[:lower:]')

if [ "$FINAL_ORDER_STATUS" == "paid" ] || [ "$FINAL_ORDER_STATUS" == "fulfilled" ]; then
    echo -e "   ${GREEN}Coreografía Kafka Exitosa: Orden está $FINAL_ORDER_STATUS en DB${NC}"
else
    echo -e "   ${RED}Error de Integración: El estado de la orden en DB es '$FINAL_ORDER_STATUS' (esperado paid)${NC}"
    exit 1
fi

echo -e "\n${GREEN}====================================================${NC}"
echo -e "${GREEN}TEST E2E COMPLETADO CON ÉXITO${NC}"
echo -e "${GREEN}====================================================${NC}"
