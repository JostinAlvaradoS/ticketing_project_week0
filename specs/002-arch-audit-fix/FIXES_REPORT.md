# Reporte de Correcciones Arquitectónicas — `002-arch-audit-fix`

**Fecha:** 2026-04-02
**Rama:** `feature/Jostin/waitlist_autoassign`
**Servicios afectados:** Inventory, Identity, Fulfillment, Ordering, Payment
**Tests finales:** 48 passing (26 Domain + 22 Application), 0 failures, 0 warnings

---

## 1. Contexto

Se realizó una auditoría de arquitectura sobre 7 microservicios (.NET 8) del proyecto de ticketing, validando cumplimiento con **Clean Architecture**, **Arquitectura Hexagonal (Ports & Adapters)** y **Clean Code**. Los hallazgos se formalizaron como una especificación SpecKit (`spec.md` → `plan.md` → `tasks.md`) antes de ejecutar cualquier cambio. Esto garantizó que cada corrección tuviera justificación trazable y criterio de aceptación verificable.

---

## 2. Fixes aplicados

### 2.1 Puertos en la capa incorrecta (Hexagonal Architecture)

**Hallazgo:** Las interfaces de puerto (`IRedisLock`, `IKafkaProducer`) vivían en `Inventory.Domain.Ports`; las de Identity (`IPasswordHasher`, `ITokenGenerator`, `IUserRepository`) en `Identity.Domain.Ports`; y `IEventPublisher` en `Fulfillment.Infrastructure.Events`.

**Problema:** En Hexagonal Architecture, los puertos son contratos que el núcleo de la aplicación define para comunicarse con el exterior. Pertenecen a la capa **Application** (que los usa), no a Domain (que no debe saber de infraestructura) ni a Infrastructure (que los implementa). Tener puertos en Domain crea dependencias hacia conceptos de mensajería/locking que son detalles de infraestructura. Tenerlos en Infrastructure invierte la dirección de la dependencia.

**Fix aplicado:**

| Servicio | Desde | Hacia |
|---|---|---|
| Inventory | `Inventory.Domain.Ports.IRedisLock` | `Inventory.Application.Ports.IRedisLock` |
| Inventory | `Inventory.Domain.Ports.IKafkaProducer` | `Inventory.Application.Ports.IKafkaProducer` |
| Inventory | (nuevo) | `Inventory.Application.Ports.ISeatRepository` |
| Inventory | (nuevo) | `Inventory.Application.Ports.IReservationRepository` |
| Identity | `Identity.Domain.Ports.*` (3 interfaces) | `Identity.Application.Ports.*` |
| Fulfillment | `Fulfillment.Infrastructure.Events.IEventPublisher` | `Fulfillment.Application.Ports.IEventPublisher` |

Se eliminaron los archivos originales. Se actualizaron los `csproj` para que `Infrastructure` referencie a `Application` (y no al revés). Todos los `using` en handlers e implementaciones se actualizaron en consecuencia.

---

### 2.2 Handler de Application accedía directamente a DbContext (violación de Clean Architecture)

**Hallazgo:** `CreateReservationCommandHandler` en `Inventory.Application` inyectaba `InventoryDbContext` (de Infrastructure) directamente.

**Problema:** La regla de dependencia de Clean Architecture prohíbe que Application conozca Infrastructure. Un handler de casos de uso no debe saber qué base de datos usa ni cómo se persiste — solo debe hablar con interfaces de puerto.

**Fix aplicado:**
- Se crearon `ISeatRepository` e `IReservationRepository` en `Inventory.Application.Ports`.
- Se implementaron en `Inventory.Infrastructure.Persistence` (`SeatRepository`, `ReservationRepository`) usando `InventoryDbContext`.
- `CreateReservationCommandHandler` se refactorizó para recibir `ISeatRepository` e `IReservationRepository` en lugar de `InventoryDbContext`.
- Se registraron las implementaciones en `ServiceCollectionExtensions`.

---

### 2.3 Modelo de dominio anémico en Order (Rich Domain Model)

**Hallazgo:** `Order` era una clase de datos pura con setters públicos. La lógica de negocio (validar estado, sumar total, transicionar estados) vivía dispersa en los handlers de Application.

**Problema:** Un modelo anémico viola el principio de encapsulación y hace que las reglas del negocio sean difíciles de encontrar y reutilizar. Los handlers acaban siendo scripts procedurales en lugar de coordinadores de dominio.

**Fix aplicado:**

```csharp
// Antes: anémico
order.State = "pending";
order.TotalAmount += price;

// Después: rich domain model
order.Checkout();       // valida estado, lanza si no es Draft
order.AddItem(seatId, price);  // valida duplicados, recalcula total
```

`Order` recibió:
- Constructor privado + factory method `Order.Create(userId, guestToken)` con validación.
- Setters privados en `Id`, `State`, `TotalAmount`, `Items`.
- Backing field `_items` con exposición `IReadOnlyCollection<OrderItem>`.
- Métodos de comportamiento: `AddItem()`, `Checkout()`, `MarkAsPaid()`, `Cancel()`, `BelongsTo()`.
- Constantes de estado: `StateDraft`, `StatePending`, `StatePaid`, `StateFulfilled`, `StateCancelled`.

La configuración de EF Core se actualizó para usar el backing field:
```csharp
entity.Navigation(x => x.Items)
      .HasField("_items")
      .UsePropertyAccessMode(PropertyAccessMode.Field);
```

---

### 2.4 Magic strings en estados y lógica de negocio

**Hallazgo:** Comparaciones directas con cadenas literales: `order.State = "pending"`, `p.Status == "succeeded" || p.Status == "completed"`, `res.Status = "expired"`.

**Problema:** Los magic strings no generan error de compilación si se escriben mal, no son refactorizables con seguridad, y ocultan el vocabulario del dominio.

**Fix aplicado:**

| Entidad | Antes | Después |
|---|---|---|
| `Order` | `"draft"`, `"pending"`, `"paid"` | `Order.StateDraft`, `Order.StatePending`, `Order.StatePaid` |
| `Reservation` | `"active"`, `"expired"`, `"confirmed"` | `Reservation.StatusActive`, `Reservation.StatusExpired`, `Reservation.StatusConfirmed` |
| `Payment` | `"succeeded"`, `"completed"`, `"pending"` | `Payment.StatusSucceeded`, `Payment.StatusPending` |

---

### 2.5 Exception swallowing en handlers de Application

**Hallazgo:** `AddToCartHandler` y `CheckoutOrderHandler` usaban `catch (Exception ex)` genérico que convertía cualquier error de infraestructura (base de datos, red, timeout) en una respuesta de negocio con `Success = false`.

**Problema:** Enmascarar excepciones de infraestructura impide que el sistema falle ruidosamente, dificulta el diagnóstico en producción y genera falsos negativos en pruebas. Una excepción de base de datos no es lo mismo que una regla de negocio violada.

**Fix aplicado:**

Se eliminó el `catch (Exception ex)` genérico. En su lugar se capturan únicamente excepciones de dominio específicas que representan violaciones de reglas de negocio:

```csharp
// AddToCartHandler — solo captura la regla "asiento ya en carrito"
try { existingOrder.AddItem(request.SeatId, request.Price); }
catch (InvalidOperationException)
{
    return new AddToCartResponse(false, "Seat is already in the cart", null);
}

// CheckoutOrderHandler — captura "orden vacía" y "estado incorrecto"
try { order.Checkout(); }
catch (InvalidOperationException ex) when (ex.Message.Contains("empty"))
    { return new CheckoutOrderResponse(false, "Order is empty", null); }
catch (InvalidOperationException)
    { return new CheckoutOrderResponse(false, "Order is not in draft state", null); }
```

Las excepciones de repositorio (infraestructura) ahora propagan correctamente.

---

### 2.6 Bug de idempotencia en Payment

**Hallazgo:** `ProcessPaymentHandler` verificaba `p.Status == "succeeded" || p.Status == "completed"` — el estado `"completed"` no existe en el dominio de pagos; nunca podía ser verdadero.

**Fix aplicado:** Se reemplazó por `p.Status == Payment.StatusSucceeded` usando la constante de dominio. El `"completed"` sobrante fue eliminado.

---

### 2.7 Hardcodes de presentación filtrados a datos de negocio

**Hallazgo:** `GetOrderHandler` retornaba `"guest@example.com"` como email de cliente para guests, y `OrderDto` tenía `EventName = "Event Details Pending"` y `SeatNumber = "N/A"` como valores por defecto.

**Problema:** Los valores por defecto de presentación no deben vivir en el handler de Application. Los consumidores del DTO (Fulfillment, API) deben decidir el fallback.

**Fix aplicado:**
- `GetOrderHandler` retorna `null` para campos sin dato (UserId de guest, EventName, EventId).
- `OrderDto` usa `string?` con `null` como default en lugar de strings de placeholder.
- Se creó `OrderEnrichmentDto` para la ruta `/orders/{id}` del controlador, separando la proyección de enriquecimiento del DTO genérico.

---

### 2.8 Tests reescritos para reflejar el nuevo modelo de dominio

**Hallazgo después de los fixes:** Los tests de Application (`AddToCartHandlerTests`, `CheckoutOrderHandlerTests`, `GetOrderHandlerTests`) construían objetos `Order` con object initializers (`new Order { Id = ..., State = "draft" }`) que dejaron de compilar al privatizar constructores y setters.

**Fix aplicado:**

Todos los tests fueron reescritos para usar los factory methods y métodos de dominio:

```csharp
// Antes
var order = new Order { Id = orderId, State = "draft", Items = new List<OrderItem> { ... } };

// Después
var order = Order.Create(userId, null);
order.AddItem(seatId, price);
var orderId = order.Id; // se captura del objeto creado
```

Adicionalmente:
- Los tests `_WhenRepositoryThrows_ShouldReturnFailure` se actualizaron para asegurar que la excepción **propaga** en lugar de esperar una respuesta de fallo (consistente con la eliminación del exception swallowing).
- El theory `Handle_WithNonDraftState_ShouldReturnFailure` usa un helper `CreateOrderInState()` que avanza el orden via métodos de dominio (`Checkout()`, `MarkAsPaid()`, `Cancel()`). El caso `"fulfilled"` fue removido del `[InlineData]` ya que no existe método `Fulfill()` en el dominio.
- Los mocks de `UpdateAsync` retornan el mismo objeto mutado en lugar de una copia construida con el initializer — aprovechando que los métodos de dominio mutan en lugar de retornar nuevas instancias.

---

## 3. Por qué SpecKit hizo esto más estructurado

### 3.1 El problema de auditar sin especificación

Sin un flujo estructurado, una auditoría arquitectónica tiende a derivar en:

- Cambios ad-hoc sin criterio de "listo" definido.
- Correcciones que resuelven el síntoma pero no la causa raíz.
- Regresiones introducidas porque no hay contexto compartido entre sesiones.
- Imposibilidad de priorizar: todo parece urgente o nada parece urgente.

### 3.2 Lo que SpecKit aportó en cada fase

**`speckit.specify` → `spec.md`**

Antes de tocar una sola línea de código, se documentaron:
- 6 User Stories con criterios de aceptación verificables (`Given / When / Then`).
- 16 Functional Requirements numerados (FR-001 a FR-016).
- 6 Success Criteria medibles (SC-01 a SC-06), todos expresados como comandos ejecutables:
  ```
  SC-01: grep -r "InventoryDbContext" services/inventory/src/Inventory.Application → 0 resultados
  SC-04: dotnet test → 0 failures
  ```

Esto convirtió la auditoría en compromisos concretos y verificables, no en opiniones.

**`speckit.plan` → `plan.md`**

El plan documentó:
- Un **Constitution Check** (tabla explícita de cumplimiento de Clean Architecture por cada decisión de diseño) antes de proponer cualquier cambio.
- Cuatro decisiones de arquitectura (D1-D4) con razonamiento explícito: por qué `const string` en lugar de `enum` para los estados (evitar complejidad de migración en EF Core), por qué backing field `_items` en lugar de lista pública, etc.
- El modelo de datos `Before / After` para los DTOs de Ordering.

Sin este paso, la decisión de usar `const string` vs `enum` se habría tomado implícitamente y podría haber requerido revertir migraciones.

**`speckit.tasks` → `tasks.md`**

36 tareas ordenadas en 9 fases con dependencias explícitas. Esto permitió detectar un problema de orden antes de ejecutar:

> La Phase 2 (refactorizar el handler) depende de que Phase 6 (enriquecer el dominio) esté completa — porque el handler llama a `seat.Reserve()` que no existía aún.

Sin el grafo de dependencias, este error de secuencia se habría descubierto solo al compilar, no al planear.

**`speckit.implement`**

Cada corrección se ejecutó con trazabilidad hacia su tarea (`T003`, `T026`, etc.) y su User Story (`US1`, `US2`). Cuando surgieron errores en cascada (privatizan `Order.Items` → falla EF Core → falla el consumer de Kafka → fallan los tests), el contexto del plan permitió diagnosticar la causa raíz en lugar de aplicar parches.

### 3.3 El valor diferencial: separación entre qué y cómo

| Fase SpecKit | Pregunta que responde |
|---|---|
| `specify` | ¿Qué está mal y cuál es el estándar correcto? |
| `plan` | ¿Cuál es la forma arquitectónicamente correcta de corregirlo? |
| `tasks` | ¿En qué orden se ejecutan los cambios sin romper el sistema? |
| `implement` | ¿Cómo se ejecuta cada cambio concreto? |

Esta separación es la diferencia entre refactorizar con un mapa y refactorizar a ciegas. El tiempo invertido en `specify` y `plan` se recupera evitando regresiones y retrabajo.

### 3.4 Evidencia concreta del valor

| Situación | Sin SpecKit | Con SpecKit |
|---|---|---|
| Order.Items como IReadOnlyCollection rompe EF Core | Se descubre en runtime o en integración | Anticipado en el plan (D2: configurar backing field) |
| Inventory.Application referenciaba Infrastructure | Se descubre al compilar, causa confusión | Documentado en Constitution Check como violación existente a corregir |
| Tests usan `new Order { }` que deja de compilar | Sorpresa al final del proceso | Listado como tarea T029 en la misma fase que el modelo |
| ¿`"completed"` es un estado válido en Payment? | Depende de memoria o grep ad-hoc | FR-010 documenta explícitamente que solo `StatusSucceeded` es el estado terminal |
| ¿Se debería hacer `Fulfill()` en Order? | Decisión improvisada | Detectado durante los tests: `"fulfilled"` no tiene método → removido del InlineData con decisión documentada |

---

## 4. Estado final de correcciones

| # | Corrección | US | Archivo(s) clave | Estado |
|---|---|---|---|---|
| 1 | Puertos Inventory Domain → Application | US1 | `Inventory.Application/Ports/I*.cs` | ✅ |
| 2 | Handler Inventory sin DbContext | US1 | `CreateReservationCommandHandler.cs` | ✅ |
| 3 | Puertos Identity Domain → Application | US1 | `Identity.Application/Ports/I*.cs` | ✅ |
| 4 | IEventPublisher Infrastructure → Application | US1 | `Fulfillment.Application/Ports/IEventPublisher.cs` | ✅ |
| 5 | Rich Domain Model en Order | US2 | `Order.cs` | ✅ |
| 6 | Magic strings → constantes en Order | US2 | `Order.cs`, handlers | ✅ |
| 7 | Magic strings → constantes en Reservation/Seat | US3 | `Reservation.cs`, `Seat.cs`, consumers | ✅ |
| 8 | Magic strings + bug idempotencia en Payment | US4 | `ProcessPaymentHandler.cs` | ✅ |
| 9 | Exception swallowing eliminado | US5 | `AddToCartHandler.cs`, `CheckoutOrderHandler.cs` | ✅ |
| 10 | Hardcodes `"guest@example.com"` eliminados | US5 | `GetOrderHandler.cs`, `OrdersController.cs` | ✅ |
| 11 | Tests Application reescritos para dominio rico | US2 | `*HandlerTests.cs` | ✅ |
| 12 | Tests Domain reescritos con cobertura de comportamiento | US2 | `OrderTests.cs`, `OrderItemTests.cs` | ✅ |

**Build:** `0 errors, 0 warnings`
**Tests:** `48 passed, 0 failed`
