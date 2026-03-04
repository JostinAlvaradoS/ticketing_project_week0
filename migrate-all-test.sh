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

# Función para ejecutar migraciones de un servicio
migrate_service() {
    local service_name=$1
    local schema_name=$2
    local context_name=$3
    local startup_project_path=$4
    local infrastructure_project_path=$5
    
    echo -e "${BLUE}📦 Migrando ${service_name}...${NC}"
    
    # Configurar esquema específico
    export DB_SCHEMA="${schema_name}"
    
    # Verificar que ambos directorios existen
    if [ ! -d "$startup_project_path" ]; then
        echo -e "${RED}❌ Error: Directorio de startup no encontrado: $startup_project_path${NC}"
        return 1
    fi
    
    if [ ! -d "$infrastructure_project_path" ]; then
        echo -e "${RED}❌ Error: Directorio de infrastructure no encontrado: $infrastructure_project_path${NC}"
        return 1
    fi
    
    echo "   - Startup project: $startup_project_path"
    echo "   - Infrastructure project: $infrastructure_project_path"
    
    # Restaurar paquetes NuGet
    echo "   - Restaurando paquetes NuGet..."
    cd "$startup_project_path/.."
    if dotnet restore > /dev/null 2>&1; then
        echo -e "${GREEN}   ✓ Paquetes restaurados correctamente${NC}"
    else
        echo -e "${YELLOW}   ⚠️  Advertencia: No se pudieron restaurar algunos paquetes${NC}"
    fi
    
    # Crear migración inicial si no existe (desde el directorio del startup project)
    cd "$startup_project_path"
    
    # Verificar si ya existen migraciones en el proyecto de infrastructure
    if [ -d "$infrastructure_project_path/Migrations" ] && [ "$(ls -A "$infrastructure_project_path/Migrations" 2>/dev/null)" ]; then
        echo "   - Migraciones existentes encontradas, omitiendo creación inicial"
    else
        echo "   - Creando migración inicial..."
        if dotnet ef migrations add InitialCreate --project "$infrastructure_project_path" --startup-project . --context "$context_name" 2>/dev/null; then
            echo -e "${GREEN}   ✓ Migración inicial creada para ${service_name}${NC}"
        else
            echo -e "${YELLOW}   ⚠️  No se pudo crear migración inicial para ${service_name} (puede que ya exista)${NC}"
        fi
    fi
    
    # Aplicar migraciones a la base de datos
    echo "   - Aplicando migraciones para esquema: $schema_name"
    if dotnet ef database update --project "$infrastructure_project_path" --startup-project . --context "$context_name" 2>/dev/null; then
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

# Array de servicios: nombre, esquema, contexto, startup_project_path, infrastructure_project_path
services=(
    "Identity|bc_identity|IdentityDbContext|services/identity/src/Identity.Api|services/identity/src/Identity.Infrastructure"
    "Catalog|bc_catalog|CatalogDbContext|services/catalog/src/Catalog.Api|services/catalog/src/Catalog.Infrastructure"
    "Inventory|bc_inventory|InventoryDbContext|services/inventory/src/Inventory.Api|services/inventory/src/Inventory.Infrastructure"
    "Ordering|bc_ordering|OrderingDbContext|services/ordering/src/Ordering.Api|services/ordering/src/Ordering.Infrastructure"
    "Payment|bc_payment|PaymentDbContext|services/payment/src/Payment.Api|services/payment/src/Payment.Infrastructure"
    "Fulfillment|bc_fulfillment|FulfillmentDbContext|services/fulfillment/src/Fulfillment.Api|services/fulfillment/src/Fulfillment.Infrastructure"
    "Notification|bc_notification|NotificationDbContext|services/notification/src/Notification.Api|services/notification/src/Notification.Infrastructure"
)

# Contador de éxito/error
success_count=0
error_count=0
total_count=${#services[@]}

# Ejecutar migraciones para cada servicio
for service_info in "${services[@]}"; do
    IFS='|' read -r service_name schema_name context_name startup_project_path infrastructure_project_path <<< "$service_info"
    
    if migrate_service "$service_name" "$schema_name" "$context_name" "$BASE_DIR/$startup_project_path" "$BASE_DIR/$infrastructure_project_path"; then
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