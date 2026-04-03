# Documentación de nueva feature a implementar: Waitlist_autoassign

| Campo | Detalle |
|---|---|
| **Proyecto** | ticketing_project_week0 |
| **Responsable** | Jostin Alvarado |
| **Entrega** | 27/03/2026 |

---

## Índice

1. [Contexto del Problema](#1-contexto-del-problema)
   - 1.1 Situación actual
   - 1.2 Motivación de negocio
2. [Análisis del Dominio](#2-análisis-del-dominio)
   - 2.1 Entidades
   - 2.2 Reglas de negocio
3. [Definición de la Feature](#3-definición-de-la-feature)
   - 3.1 Historias de Usuario
   - 3.2 Criterios de aceptación
4. [Casos de Uso](#4-casos-de-uso)
   - 4.1 CU-01: Registrar usuario en lista de espera
   - 4.2 CU-02: Asignar Turno Automático
   - 4.3 CU-03: Expirar Turno por Inacción
5. [Requerimientos funcionales y no funcionales](#5-requerimientos-funcionales-y-no-funcionales)
   - 5.1 Funcionales
   - 5.2 No Funcionales
6. [Diseño Arquitectónico](#6-diseño-arquitectónico)
   - 6.1 Tipo
   - 6.2 Justificación
   - 6.3 Componentes involucrados en la feature
   - 6.4 Vista lógica
   - 6.5 Vista de Procesos
   - 6.6 Vista de Desarrollo
7. [Decisiones arquitectónicas](#7-decisiones-arquitectónicas)
   - 7.1 ADR-01: PostgreSQL como almacén de la Cola de Espera FIFO
   - 7.2 ADR-02: Generación automática de Órdenes de Compra desde el Servicio de Lista de Espera
   - 7.3 ADR-03: El Asiento permanece bloqueado durante la Rotación de Asignación
8. [Riesgos e impactos en el sistema](#8-riesgos-e-impactos-en-el-sistema)
   - 8.1 Riesgos
   - 8.2 Impacto en servicios existentes
9. [Plan de pruebas](#9-plan-de-pruebas)
10. [Estimación de esfuerzo](#10-estimación-de-esfuerzo)

---

## 1. Contexto del Problema

### 1.1 Situación actual

| Qué hace hoy el sistema | Limitaciones actuales | Problema a resolver |
|---|---|---|
| El sistema gestiona compras de tickets mediante un flujo lineal. Se inicia una orden (Ordering), se reserva stock con un TTL de 15 minutos (Inventory) y se procesa el pago de forma síncrona o semi-aislada (Payment). | Cuando una reserva expira (por el TTL de 15 min) o un pago falla, el ticket se libera al pool general. Esto crea una "carrera" desordenada donde el usuario que tenga mejor latencia o esté refrescando la página en ese milisegundo exacto se lleva el ticket, ignorando a quienes intentaron comprar primero y fallaron por falta de stock. | La falta de equidad y eficiencia en la recuperación de inventario. No existe un mecanismo para capturar la intención de compra de los usuarios cuando el evento está lleno, ni un proceso ordenado para asignar tickets liberados a los usuarios más interesados. |

### 1.2 Motivación de negocio

| Importancia de la feature | Impacto esperado | Consecuencia de no implementarla |
|---|---|---|
| Optimiza la tasa de conversión al asegurar que cada ticket liberado sea ofrecido inmediatamente a un cliente con intención de compra real. Mejora radicalmente la percepción de justicia del sistema. | Reducción del tiempo en que un ticket liberado permanece "disponible" en el sistema antes de ser re-comprado. Incremento en la satisfacción del usuario al eliminar la necesidad de refrescar constantemente la página. | Pérdida de ventas potenciales si los usuarios abandonan el sitio al ver el estado "Sold Out". Experiencia de usuario frustrante y caótica durante la liberación de stock residual en eventos de alta demanda. |

---

## 2. Análisis del Dominio

### 2.1 Entidades

- **WaitlistEntry:** Representa a un usuario registrado en la cola de espera para un evento específico. Contiene su posición, estado y referencia a la orden generada.
- **Event:** Evento con stock agotado que origina la lista de espera. Es la clave de agrupación de la cola.
- **Seat:** Asiento concreto dentro de un evento. Es el recurso que se bloquea, reasigna o libera.
- **Order:** Orden de compra generada automáticamente para el usuario asignado desde la lista de espera.

### 2.2 Reglas de negocio

- **RN-01:** Un usuario solo puede estar registrado una vez por evento en la lista de espera (unique constraint sobre `Email + EventId`).
- **RN-02:** La cola es FIFO estricto: el primero en registrarse es el primero en ser asignado.
- **RN-03:** Un usuario asignado dispone de exactamente 30 minutos para completar el pago.
- **RN-04:** Si el usuario no paga en 30 min, su entrada pasa a estado `Expirado` y el siguiente en cola es notificado.
- **RN-05:** El asiento permanece bloqueado en Inventory hasta confirmar si hay siguiente en cola, evitando race conditions.
- **RN-06:** No se puede unir a la lista si el evento tiene stock disponible.

---

## 3. Definición de la Feature

### 3.1 Historias de Usuario

| HU-01: Registro Voluntario en Lista de Espera | HU-02: Asignación Exclusiva y Generación de Cobro | HU-03: Rotación de Asignación por Inacción |
|---|---|---|
| **Como** Usuario que visualiza un evento con todas las entradas agotadas, **Quiero** suscribirme a una lista de espera para el evento específico, **Para** ser tomado en cuenta automáticamente si una entrada se libera en el futuro. | **Como** Usuario en lista de espera, **Quiero** que cuando un ticket se libere, el sistema me asigne la reserva automáticamente y me genere una solicitud de pago lista para completar, **Para** asegurar mi lugar sin tener que competir nuevamente por el inventario y recibir la notificación directa en mi correo. | **Como** Dueño de un evento, **Quiero** que se detecte cuando un usuario asignado no completa el pago en 30 minutos y reasignar el asiento al siguiente en la cola de espera sin liberar el asiento al inventario disponible, **Para** garantizar que ningún asiento quede sin convertirse en venta y priorizar los usuarios que pueden ser una venta segura. |

### 3.2 Criterios de aceptación

#### Sistema de Lista de Espera Inteligente

| Escenario | Gherkin |
|---|---|
| Registro exitoso en lista de espera | **Dado** que el evento "Concierto Rock 2026" tiene stock = 0<br>**Cuando** el usuario "jostin@example.com" se registra en la waitlist con su correo<br>**Entonces** el sistema responde `201 Created` |
| Intento de registro con tickets disponibles | **Dado** que el evento "Concierto Rock 2026" tiene stock > 0<br>**Cuando** el usuario "jostin@example.com" intenta unirse a la lista de espera<br>**Entonces** el sistema responde "Hay tickets disponibles, realiza la compra directamente". |
| Registro duplicado en la misma lista | **Dado** que "jostin@example.com" ya está registrado en la lista del evento "Concierto Rock 2026"<br>**Cuando** el mismo correo intenta registrarse nuevamente para el mismo evento<br>**Entonces** el sistema responde "Ya estás en la lista de espera para este evento" |
| Asignación automática al expirar una reserva | **Dado** que "jostin@example.com" es el primero en la lista de espera del evento "Concierto Rock 2026"<br>**Cuando** el tiempo de pago inicial caduca<br>**Entonces** el sistema crea una orden automática para "jostin@example.com"<br>**Y** actualiza el estado de la entrada a `Asignado`<br>**Y** envía un correo con el enlace de pago con validez de 30 minutos |
| Liberación por inacción con siguiente en cola | **Dado** que "jostin@example.com" fue asignado y no pagó en 30 minutos<br>**Y** "segundo@example.com" es el siguiente en la lista<br>**Cuando** el sistema detecta este hecho<br>**Entonces** el sistema marca la entrada de "jostin@example.com" como `Expirado`<br>**Y** reasigna el asiento directamente a "segundo@example.com" sin liberarlo al pool general<br>**Y** envía correo de pago a "segundo@example.com" con validez de 30 minutos |
| Liberación por inacción con cola vacía | **Dado** que "jostin@example.com" fue asignado y no pagó en 30 minutos<br>**Y** no hay más usuarios en la lista de espera del evento<br>**Cuando** el sistema detecta este hecho<br>**Entonces** el sistema cancela la orden y libera el asiento al pool general |

---

## 4. Casos de Uso

### 4.1 CU-01: Registrar usuario en lista de espera

```
Usuario ──► [Consultar disponibilidad de tickets] ──EXTEND──► [Unirse a lista de espera]
```

> El usuario consulta la disponibilidad. Si el stock es 0, se extiende el caso de uso permitiéndole unirse a la lista de espera.

### 4.2 CU-02: Asignar Turno Automático

```
El sistema ──► [Asignar turno automático]
                    ├──Include──► [Reservar asiento]
                    ├──Include──► [Generar orden]
                    └──Include──► [Generar pago]
```

> El sistema, al detectar un ticket liberado, incluye obligatoriamente los tres sub-casos: reservar el asiento, generar la orden de compra y generar el pago.

### 4.3 CU-03: Expirar Turno por Inacción

```
El sistema ──► [Valida tiempo de expiración]
                    ├──Include──► [Liberar asiento]
                    ├──Include──► [Finalizar orden]
                    ├──Include──► [Cancelar pago]
                    └──Include──► [Asignar turno automático a siguiente en lista]
```

> Cuando el TTL de 30 minutos vence sin pago registrado, el sistema valida la expiración e incluye todos los pasos de limpieza y reasignación.

---

## 5. Requerimientos funcionales y no funcionales

### 5.1 Funcionales

- **RF-01:** Persistencia de cola FIFO por cada `EventId`.
- **RF-02:** Consumo reactivo de eventos de expiración de tickets.
- **RF-03:** Generación programática de órdenes de compra.

### 5.2 No Funcionales

- **Escalabilidad:** Soportar ráfagas de 10k usuarios/min.
- **Performance:** Proceso de asignación < 2 segundos.
- **Seguridad:** Validación anti-spam y cifrado de correos en repositorio.

---

## 6. Diseño Arquitectónico

### 6.1 Tipo

Microservicios orientados a eventos con mensajería asíncrona con Kafka.

### 6.2 Justificación

Permite agregar servicios ya que todo está desacoplado. En este caso es beneficioso para la nueva feature ya que se puede agregar sin modificar contratos internos de los demás servicios.

### 6.3 Componentes involucrados en la feature

| Componente | Responsabilidad |
|---|---|
| Servicio de Lista de Espera (Waitlist Service) | Gestionar la Cola de Espera FIFO, consumir eventos de Kafka, coordinar la asignación automática y la rotación de asignación |
| Servicio de Órdenes (Ordering Service) | Crear, reasignar y cancelar órdenes de compra; gestionar TTL de pago y publicar `order-payment-timeout` |
| Servicio de Inventario (Inventory Service) | Bloquear, reasignar y liberar asientos; publicar `reservation-expired` al vencer el TTL de reserva |
| Servicio de Notificaciones (Notification Service) | Enviar correos de confirmación de registro en cola de espera, asignación de asiento y expiración por inacción |
| Servicio de Catálogo (Catalog Service) | Proveer consulta de disponibilidad de asientos por `EventId` |

### 6.4 Vista lógica

```
┌─────────────────────────────────────────────────────────────────────┐
│                          WaitlistManager                            │
│  +JoinWaitlist(email: string, eventId: Guid) : Task                 │
│  +ProcessTicketRelease(eventId: Guid, seatId: Guid) : Task          │
│  +ProcessInactionExpiry(orderId: Guid, seatId: Guid,                │
│                          eventId: Guid) : Task                      │
└──────────────┬──────────────────────────────────┬───────────────────┘
          manages                               uses
               │                                  │
               ▼                                  ▼
┌──────────────────────────┐      ┌──────────────────────────────────┐
│       WaitlistEntry      │      │  «interface» IWaitlistRepository  │
│  +Guid Id                │      │  +Add(entry: WaitlistEntry): Task │
│  +string Email           │      │  +GetNextInQueue(eventId: Guid)  │
│  +Guid EventId           │      │    : Task<WaitlistEntry>         │
│  +Guid? OrderId          │      │  +UpdateStatus(id: Guid,         │
│  +DateTime RegisteredAt  │      │    status: WaitlistStatus): Task  │
│  +DateTime? AssignedAt   │      └──────────────────────────────────┘
│  +WaitlistStatus Status  │
│  +int Priority           │
└──────────────┬───────────┘
               │
               ▼
┌──────────────────────────┐
│  «enumeration»           │
│  WaitlistStatus          │
│  ─────────────────────   │
│  Pending                 │
│  Assigned                │
│  Expired                 │
│  Completed               │
└──────────────────────────┘
```

### 6.5 Vista de Procesos

#### Flujo A: Registro de Usuario en Lista de Espera

```
Usuario         Frontend        Catalog Service    Waitlist Service    PostgreSQL (Queue)
   │               │                  │                  │                    │
   │─Ver Evento────►│                  │                  │                    │
   │  (Sold Out)    │                  │                  │                    │
   │               │──Obtener──────►  │                  │                    │
   │               │  Disponibilidad  │                  │                    │
   │               │◄──Stock: 0 ──────│                  │                    │
   │─Ingresar──────►│                  │                  │                    │
   │ Email+"Unirse" │──POST /api/v1/waitlist/join────────►│                    │
   │               │                  │──Verificar Stock──►                    │
   │               │                  │    (EventId)      │                    │
   │               │                  │                   │                    │
   │         alt [Stock sigue en 0]   │                   │                    │
   │               │                  │──Validar formato Email (self-loop)     │
   │               │                  │                   │──INSERT ──────────►│
   │               │                  │                   │  WaitlistEntry     │
   │               │                  │                   │◄──Éxito (Posición X)│
   │               │◄──────────────201 Created (Registrado)                   │
   │◄──Confirmación: Estás en posición X                  │                    │
   │               │                  │                   │                    │
   │         alt [Stock disponible]   │                   │                    │
   │               │◄──────────────409 Conflict (Tickets disponibles)         │
   │◄──Hay tickets disponibles, compra directamente       │                    │
```

#### Flujo B: Asignación Automática por Liberación de Asiento

```
Kafka           Waitlist Service    Ordering Service    Inventory Service   Notification Service   PostgreSQL (Cola)
  │                   │                   │                   │                    │                     │
  │─reservation───────►                   │                   │                    │                     │
  │ expired           │                   │                   │                    │                     │
  │ (SeatId, EventId) │──Consultar siguiente entrada con Estado=Pendiente (FIFO)──────────────────────►│
  │                   │◄──SiguienteEntrada(Correo, EventId) o vacío────────────────────────────────────│
  │                   │                   │                   │                    │                     │
  │             alt [Hay siguiente en cola]                   │                    │                     │
  │                   │──Crear Orden Automática───────────────►                    │                     │
  │                   │  (Correo, EventId, SeatId)            │                    │                     │
  │                   │                   │──Bloquear Asiento Exclusivamente──────►                     │
  │                   │                   │  (SeatId)         │                    │                     │
  │                   │                   │◄──Asiento Bloqueado                   │                     │
  │                   │◄──Orden Creada (OrderId)──────────────│                    │                     │
  │                   │──Actualizar EstadoEntrada = Asignado WHERE Id=X───────────────────────────────►│
  │                   │──Enviar Enlace de Pago (OrderId, validez 30 min)─────────►                     │
  │                   │                   │                   │                    │                     │
  │             alt [Cola vacía]          │                   │                    │                     │
  │ [Asiento ya liberado por Inventory al expirar el TTL]     │                    │                     │
```

#### Flujo C: Rotación de Asignación por Inacción

```
Ordering Service    Kafka           Waitlist Service    Inventory Service   Notification Service   PostgreSQL (Cola)
       │              │                   │                   │                    │                     │
[TTL 30 min vence]    │                   │                   │                    │                     │
       │─Publicar order-payment-timeout──►                    │                    │                     │
       │  (IdOrden, IdAsiento, IdEvento)  │                   │                    │                     │
[Asiento permanece    │                   │                   │                    │                     │
 bloqueado hasta      │──Consumir order-payment-timeout───────►                    │                     │
 conocer si hay       │                   │──Actualizar EstadoEntrada = Expirado WHERE IdOrden=X────────►│
 siguiente en cola]   │                   │──Notificar expiración (Correo)──────────────────────────────►│
       │              │                   │──Consultar siguiente entrada con Estado=Pendiente (FIFO)────►│
       │              │                   │◄──SiguienteEntrada(Correo, IdEvento) o vacío────────────────│
       │              │                   │                   │                    │                     │
       │        alt [Hay siguiente en cola]                   │                    │                     │
       │◄──Reasignar Orden (IdAsiento, SiguienteCorreo, IdEvento)                 │                     │
       │◄──Reasignar Asiento (IdAsiento, SiguienteCorreo)────►                    │                     │
       │              │◄──Asiento Reasignado──────────────────│                    │                     │
       │◄──Nueva Orden Creada (NuevoIdOrden)───────────────────                   │                     │
       │              │──Actualizar EstadoEntrada = Asignado WHERE Id=SiguienteId────────────────────►  │
       │              │──Enviar Enlace de Pago (NuevoIdOrden, validez 30 min)─────►                     │
       │        alt [Cola vacía]          │                   │                    │                     │
       │◄──Cancelar Orden (IdOrden)────────                   │                    │                     │
       │              │──Liberar Asiento al Pool General (IdAsiento)──────────────►                     │
       │              │◄──Asiento Liberado────────────────────│                    │                     │
```

### 6.6 Vista de Desarrollo

```
┌──────────────────────────────────────────┐
│       Waitlist.Service (Microservicio)   │
│                                          │
│  ┌──────────────────────────────────┐    │
│  │     API Layer (Controllers)      │    │
│  └────────────────┬─────────────────┘    │
│                   │                      │
│  ┌────────────────▼─────────────────┐    │
│  │  Application Layer               │    │
│  │  (WaitlistManager)               │    │
│  └──────┬──────────────┬────────────┘    │
│         │              │                 │
│  ┌──────▼──────────────▼────────────┐    │
│  │  Infrastructure Layer            │    │
│  │  (EF Core, KafkaConsumer)        │    │
│  └──────┬──────────────┬────────────┘    │
│         │              │                 │
│  ┌──────▼──────────────┘                 │
│  │  Domain Layer                         │
│  │  (WaitlistEntry, WaitlistStatus)      │
│  └──────────────────┬────────────────────┘
│                     │
└─────────────────────┼────────────────────┘
                      │
         ┌────────────▼────────────────┐
         │    Dependencias Externas    │
         │  ┌──────────┐ ┌──────────┐ │
         │  │PostgreSQL│ │  Kafka   │ │
         │  │          │►│  Broker  │ │
         │  └──────────┘ └──────────┘ │
         └─────────────────────────────┘
```

---

## 7. Decisiones arquitectónicas

### 7.1 ADR-01: PostgreSQL como almacén de la Cola de Espera FIFO

| Contexto | Decisión | Alternativas descartadas | Consecuencias |
|---|---|---|---|
| La Cola de Espera necesita persistencia duradera, ordenamiento garantizado y capacidad de auditoría. Se evaluaron Redis (Sorted Sets) y PostgreSQL. | Usar PostgreSQL con índice compuesto `(EventId, Status, Priority, RegisteredAt)`. | **Redis** — ofrece mayor velocidad pero no garantiza durabilidad ante reinicios sin configuración AOF/RDB adicional, y su modelo de datos dificulta las consultas de auditoría y cambios de estado transaccionales. | Latencia de consulta ligeramente mayor que Redis en memoria, compensada por la garantía ACID y la capacidad de hacer queries de auditoría sin infraestructura adicional. |

### 7.2 ADR-02: Generación automática de Órdenes de Compra desde el Servicio de Lista de Espera

| Contexto | Decisión | Alternativas descartadas | Consecuencias |
|---|---|---|---|
| Al liberar un Asiento, se debe generar una Orden de Compra para el siguiente en la Cola de Espera. La alternativa era notificar al usuario y esperar que él iniciara la compra. | El Servicio de Lista de Espera crea la Orden de Compra automáticamente llamando al Servicio de Órdenes, enviando solo el Enlace de Pago al usuario. | Se evaluó un trigger SQL directo; sin embargo, se optó por mantener el enfoque de Kafka y reutilizar endpoints ya existentes, evitando lógica relativamente duplicada. | Mayor acoplamiento entre el Servicio de Lista de Espera y el Servicio de Órdenes, mitigado mediante el contrato de evento Kafka y el endpoint interno de reasignación. |

### 7.3 ADR-03: El Asiento permanece bloqueado durante la Rotación de Asignación

| Contexto | Decisión | Alternativas descartadas | Consecuencias |
|---|---|---|---|
| Cuando un usuario asignado no paga en 30 min, se debe decidir si liberar el asiento al inventario disponible y luego re-bloquearlo para el siguiente, o transferirlo directamente. | El asiento permanece bloqueado en el Servicio de Inventario hasta que el Servicio de Lista de Espera confirme si hay o no siguiente en la cola. Solo se libera al inventario disponible si la cola está vacía. | Liberar primero y re-bloquear después introduce una ventana de condición de carrera donde un usuario externo podría tomar el asiento antes de la rotación de asignación. | Requiere una operación nueva en el Servicio de Inventario (`ReasignarAsiento`) que transfiere el bloqueo de forma atómica, aumentando ligeramente la complejidad de ese servicio. |

---

## 8. Riesgos e impactos en el sistema

### 8.1 Riesgos

| Riesgo | Componente afectado | Probabilidad | Impacto | Mitigación |
|---|---|---|---|---|
| Spam de bots llenando la Cola de espera | Servicio de lista de espera | Alta | Alto | Rate limiting en el servicio + Cloudflare |
| Condición de carrera al asignar el mismo asiento concurrentemente | Servicio de Lista de Espera + Servicio de Inventario | Media | Alto | Se soluciona manteniendo bloqueado el asiento mientras se hace la transición de dueño usando la waitlist. |
| Usuario asignado no recibe el enlace de pago | Servicio de notificaciones | Media | Alto | Reintentos + registro de auditoría |

### 8.2 Impacto en servicios existentes

| Servicio | Afección |
|---|---|
| Servicio de lista de espera (nuevo) | Microservicio independiente. Consume `reservation-expired` y `order-payment-timeout` de Kafka. |
| Servicio de ordering | Requiere un nuevo endpoint para reasignar orden de compra. |
| Servicio de inventario | Requiere un nuevo endpoint para reasignar asiento. |
| Servicio de catálogo | El servicio de lista de espera le hace una consulta para validar que efectivamente el stock es 0. |
| Servicio de notificaciones | Nuevo endpoint para registrar los avisos de registro en cola de espera y links de pago. |

---

## 9. Plan de pruebas

Cada caso incluye su estrategia de prueba para trazabilidad directa entre el tipo de validación y el escenario cubierto. Las pruebas de Regresión cubren los servicios existentes modificados por esta feature y verifican que su comportamiento previo no fue alterado.

| ID | Estrategia | Escenario | Entrada | Resultado esperado |
|---|---|---|---|---|
| TU-01 | Unitaria | `GetNextInQueue` retorna la entrada con menor `RegisteredAt` en Estado Pendiente | Cola con 3 entradas insertadas en distinto orden | Retorna la entrada con `registeredAt` más antiguo |
| TU-02 | Unitaria | Validación de email: formato válido | `"email@email.com"` | Pasa validación |
| TU-03 | Unitaria | Validación de email: formato inválido | `"hola"` | Lanza error de validación |
| TU-04 | Unitaria | Transición de estado: `Pendiente → Asignado` | Entrada en Estado Pendiente + llamada a `ProcessTicketRelease` | Estado = `Asignado`, `AssignedAt` registrado |
| TU-05 | Unitaria | Transición de estado: `Asignado → Expirado` | Entrada en Estado Asignado + llamada a `ProcessInactionExpiry` | Estado = `Expirado` |
| TI-01 | Integración | Registro exitoso en Cola de Espera (Flujo A) | Email válido, `EventId` con `stock=0` | `201 Created`, posición X en Cola de Espera, entrada persistida en DB |
| TI-02 | Integración | Registro con asientos disponibles en Inventario | Email válido, `EventId` con `stock>0` | `409 Conflict`, redirigir a compra directa |
| TI-03 | Integración | Entrada duplicada en la misma Cola de Espera | Email ya registrado para el mismo `EventId` | `409 Conflict`, "ya estás en la lista de espera" |
| TI-04 | Integración | Asignación automática con cola de espera activa (Flujo B) | `reservation-expired`, 1 Entrada Pendiente | Orden de Compra creada, `Estado=Asignado`, Enlace de Pago enviado |
| TI-05 | Integración | `reservation-expired` con cola de espera vacía | `reservation-expired`, cola vacía | Sin asignación; asiento liberado al Inventario Disponible por el Servicio de Inventario |
| TI-06 | Integración | Rotación de asignación con siguiente en Cola de Espera (Flujo C) | `order-payment-timeout`, ≥1 Entrada Pendiente | Asiento reasignado sin liberarse, nueva Orden de Compra creada, Enlace de Pago enviado |
| TI-07 | Integración | Rotación de asignación con cola de espera vacía | `order-payment-timeout`, cola vacía | Orden de Compra cancelada, asiento liberado al Inventario Disponible |
| TR-01 | Regresión | Servicio de órdenes: creación de Orden de Compra normal no afectada por el nuevo endpoint interno | Flujo estándar de compra directa sin vínculo a Lista de Espera | Orden de Compra creada correctamente; no se publica `order-payment-timeout` |
| TR-02 | Regresión | Servicio de Inventario: bloqueo y liberación estándar de asiento no activan rotación de asignación | Reserva normal con TTL de 15 min + expiración | Asiento liberado al Inventario Disponible sin disparar flujo de Lista de Espera |

---

## 10. Estimación de esfuerzo

Se aplican tanto técnica de tallas de camiseta como cálculo de Fibonacci.

| HU | Título | Complejidad | Incertidumbre | Esfuerzo | Talla | Puntos |
|---|---|---|---|---|---|---|
| HU-01 | Registro voluntario en Lista de espera | Media | Baja | Medio | M | 5 |
| HU-02 | Asignación automática y generación de orden de compra | Alta | Media | Alto | L | 13 |
| HU-03 | Rotación de asignación por inacción | Muy Alta | Alta | Muy Alto | XL | 21 |
| | | | | | **Total:** | **39** |