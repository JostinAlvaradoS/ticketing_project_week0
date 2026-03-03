#!/bin/bash

# Script para ejecutar migraciones de todos los servicios
# Uso: ./migrate-all.sh

# Colores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Variables de entorno por defecto
export DB_HOST="${DB_HOST:-localhost}"
export DB_PORT="${DB_PORT:-5432}"
export DB_NAME="${DB_NAME:-ticketing}"
export DB_USER="${DB_USER:-postgres}"
export DB_PASSWORD="${DB_PASSWORD:-postgres}"

# Función para crear migración inicial si no existe
create_initial_migration() {
    local service_name=$1
    local context_name=$2
    
    echo "   - Verificando migraciones existentes..."
    
    # Verificar si ya existen migraciones
    if [ -d "Migrations" ] && [ "$(ls -A Migrations 2>/dev/null)" ]; then
        echo "   - Migraciones existentes encontradas, omitiendo creación inicial"
        return 0
    fi
    
    # Crear migración inicial
    echo "   - Creando migración inicial..."
    if dotnet ef migrations add InitialCreate --context "$context_name" 2>/dev/null; then
        echo -e "${GREEN}   ✓ Migración inicial creada para ${service_name}${NC}"
        return 0
    else
        echo -e "${YELLOW}   ⚠️  No se pudo crear migración inicial para ${service_name} (puede que ya exista)${NC}"
        return 0  # Continue anyway as migration might already exist
    fi
}

# Función para ejecutar migraciones de un servicio
migrate_service() {
    local service_name=$1
    local schema_name=$2
    local context_name=$3
    local service_path=$4
    
    echo -e "${BLUE}📦 Migrando ${service_name}...${NC}"
    
    # Configurar esquema específico
    export DB_SCHEMA="${schema_name}"
    
    # Navegar al directorio del servicio
    if [ ! -d "$service_path" ]; then
        echo -e "${RED}❌ Error: Directorio no encontrado: $service_path${NC}"
        return 1
    fi
    
    echo "   - Directorio: $service_path"
    cd "$service_path"
    
    # Crear migración inicial si no existe (no falla si hay error)
    create_initial_migration "$service_name" "$context_name"
    
    # Aplicar migraciones a la base de datos
    echo "   - Aplicando migraciones para esquema: $schema_name"
    if dotnet ef database update --context "$context_name" 2>/dev/null; then
        echo -e "${GREEN}   ✓ ${service_name} migrado correctamente${NC}"
        cd - > /dev/null
        return 0
    else
        echo -e "${RED}   ❌ Error migrando ${service_name}${NC}"
        cd - > /dev/null
        return 1
    fi
}

echo -e "${BLUE}🚀 Iniciando migraciones de todos los servicios...${NC}"
echo -e "${YELLOW}📋 Configuración:${NC}"
echo "   - Host: $DB_HOST:$DB_PORT"
echo "   - Database: $DB_NAME"
echo "   - User: $DB_USER"
echo ""

# Directorio base del proyecto
BASE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Array de servicios: nombre, esquema, contexto, ruta
services=(
    "Identity|bc_identity|IdentityDbContext|services/identity/src/Identity.Infrastructure"
    "Catalog|bc_catalog|CatalogDbContext|services/catalog/src/Infrastructure"
    "Inventory|bc_inventory|InventoryDbContext|services/inventory/src/Inventory.Infrastructure"
    "Ordering|bc_ordering|OrderingDbContext|services/ordering/src/Infrastructure"
    "Payment|bc_payment|PaymentDbContext|services/payment/src/Infrastructure"
    "Fulfillment|bc_fulfillment|FulfillmentDbContext|services/fulfillment/src/Infrastructure"
    "Notification|bc_notification|NotificationDbContext|services/notification/src/Infrastructure"
)

# Contador de éxito/error
success_count=0
error_count=0
total_count=${#services[@]}

# Ejecutar migraciones para cada servicio
for service_info in "${services[@]}"; do
    IFS='|' read -r service_name schema_name context_name service_path <<< "$service_info"
    
    if migrate_service "$service_name" "$schema_name" "$context_name" "$BASE_DIR/$service_path"; then
        ((success_count++))
    else
        ((error_count++))
        echo -e "${RED}⚠️  Continuando con el siguiente servicio...${NC}"
    fi
    echo ""
done

# Resumen final
echo -e "${BLUE}📊 Resumen de migraciones:${NC}"
echo -e "   ${GREEN}✓ Exitosas: $success_count/$total_count${NC}"
if [ $error_count -gt 0 ]; then
    echo -e "   ${RED}❌ Fallidas: $error_count/$total_count${NC}"
fi

if [ $error_count -eq 0 ]; then
    echo -e "${GREEN}🎉 ¡Todas las migraciones completadas exitosamente!${NC}"
    exit 0
else
    echo -e "${YELLOW}⚠️  Algunas migraciones fallaron. Revisa los errores arriba.${NC}"
    exit 1
fi