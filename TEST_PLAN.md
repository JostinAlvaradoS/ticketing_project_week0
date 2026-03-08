# Plan de Pruebas Formal: Speckit Ticketing MVP
**Versión:** 1.0  
**Estado:** Activo  
**Rol Responsable:** QA Senior  
**Fecha:** 2026-03-08

## 1. Introducción
Este documento define la estrategia, el alcance y los recursos para las actividades de prueba del MVP de la plataforma de Ticketing. El objetivo principal es garantizar que el flujo crítico de compra (Reserva -> Orden -> Pago -> Emisión) sea robusto, seguro y libre de condiciones de carrera.

## 2. Alcance de las Pruebas

### 2.1 Elementos de Prueba (En Alcance)
- **Servicio de Identidad (Identity):** Autenticación JWT y registro.
- **Servicio de Catálogo (Catalog):** Consulta de eventos y mapas de asientos.
- **Servicio de Inventario (Inventory):** Reservas temporales (TTL 15 min), manejo de concurrencia y locks en Redis.
- **Servicio de Órdenes (Ordering):** Gestión de carritos y estados de órdenes.
- **Servicio de Pagos (Payment):** Procesamiento simulado (Simulated Success/Failure).
- **Servicio de Cumplimiento (Fulfillment):** Generación de PDFs y códigos QR.
- **Servicio de Notificación (Notification):** Envío de correos y reintentos.

### 2.2 Elementos Fuera de Alcance
- Integración con pasarelas de pago reales (Stripe/PayPal) - Diferido.
- Interfaz de Usuario (Frontend Next.js) - Excluido de este ciclo por decisión técnica.

## 3. Estrategia de Prueba

### 3.1 Niveles de Prueba
| Nivel | Herramientas | Enfoque |
| :--- | :--- | :--- |
| **Unitarias** | xUnit, Moq | Verificación de lógica pura en Dominio y Handlers de Aplicación. Cobertura objetivo: >85%. |
| **Integración** | Testcontainers, Kafka, Redis, Postgres | Validación de adaptadores de infraestructura y persistencia en esquemas específicos. |
| **Contrato** | OpenAPI Validator | Asegurar que los servicios cumplen con los esquemas definidos en `/contracts/openapi/`. |
| **E2E / Smoke** | Scripts Bash, Docker-Compose | Validación de flujo completo en entorno pre-producción (Docker). |

### 3.2 Técnicas de Diseño de Pruebas
1. **Análisis de Valores Límite:** Aplicado en los TTL de reserva (14:59s vs 15:01s) y stock de tickets.
2. **Partición de Equivalencia:** Estados de asientos (`available`, `reserved`, `sold`).
3. **Pruebas de Transición de Estados:** Específicamente para el ciclo de vida de la Orden (`Draft` -> `Pending` -> `Paid` -> `Fulfilled`).
4. **Pruebas de Concurrencia:** Simulación de múltiples usuarios intentando reservar el mismo asiento (`RedisLock`).

## 4. Matriz de Pruebas por Historia de Usuario (HU)

A continuación se mapean las Historias de Usuario (HU) definidas en las [specs](specs/001-ticketing-mvp/spec.md) con los casos de prueba implementados y por implementar.

### HU-P1: Compra de Boleto (Critical Path)
*Como Cliente, quiero seleccionar un asiento, reservarlo temporalmente, agregarlo al carrito y pagar para recibir mi boleto con QR.*

| ID Prueba | Escenario de Aceptación | Nivel | Técnica / Limites establecidos | Estado |
| :--- | :--- | :--- | :--- | :--- |
| **TC-P1-01** | Selección y reserva con TTL 15 min. | Integración | **Valores Límite:** 14:59 (Válido), 15:01 (Expirado). | ✅ Implementado |
| **TC-P1-02** | Bloqueo de doble reserva simultánea. | Integración | **Concurrencia:** Lock en Redis (TTL 30s) / Optimistic Lock (Version control). | ✅ Implementado |
| **TC-P1-03** | Conversión de Reserva a Orden `Draft`. | Unitario | **Transición Estados:** Solo 1 Orden `Draft` permitida por `User_Id`. | ✅ Implementado |
| **TC-P1-04** | Pago Exitoso (Simulado) -> Orden `Paid`. | Unitario | **Partición Equiv:** Balance >= Total (Success), Balance < Total (Fail). | ✅ Implementado |
| **TC-P1-05** | Fallo en Pago -> Orden `Pending/Failed`. | Unitario | **Error Guessing:** Timeout de Red, Tarjeta declinada (Simulación). | ✅ Implementado |
| **TC-P1-06** | Generación de Ticket PDF con QR. | Integración | **Caja Negra:** Formato PDF plano A4, QR debe contener `Ticket_Id` + `Hash`. | ✅ Implementado |

### HU-P2: Navegación y Descubrimiento
*Como Visitante, quiero ver eventos y mapas de asientos para elegir qué reservar.*

| ID Prueba | Escenario de Aceptación | Nivel | Técnica / Limites establecidos | Estado |
| :--- | :--- | :--- | :--- | :--- |
| **TC-P2-01** | Consulta de catálogo de eventos activos. | Unitario | **Caja Blanca:** Solo eventos con `Start_Date` > `Now`. | ✅ Implementado |
| **TC-P2-02** | Mapa de asientos refleja disponibilidad. | Contrato | **API Testing:** Sincronización `Catalog` vs `Inventory` < 200ms. | ✅ Implementado |

### HU-P3: Gestión por Organizador
*Como Organizador, quiero crear eventos y configurar asientos.*

| ID Prueba | Escenario de Aceptación | Nivel | Técnica / Limites establecidos | Estado |
| :--- | :--- | :--- | :--- | :--- |
| **TC-P3-01** | Creación de evento con asientos. | Integración | **Límites:** Máximo 50,000 asientos por evento (Escalabilidad DB). | ✅ Implementado |

## 5. Casos de Prueba Detallados (Baseline)

### 5.1 Inventario (Inventory Service)
*   **TC-INV-01 (HU-P1):** Crear reserva exitosa con TTL de 15 minutos en Redis.
*   **TC-INV-02 (HU-P1):** Doble reserva simultánea. Validación de `RedisLock` y `Optimistic Locking`.
*   **TC-INV-03 (HU-P1):** Liberación automática de asiento tras expiración de TTL.

### 5.2 Órdenes (Ordering Service)
*   **TC-ORD-01 (HU-P1):** Validación de regla de negocio: "Un solo carrito activo por usuario".
*   **TC-ORD-02 (HU-P1):** Transición de `Pending` a `Paid` al recibir evento de Kafka `payment-succeeded`.

### 5.3 Cumplimiento y Notificaciones
*   **TC-FUL-01 (HU-P1):** Generación de artefacto PDF post-pago.
*   **TC-NOT-01 (HU-P1):** Reintento de envío de email ante fallo de infraestructura (Service Discovery/SMTP).

## 6. Criterios de Aceptación (QA Gates)
- **SC-001:** El flujo completo debe durar < 30 segundos incluyendo generación de PDF.
- **SC-002:** Cero (0) fallos en pruebas de concurrencia de reserva.
- **SC-003:** Cobertura de código mínima del 85% en capas de Dominio y Aplicación.

## 6. Configuración del Entorno
- **Base de Datos:** PostgreSQL con esquemas `bc_catalog`, `bc_inventory`, `bc_ordering`, etc.
- **Mensajería:** Kafka (vía Confluent client).
- **Cache/Locks:** Redis Stack.
- **Infraestructura de Test:** `Testcontainers` para levantar instancias reales durante la ejecución de los tests.

## 7. Plan de Defectos
- **Severidad Bloqueante:** Fallo en reserva o pérdida de mensaje de pago en Kafka.
- **Severidad Mayor:** Fallo en generación de PDF o notificación.
- **Severidad Menor:** Inconsistencias en logs o trazas de OpenTelemetry.

---
**Firmado por:** GitHub Copilot (QA Senior AI Agent)
