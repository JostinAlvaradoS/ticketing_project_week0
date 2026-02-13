#!/bin/bash

# Script para crear datos de prueba en el sistema de ticketing
# Uso: ./seed-data.sh

CRUD_URL="http://localhost:8002"

echo "🎫 Creando datos de prueba..."
echo ""

# Crear Evento 1
echo "📅 Creando evento: Concierto Rock 2025"
EVENT1=$(curl -s -X POST "$CRUD_URL/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Concierto Rock 2025",
    "startsAt": "2025-12-31T20:00:00Z"
  }')

EVENT1_ID=$(echo $EVENT1 | grep -o '"id":[0-9]*' | grep -o '[0-9]*')
echo "✅ Evento creado con ID: $EVENT1_ID"
echo ""

# Crear tickets para evento 1
echo "🎟️  Creando 10 tickets para Concierto Rock"
curl -s -X POST "$CRUD_URL/api/tickets/bulk" \
  -H "Content-Type: application/json" \
  -d "{
    \"eventId\": $EVENT1_ID,
    \"quantity\": 10
  }" > /dev/null

echo "✅ 10 tickets creados"
echo ""

# Crear Evento 2
echo "📅 Creando evento: Festival Electrónico"
EVENT2=$(curl -s -X POST "$CRUD_URL/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Festival Electrónico",
    "startsAt": "2026-01-15T22:00:00Z"
  }')

EVENT2_ID=$(echo $EVENT2 | grep -o '"id":[0-9]*' | grep -o '[0-9]*')
echo "✅ Evento creado con ID: $EVENT2_ID"
echo ""

# Crear tickets para evento 2
echo "🎟️  Creando 20 tickets para Festival Electrónico"
curl -s -X POST "$CRUD_URL/api/tickets/bulk" \
  -H "Content-Type: application/json" \
  -d "{
    \"eventId\": $EVENT2_ID,
    \"quantity\": 20
  }" > /dev/null

echo "✅ 20 tickets creados"
echo ""

# Crear Evento 3
echo "📅 Creando evento: Teatro Clásico"
EVENT3=$(curl -s -X POST "$CRUD_URL/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Teatro Clásico",
    "startsAt": "2026-02-20T19:00:00Z"
  }')

EVENT3_ID=$(echo $EVENT3 | grep -o '"id":[0-9]*' | grep -o '[0-9]*')
echo "✅ Evento creado con ID: $EVENT3_ID"
echo ""

# Crear tickets para evento 3
echo "🎟️  Creando 5 tickets para Teatro Clásico"
curl -s -X POST "$CRUD_URL/api/tickets/bulk" \
  -H "Content-Type: application/json" \
  -d "{
    \"eventId\": $EVENT3_ID,
    \"quantity\": 5
  }" > /dev/null

echo "✅ 5 tickets creados"
echo ""

# Verificar eventos creados
echo "📊 Verificando eventos creados:"
curl -s "$CRUD_URL/api/events" | grep -o '"name":"[^"]*"' | sed 's/"name":"//g' | sed 's/"//g' | while read name; do
  echo "  - $name"
done

echo ""
echo "✅ ¡Datos de prueba creados exitosamente!"
echo "🌐 Accede a http://localhost:3000/buy para ver los eventos"
