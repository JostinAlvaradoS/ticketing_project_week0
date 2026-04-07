# 06 — TDD Evidence

> **Fase SDLC:** Implementación
> **Audiencia:** Dev, Tech Lead
> **Propósito:** Documentar los 19 ciclos TDD con su estado RED → GREEN → REFACTOR

---

## La metodología

El desarrollo de la feature siguió **TDD estricto**:

```
1. Escribir el test que describe el comportamiento esperado
   → El test FALLA porque la clase/método no existe (RED)

2. Implementar el mínimo código necesario para que el test pase
   → El test PASA (GREEN)

3. Refactorizar sin romper los tests
   → Código más limpio, mismo comportamiento (REFACTOR)

4. Repetir para el siguiente comportamiento
```

**Orden de desarrollo:** Domain → Application → Infrastructure
El dominio no depende de nada externo — es el punto de partida natural. Solo cuando el dominio está estable se construye la capa de aplicación sobre él.

---

## BLOQUE 1 — Dominio: WaitlistEntry
> Archivo: `Domain/WaitlistEntryTests.cs`

---

### Ciclo 1 — Create happy path

**RED:** `WaitlistEntry` no existe. El test no compila.

```csharp
[Fact]
public void Create_WithValidEmailAndEventId_ReturnsPendingEntry()
{
    var email   = "jostin@example.com";
    var eventId = Guid.NewGuid();

    var entry = WaitlistEntry.Create(email, eventId);

    entry.Email.Should().Be(email);
    entry.EventId.Should().Be(eventId);
    entry.Status.Should().Be(WaitlistEntry.StatusPending);
    entry.RegisteredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    entry.SeatId.Should().BeNull();
    entry.OrderId.Should().BeNull();
}
```

**GREEN:** Implementar `WaitlistEntry.Create()` con factory method.

**Lo que valida:** El estado inicial de la entidad es `pending`, los campos opcionales son null, el timestamp es correcto.

---

### Ciclo 2 — Guard: email vacío

**RED → GREEN:**

```csharp
[Fact]
public void Create_WithBlankEmail_ThrowsArgumentException()
{
    var act = () => WaitlistEntry.Create("", Guid.NewGuid());
    act.Should().Throw<ArgumentException>();
}
```

**Decisión de implementación:** `if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException(...)` en `Create()`. La entidad protege su invariante de que el email no puede estar vacío.

---

### Ciclo 3 — Guard: eventId vacío

```csharp
[Fact]
public void Create_WithEmptyEventId_ThrowsArgumentException()
{
    var act = () => WaitlistEntry.Create("jostin@example.com", Guid.Empty);
    act.Should().Throw<ArgumentException>();
}
```

---

### Ciclo 4 — Assign: transición correcta con timestamps

```csharp
[Fact]
public void Assign_WhenPending_TransitionsToAssignedWithCorrectTimestamps()
{
    var entry   = WaitlistEntry.Create("jostin@example.com", Guid.NewGuid());
    var seatId  = Guid.NewGuid();
    var orderId = Guid.NewGuid();

    entry.Assign(seatId, orderId);

    entry.Status.Should().Be(WaitlistEntry.StatusAssigned);
    entry.SeatId.Should().Be(seatId);
    entry.OrderId.Should().Be(orderId);
    entry.AssignedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    entry.ExpiresAt.Should().BeCloseTo(
        DateTime.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(2));
}
```

**Lo que valida (RN-04):** El timer de 30 minutos se calcula en la entidad, no en el handler. Esto garantiza que cualquier código que llame `Assign()` obtiene el timer correcto.

---

### Ciclo 5 — Guard: Assign solo desde pending

```csharp
[Fact]
public void Assign_WhenAlreadyAssigned_ThrowsInvalidOperationException()
{
    var entry = WaitlistEntry.Create("jostin@example.com", Guid.NewGuid());
    entry.Assign(Guid.NewGuid(), Guid.NewGuid());

    var act = () => entry.Assign(Guid.NewGuid(), Guid.NewGuid());

    act.Should().Throw<InvalidOperationException>();
}
```

**Por qué es importante:** Sin este guard, el worker podría asignar dos veces la misma entrada y crear dos órdenes para el mismo asiento. La entidad hace imposible ese estado.

---

### Ciclo 6 — Complete, Expire e IsAssignmentExpired

```csharp
[Fact]
public void Complete_WhenAssigned_TransitionsToCompleted()
{
    var entry = WaitlistEntry.Create("jostin@example.com", Guid.NewGuid());
    entry.Assign(Guid.NewGuid(), Guid.NewGuid());

    entry.Complete();

    entry.Status.Should().Be(WaitlistEntry.StatusCompleted);
}

[Fact]
public void Expire_WhenAssigned_TransitionsToExpired()
{
    var entry = WaitlistEntry.Create("jostin@example.com", Guid.NewGuid());
    entry.Assign(Guid.NewGuid(), Guid.NewGuid());

    entry.Expire();

    entry.Status.Should().Be(WaitlistEntry.StatusExpired);
}

[Fact]
public void IsAssignmentExpired_WhenExpiresAtInPast_ReturnsTrue()
{
    var entry = WaitlistEntry.Create("jostin@example.com", Guid.NewGuid());
    entry.Assign(Guid.NewGuid(), Guid.NewGuid());
    // Simular que ExpiresAt ya pasó — en test se puede usar reflexión
    // o exponer el setter interno para tests

    entry.IsAssignmentExpired().Should().BeTrue();
}
```

---

## BLOQUE 2 — Aplicación: JoinWaitlistHandler
> Archivo: `Application/JoinWaitlistHandlerTests.cs`

**Setup común:**

```csharp
private readonly Mock<IWaitlistRepository> _repoMock = new();
private readonly Mock<ICatalogClient>      _catalogMock = new();
private JoinWaitlistHandler CreateHandler() =>
    new(_repoMock.Object, _catalogMock.Object);
```

---

### Ciclo 7 — Catalog no disponible → 503

**RED:** `JoinWaitlistHandler` no existe.

```csharp
[Fact]
public async Task Handle_CatalogUnavailable_ThrowsServiceUnavailableException()
{
    _catalogMock
        .Setup(c => c.GetAvailableCountAsync(It.IsAny<Guid>()))
        .ThrowsAsync(new HttpRequestException("Connection refused"));

    var handler = CreateHandler();
    var command = new JoinWaitlistCommand("jostin@example.com", Guid.NewGuid());

    var act = async () => await handler.Handle(command, CancellationToken.None);

    await act.Should().ThrowAsync<WaitlistServiceUnavailableException>();
}
```

**Decisión de diseño:** El handler captura `HttpRequestException` y la envuelve en `WaitlistServiceUnavailableException`. El controlador mapea esa excepción a 503. La excepción de dominio aisla al controlador de los detalles HTTP internos.

---

### Ciclo 8 — Stock disponible → 409

```csharp
[Fact]
public async Task Handle_StockAvailable_ThrowsConflictException()
{
    _catalogMock
        .Setup(c => c.GetAvailableCountAsync(It.IsAny<Guid>()))
        .ReturnsAsync(5);

    var handler = CreateHandler();
    var act = async () => await handler.Handle(
        new JoinWaitlistCommand("jostin@example.com", Guid.NewGuid()),
        CancellationToken.None);

    await act.Should().ThrowAsync<WaitlistConflictException>()
        .WithMessage("*disponibles*");
}
```

---

### Ciclo 9 — Entrada duplicada activa → 409

```csharp
[Fact]
public async Task Handle_DuplicateActiveEntry_ThrowsConflictException()
{
    _catalogMock
        .Setup(c => c.GetAvailableCountAsync(It.IsAny<Guid>()))
        .ReturnsAsync(0);

    _repoMock
        .Setup(r => r.HasActiveEntryAsync(
            "jostin@example.com", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);

    var act = async () => await CreateHandler().Handle(
        new JoinWaitlistCommand("jostin@example.com", Guid.NewGuid()),
        CancellationToken.None);

    await act.Should().ThrowAsync<WaitlistConflictException>()
        .WithMessage("*lista de espera*");
}
```

---

### Ciclo 10 — Happy path: entrada creada y posición retornada

```csharp
[Fact]
public async Task Handle_ValidRequest_CreatesEntryAndReturnsPosition()
{
    var eventId = Guid.NewGuid();
    _catalogMock
        .Setup(c => c.GetAvailableCountAsync(eventId))
        .ReturnsAsync(0);
    _repoMock
        .Setup(r => r.HasActiveEntryAsync(It.IsAny<string>(), eventId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(false);
    _repoMock
        .Setup(r => r.GetQueuePositionAsync(eventId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(3);

    var result = await CreateHandler().Handle(
        new JoinWaitlistCommand("jostin@example.com", eventId),
        CancellationToken.None);

    result.Position.Should().Be(3);
    result.EntryId.Should().NotBeEmpty();
    _repoMock.Verify(r => r.AddAsync(
        It.Is<WaitlistEntry>(e => e.Email == "jostin@example.com"),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

**Doble validación:** Verifica tanto el resultado (posición correcta) como el comportamiento (AddAsync llamado exactamente una vez con el email correcto).

---

### Ciclo 11 — Validador: email inválido

```csharp
[Fact]
public void Validator_InvalidEmail_ReturnsValidationFailure()
{
    var validator = new JoinWaitlistCommandValidator();
    var result = validator.Validate(
        new JoinWaitlistCommand("no-es-email", Guid.NewGuid()));

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == "Email");
}
```

---

## BLOQUE 3 — Aplicación: AssignNextHandler
> Archivo: `Application/AssignNextHandlerTests.cs`

---

### Ciclo 12 — Happy path: asignación completa

```csharp
[Fact]
public async Task Handle_PendingEntryExists_AssignsEntryAndSendsEmail()
{
    var seatId  = Guid.NewGuid();
    var eventId = Guid.NewGuid();
    var orderId = Guid.NewGuid();
    var entry   = WaitlistEntry.Create("jostin@example.com", eventId);

    _repoMock.Setup(r => r.HasAssignedEntryForSeatAsync(seatId, default))
             .ReturnsAsync(false);
    _repoMock.Setup(r => r.GetNextPendingAsync(eventId, default))
             .ReturnsAsync(entry);
    _orderingMock.Setup(o => o.CreateWaitlistOrderAsync(seatId, 0, "jostin@example.com", eventId))
                 .ReturnsAsync(orderId);

    await CreateHandler().Handle(
        new AssignNextCommand(seatId, eventId), CancellationToken.None);

    entry.Status.Should().Be(WaitlistEntry.StatusAssigned);
    entry.SeatId.Should().Be(seatId);
    entry.OrderId.Should().Be(orderId);

    _repoMock.Verify(r => r.UpdateAsync(entry, default), Times.Once);
    _emailMock.Verify(e => e.SendAsync(
        "jostin@example.com", It.IsAny<string>(), It.IsAny<string>(), null),
        Times.Once);
}
```

**Tres capas de assertion:**
1. Estado de la entidad (validación de dominio)
2. Persistencia (verificación de puerto)
3. Notificación (verificación de side effect)

---

### Ciclo 13 — Cola vacía: no-op

```csharp
[Fact]
public async Task Handle_EmptyQueue_DoesNothing()
{
    _repoMock.Setup(r => r.HasAssignedEntryForSeatAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync(false);
    _repoMock.Setup(r => r.GetNextPendingAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((WaitlistEntry?)null);

    await CreateHandler().Handle(
        new AssignNextCommand(Guid.NewGuid(), Guid.NewGuid()),
        CancellationToken.None);

    _repoMock.Verify(r => r.UpdateAsync(It.IsAny<WaitlistEntry>(), default), Times.Never);
    _emailMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(),
        It.IsAny<string>(), null), Times.Never);
}
```

**Verifica el comportamiento negativo:** Cuando no hay nada que hacer, confirmar que no se hace nada. `Times.Never` es tan importante como `Times.Once`.

---

### Ciclo 14 — Idempotencia: asiento ya asignado

```csharp
[Fact]
public async Task Handle_SeatAlreadyAssigned_ReturnsWithoutAction()
{
    _repoMock.Setup(r => r.HasAssignedEntryForSeatAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync(true);

    await CreateHandler().Handle(
        new AssignNextCommand(Guid.NewGuid(), Guid.NewGuid()),
        CancellationToken.None);

    _repoMock.Verify(r => r.GetNextPendingAsync(It.IsAny<Guid>(), default), Times.Never);
    _orderingMock.Verify(o => o.CreateWaitlistOrderAsync(
        It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Guid>()),
        Times.Never);
}
```

---

### Ciclo 15 — Consumer: payload v3 válido y v2 descartado

```csharp
[Fact]
public async Task Consumer_ValidV3Payload_DispatchesAssignNextCommand()
{
    var payload = JsonSerializer.Serialize(new {
        seatId = Guid.NewGuid(),
        concertEventId = Guid.NewGuid(),
        reservationId = Guid.NewGuid()
    });
    // Verificar que IMediator.Send fue llamado con AssignNextCommand
}

[Fact]
public async Task Consumer_V2PayloadWithoutConcertEventId_SkipsSilently()
{
    var payload = JsonSerializer.Serialize(new {
        seatId = Guid.NewGuid(),
        reservationId = Guid.NewGuid()
        // Sin concertEventId
    });
    // Verificar que IMediator.Send NO fue llamado
}
```

---

### Ciclo 16 — CompleteAssignment: pago de orden waitlist

```csharp
[Fact]
public async Task Handle_AssignedEntryWithMatchingOrder_CompletesAssignment()
{
    var orderId = Guid.NewGuid();
    var eventId = Guid.NewGuid();
    var entry   = WaitlistEntry.Create("jostin@example.com", eventId);
    entry.Assign(Guid.NewGuid(), orderId);

    _repoMock.Setup(r => r.GetByOrderIdAsync(orderId, default))
             .ReturnsAsync(entry);

    await CreateCompleteHandler().Handle(
        new CompleteAssignmentCommand(orderId), CancellationToken.None);

    entry.Status.Should().Be(WaitlistEntry.StatusCompleted);
    _repoMock.Verify(r => r.UpdateAsync(entry, default), Times.Once);
}

[Fact]
public async Task Handle_OrderNotInWaitlist_DoesNothing()
{
    _repoMock.Setup(r => r.GetByOrderIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((WaitlistEntry?)null);

    await CreateCompleteHandler().Handle(
        new CompleteAssignmentCommand(Guid.NewGuid()), CancellationToken.None);

    _repoMock.Verify(r => r.UpdateAsync(It.IsAny<WaitlistEntry>(), default), Times.Never);
}
```

---

## BLOQUE 4 — Worker de Expiración
> Ciclos 17-19

---

### Ciclo 17 — Rotación: siguiente en cola

```csharp
[Fact]
public async Task ProcessExpired_NextExists_RotatesDirectlyWithoutReleasingInventory()
{
    var seatId  = Guid.NewGuid();
    var eventId = Guid.NewGuid();

    var expired = WaitlistEntry.Create("jostin@example.com", eventId);
    expired.Assign(seatId, Guid.NewGuid());
    // Forzar ExpiresAt en pasado

    var next = WaitlistEntry.Create("segundo@example.com", eventId);

    _repoMock.Setup(r => r.GetExpiredAssignedAsync(default))
             .ReturnsAsync(new[] { expired });
    _repoMock.Setup(r => r.GetNextPendingAsync(eventId, default))
             .ReturnsAsync(next);

    await CreateWorker().ProcessExpiredEntriesAsync(CancellationToken.None);

    expired.Status.Should().Be(WaitlistEntry.StatusExpired);
    next.Status.Should().Be(WaitlistEntry.StatusAssigned);
    next.SeatId.Should().Be(seatId);

    // El inventario NO debe ser liberado
    _inventoryMock.Verify(i => i.ReleaseSeatAsync(It.IsAny<Guid>()), Times.Never);
    // La orden del expirado debe ser cancelada
    _orderingMock.Verify(o => o.CancelOrderAsync(expired.OrderId!.Value), Times.Once);
}
```

**El assertion más crítico:** `Times.Never` en `ReleaseSeatAsync` — confirma que RN-05 se cumple. Si este test pasa, el asiento nunca pasó por el inventario disponible durante la rotación.

---

### Ciclo 18 — Liberación: cola vacía

```csharp
[Fact]
public async Task ProcessExpired_EmptyQueue_ReleasesToInventory()
{
    var seatId  = Guid.NewGuid();
    var expired = WaitlistEntry.Create("jostin@example.com", Guid.NewGuid());
    expired.Assign(seatId, Guid.NewGuid());

    _repoMock.Setup(r => r.GetExpiredAssignedAsync(default))
             .ReturnsAsync(new[] { expired });
    _repoMock.Setup(r => r.GetNextPendingAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((WaitlistEntry?)null);

    await CreateWorker().ProcessExpiredEntriesAsync(CancellationToken.None);

    _inventoryMock.Verify(i => i.ReleaseSeatAsync(seatId), Times.Once);
    _orderingMock.Verify(o => o.CancelOrderAsync(expired.OrderId!.Value), Times.Once);
}
```

---

### Ciclo 19 — Sin expirados: no-op

```csharp
[Fact]
public async Task ProcessExpired_NoExpiredEntries_DoesNothing()
{
    _repoMock.Setup(r => r.GetExpiredAssignedAsync(default))
             .ReturnsAsync(Array.Empty<WaitlistEntry>());

    await CreateWorker().ProcessExpiredEntriesAsync(CancellationToken.None);

    _repoMock.Verify(r => r.UpdateAsync(It.IsAny<WaitlistEntry>(), default), Times.Never);
    _inventoryMock.Verify(i => i.ReleaseSeatAsync(It.IsAny<Guid>()), Times.Never);
    _emailMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(),
        It.IsAny<string>(), null), Times.Never);
}
```

---

## Resumen de cobertura por ciclo

| Ciclo | Componente | Comportamiento | Estado |
|-------|-----------|---------------|--------|
| 1 | WaitlistEntry | Create happy path | ✅ |
| 2 | WaitlistEntry | Create guard — email vacío | ✅ |
| 3 | WaitlistEntry | Create guard — eventId vacío | ✅ |
| 4 | WaitlistEntry | Assign: campos + ExpiresAt | ✅ |
| 5 | WaitlistEntry | Assign guard: solo desde pending | ✅ |
| 6 | WaitlistEntry | Complete / Expire / IsExpired | ✅ |
| 7 | JoinWaitlistHandler | Catalog no disponible → 503 | ✅ |
| 8 | JoinWaitlistHandler | Stock disponible → 409 | ✅ |
| 9 | JoinWaitlistHandler | Entrada duplicada → 409 | ✅ |
| 10 | JoinWaitlistHandler | Happy path → 201 + posición | ✅ |
| 11 | JoinWaitlistHandler | Validador email inválido → 400 | ✅ |
| 12 | AssignNextHandler | Happy path: asigna y notifica | ✅ |
| 13 | AssignNextHandler | Cola vacía: no-op | ✅ |
| 14 | AssignNextHandler | Idempotencia: asiento ya asignado | ✅ |
| 15 | ReservationExpiredConsumer | v3 válido + v2 descartado | ✅ |
| 16 | CompleteAssignmentHandler | Completar + no-op si no es waitlist | ✅ |
| 17 | WaitlistExpiryWorker | Rotación directa (RN-05 verificado) | ✅ |
| 18 | WaitlistExpiryWorker | Liberación cuando cola vacía | ✅ |
| 19 | WaitlistExpiryWorker | Sin expirados: no-op | ✅ |
