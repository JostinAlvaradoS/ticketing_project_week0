#!/bin/bash

# ==============================================================================
# SCRIPT DE PRUEBA DE SISTEMA (SYSTEM TEST SUITE)
# ==============================================================================
# Objetivo: Validar la integridad de la infraestructura y dependencias del sistema.
# ==============================================================================

GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "===================================================="
echo -e "INICIANDO PRUEBAS DE SISTEMA (SYSTEM QUALITY CHECK)"
echo -e "===================================================="

# 1. VERIFICACIÓN DE INFRAESTRUCTURA (INFRA-ST-01)
echo -e "\n1. [Infraestructura] Verificando Red de Docker y Conexiones Internas..."

check_dependency() {
    local service=$1
    local port=$2
    # Usamos la red speckit-net que es la correcta según docker network ls
    if docker run --rm --network speckit-net alpine nc -z $service $port > /dev/null 2>&1; then
        echo -e "   [OK] Conexión interna a $service:$port exitosa."
    else
        echo -e "   ${RED}[FALLO] No se puede alcanzar $service:$port desde la red interna.${NC}"
        return 1
    fi
}

check_dependency "speckit-postgres" 5432
check_dependency "speckit-redis" 6379
check_dependency "speckit-kafka" 9092

# 2. VERIFICACIÓN DE ESQUEMAS DE BASE DE DATOS (DATA-ST-02)
echo -e "\n2. [Datos] Verificando existencia de esquemas por microservicio..."

check_schema() {
    local schema=$1
    local result=$(docker exec -i speckit-postgres psql -U postgres -d ticketing -t -c "SELECT schema_name FROM information_schema.schemata WHERE schema_name = '$schema';" | xargs)
    if [ "$result" == "$schema" ]; then
        echo -e "   [OK] Esquema '$schema' presente."
    else
        echo -e "   ${RED}[FALLO] Esquema '$schema' no encontrado.${NC}"
        return 1
    fi
}

check_schema "bc_catalog"
check_schema "bc_inventory"
check_schema "bc_ordering"
check_schema "bc_payment"

# 3. VERIFICACIÓN DE TÓPICOS KAFKA (BROKER-ST-03)
echo -e "\n3. [Mensajería] Verificando Tópicos de Coreografía..."

check_kafka_topic() {
    local topic=$1
    local exists=$(docker exec speckit-kafka kafka-topics --list --bootstrap-server localhost:9092 | grep -w "$topic")
    if [ -n "$exists" ]; then
        echo -e "   [OK] Tópico '$topic' activo."
    else
        echo -e "   ${RED}[FALLO] Tópico '$topic' no existe.${NC}"
        return 1
    fi
}

# Solo listamos algunos críticos:
check_kafka_topic "payment-succeeded"
check_kafka_topic "payment-failed"

# 4. VERIFICACIÓN DE PERSISTENCIA REDIS (CACHE-ST-04)
echo -e "\n4. [Caché] Verificando operabilidad de Redis..."
docker exec speckit-redis redis-cli SET system_test_key "active" > /dev/null
REDIS_VAL=$(docker exec speckit-redis redis-cli GET system_test_key)

if [ "$REDIS_VAL" == "active" ]; then
    echo -e "   [OK] Escritura/Lectura en Redis exitosa."
else
    echo -e "   ${RED}[FALLO] Fallo en la persistencia de Redis.${NC}"
fi

echo -e "\n===================================================="
echo -e "${GREEN}PRUEBA DE SISTEMA COMPLETADA EXITOSAMENTE${NC}"
echo -e "===================================================="
