# Plan de Pruebas - Speckit Ticketing System

## 1. Información General del Proyecto

### 1.1 Propósito

Este documento establece el Plan de Pruebas formal para el sistema Speckit Ticketing, un sistema de venta de boletos basado en microservicios .NET 8 con arquitectura hexagonal.

### 1.2 Alcance del Proyecto

**En desarrollo:**
- Plataforma de venta de boletos para eventos
- Arquitectura de microservicios: Catalog, Inventory, Ordering, Payment, Fulfillment, Notification, Identity
- Infraestructura: PostgreSQL (schemas por bounded context), Redis (locks y caching), Kafka (eventos asíncronos)
- Frontend: Next.js (MVP sin tests por ahora)

**En pruebas:**
- Todos los servicios backend
- Integración entre servicios
- Flujos de negocio críticos

**Excluido del alcance:**
- Frontend Next.js (decisión del equipo)
- Tests de seguridad detallados (OWASP)
- Tests de chaos engineering

---

## 2. Estrategia de Pruebas

### 2.1 Enfoque General

El enfoque de pruebas sigue la Pirámide de Testing adaptada al contexto de microservicios:

```
                    ▲
                   /│\        E2E / Acceptance (5%)
                  / │ \       Playwright - 5 flujos críticos negocio
                 /  │  \
                /───┼───\     Integración (25%)
               /    │    \    Testcontainers: BD, Redis, Kafka reales
              /     │     \
             /──────┼──────\  Unitarios (70%)
            /       │       \ xUnit + Moq - handlers, dominio
           ─────────────────
```

### 2.2 Tipos de Pruebas

| Tipo | Propósito | Herramientas | Frecuencia |
|------|-----------|--------------|------------|
| **Unit Tests** | Verificar lógica de negocio aislada | xUnit, Moq, FluentAssertions | Cada commit |
| **Integration Tests** | Validar interacción entre componentes | Testcontainers, Polly | Cada commit |
| **Contract Tests** | Validar contratos API OpenAPI | Swashbuckle, Approval Tests | Pre-release |
| **E2E Tests** | Validar flujos end-to-end | Playwright | Nightly |
| **Performance Tests** | Validar SLAs de rendimiento | k6 | Weekly |
| **Smoke Tests** | Validar despliegue exitoso | Docker Compose, Scripts bash | Post-deploy |

### 2.3 Niveles de Prueba

| Nivel | Descripción | Foco |
|-------|-------------|------|
| **Unit** | Tests de métodos y clases individuales | Dominio y Handlers |
| **Component** | Tests de un servicio completo | API + Lógica + Repo |
| **Integration** | Tests entre servicios | HTTP, Kafka, Redis, BD |
| **System** | Tests del sistema completo | Todos los servicios |
| **Acceptance** | Tests de negocio con el usuario | User Stories |

---

## 3. Criterios de Prueba

### 3.1 Criterios de Entrada

Antes de iniciar la fase de pruebas se debe cumplir:

- [ ] Código fuente en repositorio Git
- [ ] Builds automatizados configurados (CI)
- [ ] Entorno de pruebas disponible (local o Docker)
- [ ] Datos de prueba preparados (seeds)
- [ ] Contratos OpenAPI actualizados
- [ ] Criterios de aceptación documentados en HU

### 3.2 Criterios de Salida

La fase de pruebas se considera completa cuando:

- [ ] 90%+ cobertura de código
- [ ] 0 tests fallidos en suite principal
- [ ] Todos los criterios de aceptación cubiertos
- [ ] Tests de rendimiento dentro de SLAs
- [ ] Reporte de defectos cerrado o aceptado
- [ ] Cobertura de ramas > 95%

### 3.3 Criterios de Suspensión

La ejecución de pruebas se suspende si:

- Defectos críticos bloquean > 30% de tests
- Entorno de pruebas no disponible
- Build principal con errores de compilación

---

## 4. Funcionalidades a Probar

### 4.1 Matriz de Funcionalidades

| Módulo | Funcionalidad | Prioridad | Tipo Prueba Principal |
|--------|--------------|-----------|---------------------|
| **Inventory** | Crear reserva de asiento | P1 | Unit + Integration |
| **Inventory** | Validar disponibilidad | P1 | Unit |
| **Inventory** | Concurrent reservation | P1 | Integration |
| **Inventory** | TTL reserva expirada | P2 | Integration |
| **Ordering** | Agregar al carrito | P1 | Unit + Integration |
| **Ordering** | Checkout pedido | P1 | Unit + Integration |
| **Ordering** | Obtener pedido | P2 | Unit |
| **Payment** | Procesar pago | P1 | Unit + Integration |
| **Payment** | Idempotencia | P1 | Unit |
| **Payment** | Pago fallido | P1 | Unit |
| **Fulfillment** | Generar ticket | P1 | Unit + Integration |
| **Fulfillment** | Generar QR | P2 | Unit |
| **Notification** | Enviar email | P1 | Unit + Integration |
| **Notification** | Idempotencia | P1 | Unit |
| **Catalog** | Listar eventos | P2 | Unit |
| **Catalog** | Seatmap | P2 | Unit |
| **Identity** | Crear usuario | P2 | Unit |
| **Identity** | Generar token | P2 | Unit |

### 4.2 Flujos Críticos (E2E)

| Flujo | Descripción | Pasos |
|-------|-------------|-------|
| **F1: Compra Exitosa** | Usuario compra boleto exitosamente | Browse → Select Seat → Reserve → Add to Cart → Checkout → Pay → Receive Ticket → Email |
| **F2: Reserva Concurrente** | Dos usuarios reservan mismo asiento | Usuario A reserva → Usuario B intenta reservar → Solo A tiene reserva |
| **F3: Reserva Expirada** | Reserva expira antes de pago | Reserva → TTL expira → Intentar pagar → Fallo |
| **F4: Pago Doble** | Usuario intenta pagar dos veces | Primer pago → Éxito → Segundo pago → Retorna éxito original |
| **F5: Fallo en Notificación** | Email falla pero ticket emitido | Pago → Ticket generado → Email falla → Notificación registrada |

---

## 5. Diseño de Pruebas

### 5.1 Estrategia de Datos de Prueba

**Principio**: Cada test debe ser independiente y no depender de estado de otros tests.

**Técnicas**:
- Bases de datos en memoria para unit tests
- Testcontainers con PostgreSQL, Redis, Kafka para integración
- Datos anónimos/fixtures por test
- Limpieza post-ejecución

**Tipos de datos**:
- Datos válidos (happy path)
- Datos inválidos (validación)
- Datos límite (boundary testing)
- Datos en borde de error (edge cases)

### 5.2 Estrategia de Datos de Prueba por Servicio

| Servicio | Datos Requeridos | Origen |
|----------|-----------------|--------|
| Inventory | Seats (available, reserved, sold) | Seed o creación en test |
| Ordering | Orders (draft, pending, paid, cancelled) | Seed o creación en test |
| Payment | Payments (pending, succeeded, failed) | Seed o creación en test |
| Fulfillment | Tickets (issued, pending) | Generados en flujo |
| Notification | EmailNotifications (sent, failed) | Generados en flujo |
| Catalog | Events, Venues, Seats | Seed data |

### 5.3 Casos de Prueba Detallados

#### 5.3.1 Caso de Uso: Crear Reserva (Inventory)

**CP-001: Reserva exitosa de asiento disponible**

| Campo | Valor |
|-------|-------|
| ID | CP-INV-001 |
| Título | Reserva exitosa |
| Precondición | Asiento con estado 'available' |
| Pasos | 1. Solicitar reserva con seatId válido |
| Resultado | Reserva creada, estado 'active', evento publicado |
| Postcondición | Asiento marcado 'reserved' |

**CP-002: Reserva fallida - asiento ocupado**

| Campo | Valor |
|-------|-------|
| ID | CP-INV-002 |
| Título | Asiento ya reservado |
| Precondición | Asiento con estado 'reserved' |
| Pasos | 1. Solicitar reserva |
| Resultado | InvalidOperationException: "Seat already reserved" |
| Tipo | Exception test |

**CP-003: Reserva concurrente**

| Campo | Valor |
|-------|-------|
| ID | CP-INV-003 |
| Título | Solo una reserva exitosa |
| Precondición | Asiento 'available' |
| Pasos | 1. Cliente A adquiere lock |
| | 2. Cliente B intenta reservar |
| Resultado | Cliente A: éxito, Cliente B: fallo (lock) |
| Tipo | Concurrency test |

**CP-004: Reserva fallida - lock no adquirido**

| Campo | Valor |
|-------|-------|
| ID | CP-INV-004 |
| Título | Redis lock no disponible |
| Precondición | Redis retorna null en acquire |
| Pasos | 1. Solicitar reserva |
| Resultado | InvalidOperationException: "Could not acquire lock" |
| Tipo | Error handling |

**CP-005: Reserva con parámetros inválidos**

| Campo | Valor |
|-------|-------|
| ID | CP-INV-005 |
| Título | Validación de entrada |
| Precondición | Ninguna |
| Pasos | 1. SeatId = Guid.Empty |
| | 2. CustomerId = "" |
| Resultado | ArgumentException |
| Tipo | Boundary test |

#### 5.3.2 Caso de Uso: Agregar al Carrito (Ordering)

**CP-006: Agregar primer item al carrito**

| Campo | Valor |
|-------|-------|
| ID | CP-ORD-001 |
| Título | Crear nuevo pedido draft |
| Precondición | Usuario sin pedido draft |
| Pasos | 1. Reserva válida |
| | 2. Agregar al carrito |
| Resultado | Order en estado 'draft' con item |
| Tipo | Happy path |

**CP-007: Agregar segundo item**

| Campo | Valor |
|-------|-------|
| ID | CP-ORD-002 |
| Título | Actualizar pedido existente |
| Precondición | Usuario con pedido draft |
| Pasos | 1. Agregar segundo asiento |
| Resultado | Total actualizado, 2 items |
| Tipo | State transition |

**CP-008: Reserva expirada**

| Campo | Valor |
|-------|-------|
| ID | CP-ORD-003 |
| Título | Reserva no válida |
| Precondición | Reserva con estado 'expired' |
| Pasos | 1. Intentar agregar |
| Resultado | Error: "Reservation expired" |
| Tipo | Business logic |

**CP-009: Asiento duplicado**

| Campo | Valor |
|-------|-------|
| ID | CP-ORD-004 |
| Título | Mismo asiento en carrito |
| Precondición | Asiento ya en pedido draft |
| Pasos | 1. Intentar agregar mismo asiento |
| Resultado | Error: "Seat already in cart" |
| Tipo | Duplicate test |

#### 5.3.3 Caso de Uso: Procesar Pago (Payment)

**CP-010: Pago exitoso**

| Campo | Valor |
|-------|-------|
| ID | CP-PAY-001 |
| Título | Pago procesado exitosamente |
| Precondición | Order 'pending', reserva válida |
| Pasos | 1. Procesar pago |
| Resultado | Payment 'succeeded', evento publicado |
| Tipo | Happy path |

**CP-011: Idempotencia**

| Campo | Valor |
|-------|-------|
| ID | CP-PAY-002 |
| Título | Pago duplicado retorna mismo resultado |
| Precondición | Pago 'succeeded' existente |
| Pasos | 1. Procesar pago (1ra vez) |
| | 2. Procesar pago (2da vez) |
| Resultado | Ambas retornan éxito, solo 1 cargo |
| Tipo | Idempotency |

**CP-012: Pago fallido - fondos insuficientes**

| Campo | Valor |
|-------|-------|
| ID | CP-PAY-003 |
| Título | Simulación de rechazo |
| Precondición | Orden válida |
| Pasos | 1. Procesar pago con simulación 'failed' |
| Resultado | Payment 'failed', evento publicado |
| Tipo | Error handling |

**CP-013: Validación de orden falla**

| Campo | Valor |
|-------|-------|
| ID | CP-PAY-004 |
| Título | Orden no existe |
| Precondición | OrderId inexistente |
| Pasos | 1. Procesar pago |
| Resultado | Error: "Order not found" |
| Tipo | Validation |

#### 5.3.4 Caso de Uso: Generación de Ticket (Fulfillment)

**CP-014: Ticket generado correctamente**

| Campo | Valor |
|-------|-------|
| ID | CP-FUL-001 |
| Título | Ticket con QR generado |
| Precondición | Pago 'succeeded' |
| Pasos | 1. Procesar payment succeeded |
| Resultado | Ticket creado, QR generado, evento publicado |
| Tipo | Happy path |

#### 5.3.5 Caso de Uso: Notificación (Notification)

**CP-015: Email enviado**

| Campo | Valor |
|-------|-------|
| ID | CP-NOT-001 |
| Título | Notificación enviada |
| Precondición | Ticket emitido |
| Pasos | 1. Enviar notificación |
| Resultado | Email enviado, registro persistido |
| Tipo | Happy path |

**CP-016: Idempotencia**

| Campo | Valor |
|-------|-------|
| ID | CP-NOT-002 |
| Título | No duplicar email |
| Precondición | Notificación anterior exitosa |
| Pasos | 1. Enviar notificación (2da vez) |
| Resultado | Retorna éxito, no envía email |
| Tipo | Idempotency |

---

## 6. Entornos de Prueba

### 6.1 Entornos Disponibles

| Entorno | Propósito | Tecnología |
|---------|-----------|-------------|
| **Local Development** | Desarrollo y debug | Docker Compose |
| **CI/CD** | Ejecución automatizada | GitHub Actions |
| **Staging** | Pruebas pre-producción | Kubernetes/Docker |

### 6.2 Configuración Local (Docker Compose)

```yaml
# Servicios requeridos para tests
services:
  postgres:
    image: postgres:15
    ports:
      - "5432:5432"
    environment:
      POSTGRES_PASSWORD: test

  redis:
    image: redis:7
    ports:
      - "6379:6379"

  kafka:
    image: confluentinc/cp-kafka:7.5.0
    ports:
      - "9092:9092"
    environment:
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181

  zookeeper:
    image: confluentinc/cp-zookeeper:7.5.0
```

### 6.3 Datos de Prueba (Seed Data)

| Tabla | Registros | Descripción |
|-------|-----------|-------------|
| events | 5 | Eventos de prueba |
| venues | 3 | Sedes |
| seats | 100 | Asientos por sección |
| users | 10 | Usuarios de prueba |

---

## 7. Herramientas y Tecnologías

### 7.1 Frameworks de Prueba

| Herramiente | Versión | Propósito |
|-------------|---------|-----------|
| xUnit | 2.6+ | Framework de tests |
| Moq | 4.20+ | Mocking de dependencias |
| FluentAssertions | 6.12+ | Aserciones legibles |
| Testcontainers | 10.0+ | Containers para tests |
| Polly | 8.0+ | Resilience y retry |

### 7.2 Herramientas de Cobertura

| Herramienta | Propósito |
|-------------|-----------|
| Coverlet | Recolección de cobertura |
| ReportGenerator | Reportes HTML |
| SonarQube | Análisis de calidad |

### 7.3 Herramientas de Rendimiento

| Herramienta | Propósito |
|-------------|-----------|
| k6 | Load testing |
| Grafana | Métricas |

---

## 8. Schedule de Pruebas

### 8.1 Timeline

| Semana | Actividad | Entregable |
|--------|-----------|------------|
| 1 | Auditoría de tests actuales | Reporte de cobertura |
| 2 | Contract testing | Validación OpenAPI |
| 3 | Integration tests completos | Suite de integración |
| 4 | Performance tests | Reporte k6 |
| 5 | E2E tests (Playwright) | 5 flujos automatizados |
| 6 | QA final y remediation | Suite completa |

### 8.2 Frecuencia de Ejecución

| Tipo Test | Frecuencia | Trigger |
|-----------|------------|---------|
| Unit | Cada commit | CI pipeline |
| Integration | Cada commit | CI pipeline |
| Contract | Pre-release | Merge a main |
| E2E | Nightly | Scheduled job |
| Performance | Weekly | Scheduled job |
| Smoke | Post-deploy | Deployment pipeline |

---

## 9. Roles y Responsabilidades

| Rol | Responsabilidad |
|-----|-----------------|
| **Desarrollador** | Escribir unit tests, verificar cobertura |
| **QA Engineer** | Diseñar tests de integración y E2E |
| **Tech Lead** | Revisar estrategia, aprobar criterios |
| **DevOps** | Mantener entornos de prueba |

---

## 10. Riesgos y Mitigaciones

### 10.1 Riesgos Identificados

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|--------------|---------|------------|
| Tests flaky por Kafka | Alta | Medio | Polly retry, datos idempotentes |
| Datos contaminados | Media | Alto | Testcontainers por suite |
| Performance degrade | Baja | Alto | k6 en CI semanal |
| Cobertura insuficiente | Media | Medio | Gate en CI con mínimo 85% |
| Entorno inestable | Baja | Alto | Health checks before tests |

### 10.2 Plan de Contingencia

- **Si tests fallan > 10%**: Detener pipeline, investigar causa raíz
- **Si cobertura baja**: Bloquear merge, aumentar tests
- **Si entorno no disponible**: Usar cache de Docker images

---

## 11. Métricas de Calidad

### 11.1 KPIs de Pruebas

| Métrica | Meta | Umbral de Alerta |
|---------|------|------------------|
| Cobertura de código | ≥90% | <85% |
| Cobertura de ramas | ≥95% | <90% |
| Tests ejecutados/día | 100% suite | <90% |
| Tiempo unit tests | <5 min | >7 min |
| Tiempo integración | <15 min | >20 min |
| Tests rotos | 0 | >0 |

### 11.2 Dashboard

Generar reporte semanal con:
- Cobertura por servicio
- Tests ejecutados vs planificados
- Defectos abiertos por severidad
- Tiempo de ejecución

---

## 12. Entregables

| Entregable | Descripción | Frecuencia |
|------------|-------------|------------|
| Suite de Unit Tests | Tests por handler | Cada commit |
| Suite de Integration Tests | Flujos cross-service | Cada commit |
| Reporte de Cobertura | HTML con detalles | Cada build |
| Reporte de Performance | Métricas k6 | Weekly |
| Plan de Pruebas | Este documento | Actualizado según necesidad |
| Casos de Prueba | Documentación BDD | Por feature |

---

## 13. Anexos

### 13.1 Glosario

| Término | Definición |
|---------|------------|
| **Unit Test** | Test de una unidad de código aislada |
| **Integration Test** | Test de interacción entre componentes |
| **E2E Test** | Test de extremo a extremo |
| **Contract Test** | Test de cumplimiento de API |
| **Idempotency** | Propiedad de ejecutar múltiples veces con mismo resultado |
| **TTL** | Time To Live - tiempo de expiración |
| **Happy Path** | Flujo sin errores |

### 13.2 Referencias

- Estrategia de Testing: `TESTING_STRATEGY.md`
- Especificación MVP: `specs/001-ticketing-mvp/spec.md`
- Contratos API: `contracts/openapi/`
- Contratos Kafka: `contracts/kafka/`

---

## 14. Aprobaciones

| Rol | Nombre | Fecha | Firma |
|-----|--------|-------|-------|
| Tech Lead | | | |
| QA Lead | | | |
| Product Owner | | | |

---

*Documento creado: 2026-03-06*
*Versión: 1.0*
