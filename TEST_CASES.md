# 🧪 Test Cases - Sistema de Ticketing

Documento completo de casos de prueba para el sistema distribuido de gestión de tickets y eventos.

---

## 📋 Índice

1. [Casos de Prueba de API](#casos-de-prueba-de-api)
2. [Casos de Prueba de Flujos de Negocio](#casos-de-prueba-de-flujos-de-negocio)
3. [Casos de Prueba de Concurrencia](#casos-de-prueba-de-concurrencia)
4. [Casos de Prueba de Validación](#casos-de-prueba-de-validación)
5. [Casos de Prueba de Resiliencia](#casos-de-prueba-de-resiliencia)
6. [Casos de Prueba de Performance](#casos-de-prueba-de-performance)
7. [Casos de Prueba End-to-End](#casos-de-prueba-end-to-end)
8. [Scripts de Prueba](#scripts-de-prueba)

---

## 1. Casos de Prueba de API

### 1.1 Events API (CRUD Service)

#### TC-API-001: Crear Evento Válido
```yaml
Endpoint: POST /api/events
Precondiciones: Servicio CRUD corriendo
Datos de entrada:
  {
    "name": "Concierto de Rock 2026",
    "startsAt": "2026-03-15T20:00:00Z"
  }
Resultado esperado:
  - Status: 201 Created
  - Response contiene: id, name, startsAt, availableTickets=0, reservedTickets=0, paidTickets=0
  - Evento se persiste en PostgreSQL
Criterios de aceptación:
  ✓ ID generado automáticamente
  ✓ Timestamps en formato ISO 8601
  ✓ Contadores de tickets inicializados en 0
```

#### TC-API-002: Crear Evento Sin Nombre
```yaml
Endpoint: POST /api/events
Datos de entrada:
  {
    "name": "",
    "startsAt": "2026-03-15T20:00:00Z"
  }
Resultado esperado:
  - Status: 400 Bad Request
  - Mensaje: "El nombre del evento es requerido"
Criterios de aceptación:
  ✓ No se crea registro en BD
  ✓ Respuesta en < 100ms
```

#### TC-API-003: Crear Evento Sin Fecha
```yaml
Endpoint: POST /api/events
Datos de entrada:
  {
    "name": "Evento Test",
    "startsAt": null
  }
Resultado esperado:
  - Status: 400 Bad Request
  - Mensaje: "La fecha de inicio es requerida"
```

#### TC-API-004: Listar Todos los Eventos
```yaml
Endpoint: GET /api/events
Precondiciones: 
  - 3 eventos creados en BD
Resultado esperado:
  - Status: 200 OK
  - Array con 3 elementos
  - Cada elemento tiene estructura de EventDto
  - Contadores de tickets calculados correctamente
```

#### TC-API-005: Obtener Evento por ID
```yaml
Endpoint: GET /api/events/{id}
Precondiciones: Evento con id=1 existe
Datos de entrada: id=1
Resultado esperado:
  - Status: 200 OK
  - Response contiene evento completo con contadores
```

#### TC-API-006: Obtener Evento Inexistente
```yaml
Endpoint: GET /api/events/999999
Resultado esperado:
  - Status: 404 Not Found
  - Mensaje: "Evento 999999 no encontrado"
```

#### TC-API-007: Actualizar Evento
```yaml
Endpoint: PUT /api/events/1
Datos de entrada:
  {
    "name": "Concierto ACTUALIZADO",
    "startsAt": "2026-04-20T21:00:00Z"
  }
Resultado esperado:
  - Status: 200 OK
  - Datos actualizados en BD
  - ID no cambia
```

#### TC-API-008: Eliminar Evento Con Tickets
```yaml
Endpoint: DELETE /api/events/1
Precondiciones: Evento tiene 10 tickets asociados
Resultado esperado:
  - Status: 204 No Content
  - Evento eliminado
  - Tickets eliminados en cascada (ON DELETE CASCADE)
```

---

### 1.2 Tickets API (CRUD Service)

#### TC-API-009: Crear Tickets en Lote
```yaml
Endpoint: POST /api/tickets/bulk
Datos de entrada:
  {
    "eventId": 1,
    "quantity": 50
  }
Resultado esperado:
  - Status: 201 Created
  - 50 tickets creados
  - Todos con status="available"
  - Version=0 para todos
  - IDs consecutivos
Tiempo: < 2 segundos
```

#### TC-API-010: Crear Tickets con Cantidad Inválida
```yaml
Endpoint: POST /api/tickets/bulk
Casos de prueba:
  1. quantity = 0
  2. quantity = -5
  3. quantity = 1001 (límite excedido)
Resultado esperado para todos:
  - Status: 400 Bad Request
  - Mensaje: "Quantity debe estar entre 1 y 1000"
```

#### TC-API-011: Crear Tickets para Evento Inexistente
```yaml
Endpoint: POST /api/tickets/bulk
Datos de entrada:
  {
    "eventId": 999999,
    "quantity": 10
  }
Resultado esperado:
  - Status: 400 Bad Request
  - Mensaje: "Evento no encontrado"
```

#### TC-API-012: Listar Tickets por Evento
```yaml
Endpoint: GET /api/tickets/event/1
Precondiciones: Evento 1 tiene 50 tickets (20 available, 15 reserved, 10 paid, 5 released)
Resultado esperado:
  - Status: 200 OK
  - Array con 50 elementos
  - Distribución de status correcta
  - Campos reserved_at, expires_at presentes para tickets reserved
```

#### TC-API-013: Obtener Ticket Individual
```yaml
Endpoint: GET /api/tickets/1
Resultado esperado:
  - Status: 200 OK
  - Estructura TicketDto completa
  - Version field presente
```

#### TC-API-014: Actualizar Status de Ticket
```yaml
Endpoint: PUT /api/tickets/1/status
Datos de entrada:
  {
    "newStatus": "cancelled",
    "reason": "Cancelación por cliente"
  }
Precondiciones: Ticket 1 tiene status="reserved"
Resultado esperado:
  - Status: 200 OK
  - Status actualizado a "cancelled"
  - Version incrementado
  - Registro en ticket_history creado
```

#### TC-API-015: Liberar Ticket
```yaml
Endpoint: DELETE /api/tickets/1/release?reason=Expirado
Precondiciones: Ticket 1 tiene status="reserved"
Resultado esperado:
  - Status: 200 OK
  - Status cambiado a "available"
  - reserved_at, expires_at, order_id, reserved_by = NULL
  - Version incrementado
  - Historial registrado
```

#### TC-API-016: Obtener Tickets Expirados
```yaml
Endpoint: GET /api/tickets/expired/list
Precondiciones: 
  - 3 tickets con expires_at < NOW()
  - 5 tickets con expires_at > NOW()
Resultado esperado:
  - Status: 200 OK
  - Array con 3 tickets expirados
  - Ordenados por expires_at ASC
```

---

### 1.3 Producer API

#### TC-API-017: Reservar Ticket (Happy Path)
```yaml
Endpoint: POST /api/tickets/reserve
Datos de entrada:
  {
    "eventId": 1,
    "ticketId": 1,
    "orderId": "ORD-12345",
    "reservedBy": "user@example.com",
    "expiresInSeconds": 300
  }
Resultado esperado:
  - Status: 202 Accepted
  - Response: {"message": "Reserva procesada", "ticketId": 1}
  - Mensaje publicado a RabbitMQ queue "q.ticket.reserved"
  - Tiempo de respuesta: < 500ms
```

#### TC-API-018: Reservar Ticket con Datos Inválidos
```yaml
Casos de prueba:
  1. ticketId = 0
  2. eventId = -1
  3. orderId = ""
  4. reservedBy = null
  5. expiresInSeconds = 0

Resultado esperado para cada caso:
  - Status: 400 Bad Request
  - Mensaje específico del campo inválido
  - NO se publica mensaje a RabbitMQ
```

#### TC-API-019: Procesar Pago Válido
```yaml
Endpoint: POST /api/payments/process
Datos de entrada:
  {
    "ticketId": 1,
    "eventId": 1,
    "amountCents": 5000,
    "currency": "USD",
    "paymentBy": "buyer@example.com",
    "paymentMethodId": "card_1234",
    "transactionRef": "txn_ext_abc123"
  }
Resultado esperado:
  - Status: 202 Accepted
  - Response incluye: ticketId, eventId, status (approved/rejected)
  - Mensaje publicado a RabbitMQ
  - 80% probabilidad de "approved", 20% de "rejected"
```

#### TC-API-020: Procesar Pago con Monto Inválido
```yaml
Datos de entrada: amountCents = 0 o negativo
Resultado esperado:
  - Status: 400 Bad Request
  - Mensaje: "AmountCents debe ser mayor a 0"
```

---

## 2. Casos de Prueba de Flujos de Negocio

### 2.1 Flujo Completo de Compra (Happy Path)

#### TC-FLOW-001: Compra Exitosa Completa
```yaml
Descripción: Usuario compra un ticket desde cero hasta pago confirmado
Pasos:
  1. Admin crea evento "Concierto Rock"
  2. Admin crea 100 tickets para el evento
  3. Usuario lista eventos disponibles
  4. Usuario selecciona evento y ve tickets available
  5. Usuario reserva un ticket
  6. Sistema procesa reserva (async)
  7. Frontend hace polling hasta status="reserved"
  8. Usuario ingresa datos de pago
  9. Sistema procesa pago (async)
  10. Frontend hace polling hasta status="paid"
  
Resultado esperado:
  ✓ Evento creado con ID válido
  ✓ 100 tickets con status="available"
  ✓ Ticket cambia a "reserved" en < 5 segundos
  ✓ reserved_at y expires_at seteados
  ✓ Ticket cambia a "paid" en < 10 segundos
  ✓ paid_at seteado
  ✓ Registro en payments creado con status="approved"
  ✓ Dos entradas en ticket_history (available→reserved, reserved→paid)
  ✓ Contadores del evento actualizados

Tiempo total: < 20 segundos
```

---

### 2.2 Flujo de Pago Rechazado

#### TC-FLOW-002: Pago Rechazado Libera Ticket
```yaml
Pasos:
  1. Usuario reserva ticket exitosamente
  2. Ticket status="reserved"
  3. Usuario procesa pago
  4. Sistema rechaza pago (20% probabilidad)
  5. Payment Service consume evento "ticket.payments.rejected"
  6. Sistema libera ticket

Resultado esperado:
  ✓ Status cambia de "reserved" a "released"
  ✓ reserved_at, expires_at, order_id = NULL
  ✓ Payment record con status="failed"
  ✓ Registro en ticket_history con reason="Payment rejected"
  ✓ Ticket disponible para otro comprador
```

---

### 2.3 Flujo de Expiración de Reserva

#### TC-FLOW-003: Ticket Expira Por TTL
```yaml
Precondiciones: 
  - Ticket reservado hace 4 minutos 50 segundos
  - expires_at = NOW() + 10 segundos
  
Pasos:
  1. Esperar 15 segundos
  2. Usuario intenta pagar el ticket expirado
  3. Payment Service valida TTL
  
Resultado esperado:
  ✓ Pago rechazado por TTL excedido
  ✓ Ticket liberado automáticamente
  ✓ Status="released"
  ✓ Historial: reason="Payment received after TTL"
```

---

### 2.4 Flujo de Reserva Concurrente

#### TC-FLOW-004: Dos Usuarios Reservan El Mismo Ticket
```yaml
Precondiciones: 1 ticket disponible (ID=1)

Pasos (simultáneos):
  1. Usuario A solicita reserva del ticket 1
  2. Usuario B solicita reserva del ticket 1 (delay 50ms)
  
Resultado esperado:
  ✓ Solo UNO de los usuarios logra la reserva
  ✓ El primero en llegar al ReservationService gana (optimistic locking)
  ✓ El segundo recibe mensaje de log: "Ticket was modified by another process"
  ✓ Version del ticket incrementado solo una vez
  ✓ No hay datos inconsistentes en BD
```

---

## 3. Casos de Prueba de Concurrencia

### 3.1 Optimistic Locking

#### TC-CONC-001: Actualización Concurrente de Ticket
```yaml
Setup:
  - Ticket 1 con version=0, status="available"
  
Pasos simulados:
  1. Proceso A lee ticket (version=0)
  2. Proceso B lee ticket (version=0)
  3. Proceso A actualiza: WHERE id=1 AND version=0
     SET status='reserved', version=1
  4. Proceso B intenta actualizar: WHERE id=1 AND version=0
     SET status='reserved', version=1
     
Resultado esperado:
  ✓ Actualización de A: exitosa (1 row affected)
  ✓ Actualización de B: fallida (0 rows affected)
  ✓ Ticket final: version=1, status='reserved' por proceso A
  ✓ Proceso B lanza excepción o retorna false
```

---

### 3.2 Race Conditions

#### TC-CONC-002: 100 Usuarios Compiten Por 10 Tickets
```yaml
Setup:
  - Evento con exactamente 10 tickets disponibles
  - 100 usuarios intentan reservar simultáneamente
  
Ejecución:
  - Lanzar 100 peticiones POST /api/tickets/reserve en paralelo
  
Resultado esperado:
  ✓ Exactamente 10 reservas exitosas
  ✓ 90 reservas fallidas
  ✓ No hay over-booking
  ✓ No hay deadlocks
  ✓ Todos los tickets tienen version > 0
  ✓ Base de datos consistente
  
Tiempo límite: < 30 segundos
```

---

### 3.3 Doble Procesamiento

#### TC-CONC-003: Idempotencia en Payment Service
```yaml
Setup:
  - Ticket 1 con status="reserved"
  - Mensaje "PaymentApprovedEvent" para ticket 1
  
Pasos:
  1. Mensaje procesado por Payment Service (ticket → paid)
  2. Mismo mensaje re-encolado (por error simulado)
  3. Mensaje procesado nuevamente
  
Resultado esperado:
  ✓ Primera ejecución: ticket cambia a "paid"
  ✓ Segunda ejecución: detecta idempotencia
  ✓ Log: "Ticket {id} already paid. Skipping duplicate event"
  ✓ No se crea duplicado en ticket_history
  ✓ Version no se incrementa la segunda vez
  ✓ Payment status sigue siendo "approved"
```

---

## 4. Casos de Prueba de Validación

### 4.1 Validaciones de Negocio

#### TC-VAL-001: No Se Puede Pagar Ticket No Reservado
```yaml
Setup: Ticket con status="available"
Acción: Payment Service recibe PaymentApprovedEvent
Resultado esperado:
  ✓ Validación falla: "Invalid ticket status: available"
  ✓ Ticket NO cambia a "paid"
  ✓ Mensaje ACK (no reintentar)
```

#### TC-VAL-002: No Se Puede Reservar Ticket Ya Reservado
```yaml
Setup: Ticket con status="reserved"
Acción: Reservation Service recibe evento de reserva
Resultado esperado:
  ✓ Validación falla: "Ticket is not available"
  ✓ Status no cambia
  ✓ Mensaje ACK sin procesar
```

#### TC-VAL-003: Pago Después de Expiración
```yaml
Setup:
  - Ticket reservado hace 6 minutos
  - expires_at = NOW() - 1 minuto
  
Acción: PaymentApprovedEvent llega
Resultado esperado:
  ✓ Validación TTL falla
  ✓ Ticket transiciona a "released" (no a "paid")
  ✓ Payment status = "expired"
  ✓ Log: "Payment received after TTL"
```

---

### 4.2 Validaciones de Input

#### TC-VAL-004: Email Inválido en Reserva
```yaml
Casos:
  - reservedBy = "not-an-email"
  - reservedBy = "@nodomain.com"
  - reservedBy = "user@"
  
Resultado esperado:
  ✓ Status: 400 Bad Request (si se valida en API)
  ✓ O procesamiento exitoso (si no hay validación de formato)
  
Recomendación: Agregar validación de email
```

#### TC-VAL-005: Fecha de Evento en el Pasado
```yaml
Datos:
  {
    "name": "Evento Pasado",
    "startsAt": "2020-01-01T10:00:00Z"
  }
  
Resultado actual:
  ✓ Se permite crear evento en el pasado
  
Recomendación: Validar startsAt >= NOW() en producción
```

---

## 5. Casos de Prueba de Resiliencia

### 5.1 Tolerancia a Fallos

#### TC-RES-001: RabbitMQ Caído Durante Reserva
```yaml
Pasos:
  1. Detener contenedor de RabbitMQ: docker stop ticketing_broker
  2. Intentar POST /api/tickets/reserve
  
Resultado esperado:
  ✓ Producer Service lanza excepción
  ✓ Status: 500 Internal Server Error
  ✓ Log de error registrado
  ✓ Usuario recibe mensaje amigable
  
Recuperación:
  1. Reiniciar RabbitMQ: docker start ticketing_broker
  2. Verificar que servicios se reconecten automáticamente
  3. Reintentar reserva: debe funcionar
```

#### TC-RES-002: PostgreSQL Caído Durante Lectura
```yaml
Pasos:
  1. Detener PostgreSQL: docker stop ticketing_db
  2. Intentar GET /api/events
  
Resultado esperado:
  ✓ Status: 500 Internal Server Error
  ✓ Mensaje: "Error al obtener eventos"
  ✓ No se expone stack trace al cliente
  
Recuperación:
  1. Reiniciar PostgreSQL
  2. Verificar conexión de EF Core se restablece
  3. Retry debe funcionar sin reiniciar servicios
```

#### TC-RES-003: Payment Service Cae Mientras Procesa
```yaml
Escenario:
  1. PaymentApprovedEvent en queue
  2. Payment Service comienza a procesar
  3. Servicio se detiene abruptamente (kill -9)
  4. Mensaje NO fue ACK
  
Resultado esperado:
  ✓ RabbitMQ mantiene mensaje en queue
  ✓ Al reiniciar Payment Service, mensaje se reprocesa
  ✓ Idempotencia garantiza resultado correcto
  ✓ Ticket termina en estado "paid"
```

---

### 5.2 Reintentos y Dead Letter Queue

#### TC-RES-004: Mensaje Inválido en Queue
```yaml
Acción: 
  - Publicar mensaje con JSON inválido a q.ticket.reserved
  
Resultado esperado:
  ✓ Consumer detecta error de deserialización
  ✓ Mensaje se mueve a Dead Letter Queue (si configurada)
  ✓ O se hace ACK para evitar bloqueo de cola
  ✓ Log de error con payload completo
```

---

## 6. Casos de Prueba de Performance

### 6.1 Carga de Lectura

#### TC-PERF-001: Listar 1000 Eventos
```yaml
Setup: Base de datos con 1000 eventos
Acción: GET /api/events
Métricas esperadas:
  ✓ Tiempo de respuesta: < 500ms (p95)
  ✓ Memoria del servicio: < 200MB
  ✓ Query ejecutado con índice (EXPLAIN ANALYZE)
```

#### TC-PERF-002: Listar 10,000 Tickets de Un Evento
```yaml
Setup: Evento con 10,000 tickets
Acción: GET /api/tickets/event/1
Métricas esperadas:
  ✓ Tiempo: < 2 segundos
  ✓ Response size: ~1.5MB (verificar compresión)
  
Recomendación: Implementar paginación
```

---

### 6.2 Carga de Escritura

#### TC-PERF-003: Crear 1000 Tickets en Lote
```yaml
Acción: POST /api/tickets/bulk con quantity=1000
Métricas esperadas:
  ✓ Tiempo: < 5 segundos
  ✓ Inserts en transacción única
  ✓ Sin degradación de performance en RabbitMQ
```

#### TC-PERF-004: 100 Reservas Simultáneas
```yaml
Setup: 100 tickets disponibles
Acción: 100 usuarios reservan al mismo tiempo
Métricas esperadas:
  ✓ Tiempo total: < 10 segundos
  ✓ Todas las reservas procesadas
  ✓ RabbitMQ queue drena completamente
  ✓ CPU del Reservation Service: < 80%
```

---

### 6.3 Throughput de Mensajería

#### TC-PERF-005: Procesar 1000 Pagos por Minuto
```yaml
Setup: 
  - 1000 tickets reservados
  - 1000 PaymentApprovedEvent en queue
  
Métricas esperadas:
  ✓ Payment Service procesa todos en < 60 segundos
  ✓ Throughput: ~17 mensajes/segundo
  ✓ No hay errores de timeout
  ✓ BD mantiene < 100 conexiones activas
```

---

## 7. Casos de Prueba End-to-End

### 7.1 Flujo Completo con Frontend

#### TC-E2E-001: Usuario Compra Ticket Via UI
```yaml
Herramienta: Cypress / Playwright
Pasos automatizados:
  1. Navegar a http://localhost:3000/buy
  2. Esperar carga de eventos
  3. Hacer click en primer evento con tickets disponibles
  4. Hacer click en botón "Reservar Ticket"
  5. Esperar indicador de "Reservando..."
  6. Verificar mensaje "Ticket reservado exitosamente"
  7. Llenar formulario de pago
  8. Hacer click en "Procesar Pago"
  9. Esperar indicador de "Procesando pago..."
  10. Verificar mensaje "Pago confirmado"
  
Verificaciones:
  ✓ Todos los elementos visibles en tiempo adecuado
  ✓ No hay errores en consola del navegador
  ✓ Loading states funcionan
  ✓ Mensajes de error/éxito se muestran
  ✓ Estado del ticket se actualiza en UI
  
Tiempo total: < 30 segundos
```

---

### 7.2 Flujo Admin

#### TC-E2E-002: Admin Crea Evento y Monitorea Ventas
```yaml
Pasos:
  1. Login como admin (si hay auth)
  2. Navegar a /admin
  3. Click en "Crear Evento"
  4. Llenar formulario (nombre, fecha)
  5. Guardar evento
  6. Agregar 50 tickets al evento
  7. Ver dashboard con estadísticas
  8. Verificar contadores:
     - Available: 50
     - Reserved: 0
     - Paid: 0
  
Verificaciones:
  ✓ Evento aparece en lista
  ✓ Contadores se actualizan en tiempo real (SWR)
  ✓ Gráficos/stats renderizados
```

---

## 8. Scripts de Prueba

### 8.1 Script de Prueba Funcional (Bash)

```bash
#!/bin/bash
# test-functional.sh

API_CRUD="http://localhost:8002"
API_PRODUCER="http://localhost:8001"

echo "=== Test Suite: Functional Tests ==="

# TC-API-001: Crear evento
echo "Test 1: Crear evento válido"
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_CRUD/api/events" \
  -H "Content-Type: application/json" \
  -d '{"name":"Evento Test","startsAt":"2026-12-31T20:00:00Z"}')

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | head -n-1)

if [ "$HTTP_CODE" -eq 201 ]; then
  EVENT_ID=$(echo "$BODY" | jq -r '.id')
  echo "✓ Evento creado con ID: $EVENT_ID"
else
  echo "✗ Falló crear evento. Status: $HTTP_CODE"
  exit 1
fi

# TC-API-009: Crear tickets
echo "Test 2: Crear 10 tickets"
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_CRUD/api/tickets/bulk" \
  -H "Content-Type: application/json" \
  -d "{\"eventId\":$EVENT_ID,\"quantity\":10}")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
if [ "$HTTP_CODE" -eq 201 ]; then
  echo "✓ 10 tickets creados"
else
  echo "✗ Falló crear tickets. Status: $HTTP_CODE"
  exit 1
fi

# TC-API-012: Listar tickets
echo "Test 3: Listar tickets del evento"
TICKETS=$(curl -s "$API_CRUD/api/tickets/event/$EVENT_ID")
COUNT=$(echo "$TICKETS" | jq 'length')

if [ "$COUNT" -eq 10 ]; then
  echo "✓ Listado correcto: 10 tickets"
else
  echo "✗ Esperados 10 tickets, obtenidos: $COUNT"
  exit 1
fi

# TC-API-017: Reservar ticket
echo "Test 4: Reservar primer ticket"
TICKET_ID=$(echo "$TICKETS" | jq -r '.[0].id')
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_PRODUCER/api/tickets/reserve" \
  -H "Content-Type: application/json" \
  -d "{
    \"eventId\":$EVENT_ID,
    \"ticketId\":$TICKET_ID,
    \"orderId\":\"ORD-TEST-001\",
    \"reservedBy\":\"test@example.com\",
    \"expiresInSeconds\":300
  }")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
if [ "$HTTP_CODE" -eq 202 ]; then
  echo "✓ Reserva encolada correctamente"
else
  echo "✗ Falló reserva. Status: $HTTP_CODE"
  exit 1
fi

# Esperar procesamiento asíncrono
echo "Esperando procesamiento de reserva (5s)..."
sleep 5

# Verificar que ticket cambió a reserved
TICKET=$(curl -s "$API_CRUD/api/tickets/$TICKET_ID")
STATUS=$(echo "$TICKET" | jq -r '.status')

if [ "$STATUS" == "reserved" ]; then
  echo "✓ Ticket reservado correctamente"
else
  echo "✗ Status esperado: reserved, obtenido: $STATUS"
  exit 1
fi

echo ""
echo "=== ✓ Todos los tests funcionales pasaron ==="
```

---

### 8.2 Script de Prueba de Carga (con Apache Bench)

```bash
#!/bin/bash
# test-load.sh

API_CRUD="http://localhost:8002"

echo "=== Test Suite: Load Tests ==="

# TC-PERF-001: Listar eventos (100 requests, 10 concurrent)
echo "Test: GET /api/events (100 requests, concurrency=10)"
ab -n 100 -c 10 -g results-events.tsv "$API_CRUD/api/events"

# Análisis básico
echo ""
echo "Analizando resultados..."
avg_time=$(awk '{sum+=$5; count++} END {print sum/count}' results-events.tsv | tail -n +2)
echo "Tiempo promedio: ${avg_time}ms"

if (( $(echo "$avg_time < 500" | bc -l) )); then
  echo "✓ Performance aceptable (< 500ms)"
else
  echo "⚠ Performance degradada (> 500ms)"
fi

echo ""
echo "=== Test de carga completado ==="
```

---

### 8.3 Script de Prueba de Concurrencia (Python)

```python
#!/usr/bin/env python3
# test-concurrency.py

import requests
import threading
import time
from concurrent.futures import ThreadPoolExecutor, as_completed

API_CRUD = "http://localhost:8002"
API_PRODUCER = "http://localhost:8001"

def reserve_ticket(ticket_id, user_id):
    """TC-CONC-002: Múltiples usuarios reservan simultáneamente"""
    payload = {
        "eventId": 1,
        "ticketId": ticket_id,
        "orderId": f"ORD-{user_id}",
        "reservedBy": f"user{user_id}@test.com",
        "expiresInSeconds": 300
    }
    
    try:
        response = requests.post(
            f"{API_PRODUCER}/api/tickets/reserve",
            json=payload,
            timeout=5
        )
        return {
            "user_id": user_id,
            "status": response.status_code,
            "success": response.status_code == 202
        }
    except Exception as e:
        return {
            "user_id": user_id,
            "status": 0,
            "success": False,
            "error": str(e)
        }

def test_concurrent_reservations():
    """100 usuarios compiten por 10 tickets"""
    print("=== TC-CONC-002: Concurrent Reservations ===")
    
    # Setup: Crear evento con 10 tickets
    event_resp = requests.post(f"{API_CRUD}/api/events", json={
        "name": "Concierto Concurrencia Test",
        "startsAt": "2026-12-31T20:00:00Z"
    })
    event_id = event_resp.json()["id"]
    
    tickets_resp = requests.post(f"{API_CRUD}/api/tickets/bulk", json={
        "eventId": event_id,
        "quantity": 10
    })
    tickets = tickets_resp.json()
    ticket_ids = [t["id"] for t in tickets]
    
    print(f"✓ Evento creado: ID={event_id}")
    print(f"✓ 10 tickets creados: IDs={ticket_ids[:3]}...")
    
    # Test: 100 usuarios intentan reservar ticket 1
    target_ticket = ticket_ids[0]
    num_users = 100
    
    print(f"\nLanzando {num_users} peticiones concurrentes para ticket {target_ticket}...")
    start_time = time.time()
    
    with ThreadPoolExecutor(max_workers=50) as executor:
        futures = [
            executor.submit(reserve_ticket, target_ticket, user_id)
            for user_id in range(1, num_users + 1)
        ]
        
        results = [future.result() for future in as_completed(futures)]
    
    elapsed = time.time() - start_time
    
    # Análisis
    successful = sum(1 for r in results if r["success"])
    failed = num_users - successful
    
    print(f"\n=== Resultados ===")
    print(f"Tiempo total: {elapsed:.2f}s")
    print(f"Reservas exitosas: {successful}")
    print(f"Reservas fallidas: {failed}")
    
    # Verificar en BD
    time.sleep(5)  # Esperar procesamiento async
    ticket = requests.get(f"{API_CRUD}/api/tickets/{target_ticket}").json()
    
    print(f"\nStatus final del ticket: {ticket['status']}")
    print(f"Version final: {ticket['version']}")
    
    # Assertions
    assert successful <= 1, f"❌ Over-booking detectado: {successful} reservas exitosas"
    assert ticket["status"] in ["reserved", "available"], "❌ Status inconsistente"
    
    if successful == 1 and ticket["status"] == "reserved":
        print("✓ Test PASSED: Solo 1 reserva exitosa, sin over-booking")
    else:
        print(f"⚠ Test WARNING: {successful} exitosas, status={ticket['status']}")

if __name__ == "__main__":
    test_concurrent_reservations()
```

---

### 8.4 Checklist de Testing

```markdown
## Pre-Deployment Testing Checklist

### Funcional
- [ ] Todos los endpoints responden (smoke test)
- [ ] CRUD de eventos funciona correctamente
- [ ] CRUD de tickets funciona correctamente
- [ ] Reserva asíncrona funciona
- [ ] Pago asíncrono funciona
- [ ] Polling desde frontend funciona
- [ ] Estados de tickets son correctos

### Validación
- [ ] Inputs inválidos son rechazados
- [ ] Errores 400 tienen mensajes claros
- [ ] No se expone información sensible en errores

### Concurrencia
- [ ] No hay over-booking de tickets
- [ ] Optimistic locking funciona
- [ ] Idempotencia garantizada

### Resiliencia
- [ ] Sistema se recupera de caída de RabbitMQ
- [ ] Sistema se recupera de caída de PostgreSQL
- [ ] Mensajes no se pierden

### Performance
- [ ] GET /api/events responde en < 500ms
- [ ] Reserva procesada en < 5s
- [ ] Pago procesado en < 10s

### Seguridad
- [ ] CORS configurado correctamente
- [ ] No hay SQL injection posible
- [ ] Logs no exponen passwords/tokens

### Observabilidad
- [ ] Logs estructurados funcionan
- [ ] Health checks responden
- [ ] Métricas de RabbitMQ visibles
```

---

## 📊 Métricas de Cobertura Esperadas

```yaml
Cobertura de Código:
  - Backend Services: > 80%
  - Repositories: > 90%
  - Controllers: > 75%
  - Frontend Hooks: > 70%

Casos de Prueba por Categoría:
  - API Tests: 20
  - Business Flow Tests: 4
  - Concurrency Tests: 3
  - Validation Tests: 5
  - Resilience Tests: 4
  - Performance Tests: 5
  - E2E Tests: 2
  - Total: 43 casos base
```

---

## 🚀 Ejecución de Tests

### Orden recomendado:
1. **Unit Tests** (si existen) - más rápidos, locales
2. **API Tests** - verification de contratos
3. **Integration Tests** - con servicios reales
4. **Concurrency Tests** - detección de race conditions
5. **E2E Tests** - validación completa del flujo
6. **Performance Tests** - bajo carga controlada
7. **Resilience Tests** - chaos engineering

### Ambientes:
- **Local**: Todos los tests salvo performance pesados
- **Staging**: Suite completa antes de deploy
- **Production**: Solo smoke tests y health checks

---

**Generado el**: 12 de febrero de 2026  
**Versión del sistema**: MVP Week 0  
**Mantenido por**: QA Team + Backend Team
