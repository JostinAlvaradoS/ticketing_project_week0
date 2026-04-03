# Feature Specification: Lista de Espera con Autoasignación

**Feature Branch**: `004-waitlist-autoassign`
**Created**: 2026-04-02
**Status**: Draft
**Input**: Lista de espera con autoasignación de asientos liberados: registro por email sin autenticación, cola FIFO por evento, asignación automática al expirar reserva, rotación por inacción con TTL de 30 minutos

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Registro en Lista de Espera (Priority: P1)

Un visitante que ve un evento agotado puede dejar su correo electrónico para ser tomado en cuenta automáticamente si un asiento se libera. No necesita cuenta registrada. Recibe una confirmación inmediata con su posición en la cola.

**Why this priority**: Sin esta historia no existe cola de espera. Es el punto de entrada de toda la feature y puede demostrarse de forma independiente.

**Independent Test**: Llamar `POST /api/v1/waitlist/join` con un email válido y un `eventId` con stock 0. Verificar que se retorna `201` con posición en cola y que la entrada queda persistida.

**Acceptance Scenarios**:

1. **Given** el evento tiene stock = 0, **When** un email válido llama `POST /waitlist/join` con ese `eventId`, **Then** el sistema responde `201 Created` con posición en la cola.
2. **Given** el evento tiene stock > 0, **When** cualquier email intenta unirse, **Then** el sistema responde `409 Conflict` con mensaje de tickets disponibles.
3. **Given** el email ya está en la cola del evento en estado `Pending` o `Assigned`, **When** intenta unirse de nuevo, **Then** el sistema responde `409 Conflict`.
4. **Given** se envía un email con formato inválido, **When** se llama `POST /waitlist/join`, **Then** el sistema responde `400 Bad Request`.

---

### User Story 2 — Asignación Automática al Liberarse un Asiento (Priority: P1)

Cuando una reserva expira y hay personas en la lista de espera del evento correspondiente, el sistema asigna el asiento automáticamente al primero de la cola (FIFO), genera una orden de compra como invitado usando el email, y le envía el enlace de pago con validez de 30 minutos. El asiento no se libera al inventario general mientras haya cola activa.

**Why this priority**: Es el núcleo del valor de la feature. Sin asignación automática la lista de espera no tiene utilidad.

**Independent Test**: Publicar un evento `reservation-expired` con un `eventId` que tenga entradas `Pending` en cola. Verificar que se crea una orden para el primer usuario, su entrada pasa a `Assigned` y recibe el correo.

**Acceptance Scenarios**:

1. **Given** hay al menos un `Pending` en la cola del evento, **When** se recibe `reservation-expired` para ese evento, **Then** se crea una orden de compra de invitado para el primer email en cola, su entrada pasa a `Assigned` y se envía el correo de pago con validez 30 min.
2. **Given** la cola del evento está vacía, **When** se recibe `reservation-expired`, **Then** el asiento se libera al inventario general y no se genera orden.
3. **Given** el usuario asignado completa el pago, **When** se recibe `payment-succeeded` para esa orden, **Then** la entrada pasa a `Completed`.

---

### User Story 3 — Rotación por Inacción (Priority: P2)

Si el usuario asignado no paga en 30 minutos, el sistema detecta el vencimiento, marca su turno como `Expired`, le notifica, y reasigna el asiento directamente al siguiente `Pending` en cola sin liberarlo al inventario general. Si la cola está vacía, libera el asiento.

**Why this priority**: Cierra el ciclo de la feature garantizando que ningún asiento quede bloqueado por inacción.

**Independent Test**: Crear una entrada `Assigned` con `AssignedAt` hace 31 minutos. Ejecutar el worker de expiración. Verificar la rotación o liberación según estado de la cola.

**Acceptance Scenarios**:

1. **Given** el turno `Assigned` lleva más de 30 min sin pago, **And** hay un `Pending` siguiente, **When** el worker ejecuta, **Then** la entrada pasa a `Expired`, el asiento se reasigna sin liberarse al pool, y el siguiente recibe el correo de pago.
2. **Given** el turno `Assigned` lleva más de 30 min sin pago, **And** la cola está vacía, **When** el worker ejecuta, **Then** la entrada pasa a `Expired` y el asiento se libera al inventario general.
3. **Given** el usuario pagó antes del vencimiento (entrada en `Completed`), **When** el worker ejecuta, **Then** no se realiza ninguna acción sobre esa entrada.

---

### Edge Cases

- ¿Qué pasa si llegan dos eventos `reservation-expired` para el mismo asiento? El segundo es idempotente: si ya hay una entrada `Assigned` activa para ese asiento, no se crea una segunda orden.
- ¿Qué pasa si la notificación por correo falla? La asignación persiste; la notificación se registra como fallida en la auditoría pero no revierte el estado.
- ¿Puede un email re-unirse a la lista de un mismo evento después de que su turno expiró? Sí — el estado `Expired` no bloquea nuevos registros `Pending`.
- ¿Qué ocurre si el Catalog Service no responde al validar stock? El join se rechaza con `503 Service Unavailable`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema DEBE permitir el registro en la lista de espera con solo un email y un `eventId`, sin autenticación.
- **FR-002**: El sistema DEBE rechazar el registro si el evento tiene asientos disponibles (stock > 0 en Catalog).
- **FR-003**: El sistema DEBE rechazar registros duplicados activos: mismo `email + eventId` en estado `Pending` o `Assigned`.
- **FR-004**: La cola DEBE ser FIFO estricto ordenado por `RegisteredAt` ascendente.
- **FR-005**: Al recibir `reservation-expired` para un asiento de un evento con cola activa, el sistema DEBE asignar sin liberar el asiento al inventario general.
- **FR-006**: Al asignar un turno, el sistema DEBE crear una orden de compra de invitado (usando el email como identificador) y enviar el enlace de pago por correo.
- **FR-007**: El turno asignado DEBE expirar automáticamente a los 30 minutos si no hay pago registrado.
- **FR-008**: Al expirar por inacción: reasignar al siguiente `Pending`, o liberar el asiento si la cola está vacía.
- **FR-009**: Todos los consumidores de eventos DEBEN ser idempotentes.
- **FR-010**: El sistema DEBE notificar al usuario en: registro confirmado, asignación recibida, y turno expirado.

### Key Entities

- **WaitlistEntry**: Registro de un email en la cola de espera de un evento. Ciclo de vida: `Pending → Assigned → Completed / Expired`. Almacena posición FIFO, referencia al `OrderId` generado y `AssignedAt`.
- **Event** (referencia externa): Agrupador de la cola, identificado por `EventId`. Propiedad del Catalog Service.
- **Seat** (referencia externa): Recurso que se asigna. Propiedad del Inventory Service. La Waitlist no gestiona su bloqueo directamente.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El tiempo entre la expiración de una reserva y la recepción del correo de pago por el primer usuario en cola es menor a 5 segundos en P95.
- **SC-002**: Cero asientos quedan bloqueados sin usuario `Assigned` activo mientras existan entradas `Pending` en la cola.
- **SC-003**: El 100% de los eventos `reservation-expired` con cola activa genera exactamente una orden de compra (sin duplicados).
- **SC-004**: El 100% de los turnos vencidos son procesados en los 2 minutos posteriores al vencimiento.
- **SC-005**: La tasa de registro exitoso para emails válidos con stock = 0 es del 100% bajo carga normal.

## Assumptions

- La compra y el registro en lista de espera son flujos de invitado; el email se usa como `GuestToken`.
- La disponibilidad de asientos se consulta al Catalog Service en el momento del registro; si no responde, el join se rechaza con `503`.
- El TTL de 30 minutos de la lista de espera es exclusivamente gestionado por el Waitlist Service (no por Ordering).
- Un usuario cuyo turno expiró puede volver a registrarse en la lista del mismo evento.
- La notificación incluye el `OrderId` para que el frontend construya la URL de pago; no se genera URL directamente en el backend.
