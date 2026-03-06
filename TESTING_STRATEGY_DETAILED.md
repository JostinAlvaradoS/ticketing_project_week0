# Plan Formal de Estrategia de Testing - Speckit Ticketing

## 1. Análisis de Casos de Uso Implementados

Basado en el análisis del código fuente, el sistema contempla los siguientes casos de uso organizados por servicio:

### 1.1 Servicio de Inventario (Inventory)

**Caso de Uso: Crear Reserva de Asiento**

- **Nombre técnico**: `CreateReservationCommandHandler`
- **Descripción**: Permite reservar un asiento temporalmente por 15 minutos mediante Redis distributed lock
- **Reglas de negocio**:
  - Un asiento solo puede ser reservado si está en estado `available`
  - La reserva tiene TTL de 15 minutos
  - Se utiliza Redis lock para evitar reservas concurrentes del mismo asiento
  - Se publica evento `reservation-created` a Kafka
- **Flujo**: Adquirir lock → Validar disponibilidad → Crear reserva → Actualizar estado del asiento → Publicar evento → Liberar lock
- **Validación técnica existente**:
  - Verificación de seatId y customerId no vacíos
  - Verificación de que el asiento existe
  - Verificación de que el asiento no esté reservado
  - Verificación de adquisición del lock

### 1.2 Servicio de Pedidos (Ordering)

**Caso de Uso: Agregar al Carrito**

- **Nombre técnico**: `AddToCartHandler`
- **Descripción**: Agrega un asiento reservado al carrito de compras del usuario
- **Reglas de negocio**:
  - La reserva debe estar válida antes de agregar al carrito
  - Un usuario puede tener un solo pedido en estado `draft`
  - Un asiento no puede estar dos veces en el mismo carrito
  - El precio total se calcula como suma de precios de items
- **Flujo**: Validar reserva → Buscar pedido draft existente → Agregar item o crear nuevo pedido → Calcular total
- **Validación técnica existente**:
  - Verificación de reserva activa
  - Verificación de que el asiento no esté ya en el carrito

**Caso de Uso: Finalizar Compra**

- **Nombre técnico**: `CheckoutOrderHandler`
- **Descripción**: Transiciona el pedido de `draft` a `pending` y genera la solicitud de pago
- **Reglas de negocio**:
  - El pedido debe estar en estado `draft`
  - Debe tener al menos un item
  - El monto debe ser mayor a cero
- **Flujo**: Validar estado draft → Validar items → Transicionar a pending → Publicar evento

**Caso de Uso: Obtener Pedido**

- **Nombre técnico**: `GetOrderHandler`
- **Descripción**: Recupera los detalles de un pedido por su ID

### 1.3 Servicio de Pagos (Payment)

**Caso de Uso: Procesar Pago**

- **Nombre técnico**: `ProcessPaymentHandler`
- **Descripción**: Procesa el pago de un pedido (simulado para MVP)
- **Reglas de negocio**:
  - Idempotencia: Si el pago ya fue exitoso, retornar el mismo resultado
  - Validar que el pedido exista y esté en estado correcto
  - Validar que la reserva aún esté activa
  - Simular resultado del pago (éxito o fracaso)
  - Publicar evento `payment-succeeded` o `payment-failed`
- **Flujo**: Verificar idempotencia → Validar pedido → Validar reserva → Crear registro → Simular cargo → Actualizar estado → Publicar evento
- **Validación técnica existente**:
  - Verificación de idempotencia
  - Verificación de estado del pedido
  - Verificación de reserva activa

### 1.4 Servicio de Cumplimiento (Fulfillment)

**Caso de Uso: Procesar Pago Exitoso**

- **Nombre técnico**: `ProcessPaymentSucceededHandler`
- **Descripción**: Genera el ticket cuando el pago fue exitoso
- **Reglas de negocio**:
  - Genera PDF del ticket con código QR
  - Publica evento `ticket-issued`
- **Flujo**: Recibir evento payment-succeeded → Generar ticket → Generar QR → Almacenar → Publicar evento

### 1.5 Servicio de Notificaciones (Notification)

**Caso de Uso: Enviar Notificación de Ticket**

- **Nombre técnico**: `SendTicketNotificationHandler`
- **Descripción**: Envía email con el ticket al cliente
- **Reglas de negocio**:
  - Idempotencia: No enviar notificación duplicada
  - Registrar la notificación en la base de datos
  - Manejar fallo en envío de email
- **Flujo**: Verificar si ya se envió → Construir email → Enviar → Persistir resultado

### 1.6 Servicio de Catálogo (Catalog)

**Casos de Uso**:

- `GetAllEventsHandler`: Listar todos los eventos
- `GetEventHandler`: Obtener detalles de un evento
- `GetEventSeatmapHandler`: Obtener mapa de asientos de un evento

### 1.7 Servicio de Identidad (Identity)

**Casos de Uso**:

- `CreateUserHandler`: Crear nuevo usuario
- `IssueTokenHandler`: Generar token JWT

---

## 2. Marco Conceptual: Verificación vs Validación

La diferenciación entre verificación y validación es fundamental para establecer qué tipo de pruebas corresponden a cada fase del desarrollo.

### 2.1 Definiciones Operativas

**Verificación** responde a la pregunta: ¿Estamos construyendo el producto correctamente? Se enfoca en la correcta implementación de las especificaciones técnicas. Busca defectos en el código y la lógica. Se realiza durante el desarrollo mediante pruebas automatizadas. Utiliza mocks y stubs para aislar el código bajo prueba. Es responsabilidad del equipo de desarrollo.

**Validación** responde a la pregunta: ¿Estamos construyendo el producto correcto? Se enfoca en que el sistema satisfaga las necesidades del negocio y del usuario. Busca defectos en los requisitos y expectativas. Se realiza mediante pruebas de integración, sistema y aceptación. Utiliza sistemas reales y datos de producción simulados. Es responsabilidad del equipo de QA y stakeholders del negocio.

### 2.2 Aplicación al Proyecto Speckit

Para el sistema Speckit Ticketing, la verificación y validación se distribuyen de la siguiente manera:

| Componente | Verificación | Validación |
|------------|--------------|------------|
| **Dominio** | Entidades: Reservación, Pedido, Pago, Ticket, Notificación | Estados válidos del ciclo de vida |
| **Aplicación** | Handlers con mocks de puertos | Flujos completos con servicios reales |
| **Infraestructura** | Redis locks, repositorios con BD en memoria | PostgreSQL real, Kafka real, Redis real |
| **Integración** | N/A | Flujo end-to-end: reserva → pago → ticket → email |

### 2.3 Principio Rector

Todo caso de uso debe pasar primero por verificación (tests unitarios con mocks) antes de pasar a validación (tests de integración con infraestructura real). Esta progresión asegura que los defectos se detecten tempranamente al menor costo posible.

---

## 3. Historias de Usuario Derivadas de lo Implementado

Las siguientes historias de usuario se derivan directamente del código existente y representan funcionalidad que ya está implementada o debe estarlo.

### 3.1 Flujo Principal de Compra

**HU-001: Reserva de Asiento**

Como cliente quiero reservar un asiento específico para un evento para asegurarme de que nadie más pueda reservarlo mientras completo mi compra.

Criterios de aceptación:

- Dado un evento con asientos disponibles, cuando el cliente selecciona un asiento y solicita reserva, entonces el sistema marca el asiento como reservado con TTL de 15 minutos y retorna ID de reserva
- Dado un asiento ya reservado, cuando otro cliente intenta reservar el mismo asiento, entonces el sistema rechaza la solicitud con mensaje de error apropiado
- Dado un intento de reserva concurrente, cuando dos clientes intentan reservar el mismo asiento al mismo tiempo, entonces solo una reserva se completa exitosamente

**HU-002: Carrito de Compras**

Como cliente quiero agregar un asiento reservado a mi carrito para poder continuar browsando o proceder al pago.

Criterios de aceptación:

- Dada una reserva válida, cuando el cliente agrega el asiento al carrito, entonces se crea un pedido en estado draft con el precio correcto
- Dada una reserva expirada, cuando el cliente intenta agregar al carrito, entonces el sistema rechaza con mensaje de error
- Dado un cliente con pedido draft existente, cuando agrega otro asiento, entonces se adiciona al pedido existente actualizando el total
- Dado un asiento ya en el carrito, cuando el cliente intenta agregar el mismo asiento, entonces el sistema rechaza con mensaje de error

**HU-003: Procesamiento de Pago**

Como cliente quiero pagar mi pedido para completar la compra del boleto.

Criterios de aceptación:

- Dado un pedido en estado pending con monto válido, cuando el cliente inicia el pago, entonces se procesa exitosamente y el pedido transiciona a paid
- Dado un pago exitoso, cuando se completa, entonces se publica evento payment-succeeded y el sistema genera el ticket
- Dado un pago rechazado, cuando el banco deniega, entonces se publica evento payment-failed y el pedido vuelve a estar disponible
- Dado un pago duplicado, cuando el cliente intenta pagar dos veces el mismo pedido, entonces el sistema retorna el resultado original sin cobrar de nuevo

**HU-004: Generación de Ticket**

Como cliente quiero recibir mi ticket después del pago exitoso para poder asistir al evento.

Criterios de aceptación:

- Dado un pago exitoso, cuando se confirma, entonces el sistema genera un ticket con código QR único
- Dado un ticket generado, cuando se emite, entonces el sistema publica evento ticket-issued
- Dado un cliente que recibe su ticket, cuando llega al evento, entonces puede escanear el QR para verificar autenticidad

**HU-005: Notificación por Email**

Como cliente quiero recibir un email con mi ticket después de la compra para tener un registro digital.

Criterios de aceptación:

- Dado un ticket emitido, cuando se genera, entonces el sistema envía email al cliente con los detalles del evento, asiento y precio
- Dada una notificación duplicada, cuando el sistema intenta enviar de nuevo, entonces no envía email duplicado
- Dado un fallo en el servidor de email, cuando la notificación falla, entonces el sistema registra la falla y permite reintento

### 3.2 Historias de Navegación

**HU-006: Exploración de Eventos**

Como visitante quiero ver los eventos disponibles para decidir cuál asistir.

Criterios de aceptación:

- Dado que existen eventos en el sistema, cuando el visitante consulta la lista, entonces retorna eventos con nombre, fecha, lugar y precio base
- Dado un evento específico, cuando el visitante consulta los detalles, entonces retorna información completa del evento

**HU-007: Mapa de Asientos**

Como cliente quiero ver la disponibilidad de asientos para seleccionar el que mejor me convenga.

Criterios de aceptación:

- Dado un evento, cuando el cliente consulta el seatmap, entonces retorna todos los asientos con su estado: available, reserved, sold
- Dado un asiento, cuando está disponible, entonces muestra el precio base

---

## 4. Estrategia de Testing por Caso de Uso

A continuación se presenta la estrategia de pruebas para cada caso de uso, distinguiendo entre verificación y validación.

### 4.1 Caso de Uso: Crear Reserva de Asiento

**VERIFICACIÓN (Unit Tests)**

| Escenario | Tipo de Prueba | Técnica | Descripción |
|-----------|----------------|---------|-------------|
| Happy path: asiento disponible | Unit test | Happy path | Reservar cuando el asiento está libre |
| Asiento ya reservado | Unit test | Exception test | Rechazar reserva de asiento ocupado |
| Asiento no existe | Unit test | Exception test | Manejar error de entidad no encontrada |
| Lock de Redis no disponible | Unit test | Exception test | Manejar fallo de adquisición de lock |
| SeatId vacío | Unit test | Exception test | Validar parámetros de entrada |
| CustomerId vacío | Unit test | Exception test | Validar parámetros de entrada |
| Reserva con seatId inválido | Unit test | Boundary test | Verificar validación de GUID |

Técnica de testing: Partitioning de estados (available, reserved, sold) + Boundary testing para TTL.

**VALIDACIÓN (Integration Tests)**

| Escenario | Tipo de Prueba | Técnica | Descripción |
|-----------|----------------|---------|-------------|
| Reserva exitosa con PostgreSQL real | Integration test | End-to-end integration | Reservar con BD real |
| Reserva concurrente: solo una exitos | Integration test | Concurrency test | Simular 2 clientes reservando mismo asiento |
| Evento Kafka publicado correctamente | Integration test | Event verification | Verificar que reservation-created llega a Kafka |
| TTL expira y asientos vuelve a available | Integration test | Time-based test | Verificar limpieza de reservas expiradas |

**Validaciones de desempeño**:

- Tiempo de respuesta promedio: <100ms
- Throughput: >100 reservas/segundo
- Concurrencia: 50 usuarios simultáneos reservando diferentes asientos

### 4.2 Caso de Uso: Agregar al Carrito

**VERIFICACIÓN (Unit Tests)**

| Escenario | Tipo de Prueba | Técnica | Descripción |
|-----------|----------------|---------|-------------|
| Crear nuevo pedido draft | Unit test | Happy path | Agregar primer item al carrito |
| Agregar a pedido existente | Unit test | State transition | Agregar segundo item al pedido |
| Reserva inválida | Unit test | Exception test | Rechazar reserva expirada |
| Asiento ya en carrito | Unit test | Duplicate test | Prevenir items duplicados |
| Reserva no encontrada | Unit test | Exception test | Manejar reserva inexistente |
| Error de base de datos | Unit test | Resilience test | Manejar fallo de BD |
| Usuario guest con token | Unit test | State test | Crear pedido para guest |
| Cálculo de total correcto | Unit test | Calculation test | Verificar suma de precios |

**VALIDACIÓN (Integration Tests)**

| Escenario | Tipo de Prueba | Descripción |
|-----------|----------------|-------------|
| Crear pedido con PostgreSQL real | Integration test | Persistencia real de pedido |
| Evento order-created a Kafka | Integration test | Verificar publicación de evento |
| Validación de reserva real con Inventory | Integration test | Verificar comunicación inter-servicio |

### 4.3 Caso de Uso: Procesar Pago

**VERIFICACIÓN (Unit Tests)**

| Escenario | Tipo de Prueba | Descripción |
|-----------|----------------|-------------|
| Pago exitoso | Unit test (existente) | Happy path con simulación |
| Pago fallido por fondos insuficientes | Unit test (existente) | Manejo de pago denegado |
| Validación de pedido falla | Unit test (existente) | Pedido no encontrado |
| Validación de reserva falla | Unit test (existente) | Reserva expirada |
| Idempotencia: segundo pago相同orden | Unit test (existente) | No cobrar dos veces |
| Fallo en publicación a Kafka | Unit test (existente) | Resiliencia ante fallo de evento |

**VALIDACIÓN (Integration Tests)**

| Escenario | Descripción |
|-----------|-------------|
| Pago con PostgreSQL real | Persistencia de Payment entity |
| Evento payment-succeeded publicado | Verificar topic de Kafka |
| Evento payment-failed publicado | Verificar topic de Kafka |
| Integración Ordering → Payment por Kafka | Flujo completo asíncrono |

### 4.4 Caso de Uso: Generación de Ticket

**VERIFICACIÓN (Unit Tests)**

| Escenario | Descripción |
|-----------|-------------|
| Generación exitosa de ticket | Crear ticket con datos correctos |
| Generación de QR code | Verificar que QR se genera con payload correcto |
| Generación de PDF | Verificar que PDF contiene información del ticket |

**VALIDACIÓN (Integration Tests)**

| Escenario | Descripción |
|-----------|-------------|
| Ticket persisted en PostgreSQL | Verificar persistencia real |
| Evento ticket-issued publicado | Verificar topic de Kafka |
| Flujo completo: payment → ticket | End-to-end del flujo |

### 4.5 Caso de Uso: Notificación de Ticket

**VERIFICACIÓN (Unit Tests)**

| Escenario | Descripción |
|-----------|-------------|
| Envío exitoso de email | Notificación enviada correctamente |
| Email ya enviado (idempotencia) | No enviar duplicado |
| Fallo en SMTP | Registrar error y retornar failure |
| Contenido del email correcto | Verificar subject, body, attachments |

**VALIDACIÓN (Integration Tests)**

| Escenario | Descripción |
|-----------|-------------|
| Notificación persistida en BD | Verificar EmailNotification entity |
| Email enviado con servidor SMTP real | Integration con servidor real |
| Evento ticket-issued consumido | Verificar consumo de Kafka |

### 4.6 Casos de Uso de Catálogo

**VERIFICACIÓN**

| Caso de Uso | Escenarios de Test |
|-------------|---------------------|
| GetAllEvents | Lista vacía, lista con eventos, paginación |
| GetEvent | Evento existe, evento no existe |
| GetEventSeatmap | Mapa con asientos disponibles, ocupados, vendidos |

**VALIDACIÓN**

- Consultas con PostgreSQL real
- Rendimiento: <200ms p95

---

## 5. Plan de Ejecución y Métricas

### 5.1 Fases de Implementación

**Fase 1: Auditoría y Consolidación (Semana 1)**

Actividades: Ejecutar suite de tests actual, analizar cobertura por caso de uso, identificar gaps, documentar estado actual.

Entregables: Reporte de cobertura por handler, lista de gaps identificados, plan de remediación.

**Fase 2: Contract Testing (Semana 2)**

Actividades: Implementar validación automática de contratos OpenAPI, verificar que implementaciones coincide con especificaciones.

Herramientas sugeridas: Swashbuckle para generación de specs, Approval tests para verificación.

**Fase 3: Integration Tests Completos (Semana 3)**

Actividades: Completar integration tests para flujos cross-service, implementar tests de concurrencia, implementar tests de TTL.

Herramientas: Testcontainers, Polly para retry policies.

**Fase 4: Performance Testing (Semana 4)**

Actividades: Configurar k6 para load testing, definir SLAs baselines, ejecutar pruebas de estrés.

Métricas meta: p95 latency <200ms, throughput >1000 requests/segundo.

### 5.2 Matriz de Trazabilidad

| Historia de Usuario | Criterio de Aceptación | Test IDs | Tipo |
|---------------------|------------------------|----------|------|
| HU-001 | Reserva exitosa | UNI-INV-001, INT-INV-001 | Unit, Integration |
| HU-001 | Reserva concurrente | INT-INV-002 | Integration |
| HU-002 | Crear pedido draft | UNI-ORD-001, INT-ORD-001 | Unit, Integration |
| HU-003 | Pago exitoso | UNI-PAY-001, INT-PAY-001 | Unit, Integration |
| HU-003 | Idempotencia | UNI-PAY-002 | Unit |
| HU-004 | Ticket generado | UNI-FUL-001, INT-FUL-001 | Unit, Integration |
| HU-005 | Email enviado | UNI-NOT-001, INT-NOT-001 | Unit, Integration |

### 5.3 Métricas de Calidad

**Cobertura de Código**

- Cobertura de líneas: 90% (mantener)
- Cobertura de ramas: 97% (mantener)
- Cobertura de métodos: 85% (mejorar desde 80%)

**Ejecución de Tests**

- Unit tests: <5 minutos
- Integration tests: <15 minutos
- E2E tests: <30 minutos

**Calidad de Código**

- Code smell: 0 críticos, <5 mayores
- Duplicación: <3%
- Complejidad ciclomática: <10 por método

---

## 6. Recomendaciones

Considerando el estado actual del proyecto (89.8% de cobertura, metodología semántica implementada, arquitectura hexagonal bien definida), se recomienda priorizar las siguientes acciones:

Primero, completar los integration tests faltantes para los flujos cross-service, especialmente la comunicación asíncrona por Kafka y la validación de TTL de reservas.

Segundo, implementar contract testing para validar que las APIs cumplen con los contratos OpenAPI definidos en /contracts/openapi/.

Tercero, establecer tests de carga con k6 para validar los tiempos de respuesta (<200ms p95) y el throughput requeridos.

Cuarto, documentar las historias de usuario con sus criterios de aceptación en un formato que permita trazabilidad directa con los tests.

El enfoque propuesto permite mantener el nivel de calidad actual mientras se completan las brechas identificadas, priorizando aquellas que mayor impacto tienen en la detección temprana de defectos.
