# DEBT REPORT

Resumen: este documento lista los 3 "pecados capitales" más relevantes detectados en el repo (alcance: solo carpetas `src/`) y el principio SOLID que cada uno viola, con ejemplos y recomendaciones rápidas.

---

## 1) Múltiples `DbContext` duplicados y mapeos repartidos

- Descripción: hay varias implementaciones de `DbContext` (por ejemplo `TicketingDbContext`, `PaymentDbContext`) repetidas en distintos servicios / carpetas. Esto provoca duplicación de mapeos, desincronización de entidades y mantenimiento costoso.
- Ejemplos en el repo:
- Ejemplos en el repo:
  - [ReservationService/src/ReservationService.Infrastructure/Persistence/TicketingDbContext.cs](ReservationService/src/ReservationService.Infrastructure/Persistence/TicketingDbContext.cs#L1)
  - [crud_service/src/CrudService.Infrastructure/Persistence/TicketingDbContext.cs](crud_service/src/CrudService.Infrastructure/Persistence/TicketingDbContext.cs#L1)
  - [paymentService/src/PaymentService.Infrastructure/Persistence/PaymentDbContext.cs](paymentService/src/PaymentService.Infrastructure/Persistence/PaymentDbContext.cs#L1)
- SOLID violado: **Single Responsibility Principle (SRP)** — la responsabilidad de definir el modelo de dominio/persistencia está fragmentada y repetida en varios artefactos, cada `DbContext` termina cargando más de una responsabilidad (mapeo + configuración específica del servicio).
- Riesgo: inconsistencias en esquema, bugs al migrar, esfuerzo duplicado para cambios en entidades.
- Remediación rápida: si varios servicios comparten la MISMA base de datos y hay contrato fuerte entre ellos, considerar consolidar entidades/mapeos en una librería compartida de dominio o en una capa de infraestructura común; reducir a 1-2 DbContext bien definidos. Dado que este repo agrupa microservicios independientes, la opción más segura suele ser:
  - Mantener un DbContext por microservicio (database-per-service) para preservar autonomía.
  - Extraer solo los modelos/contratos compartidos (DTOs, enums, tipos) a una librería versionada cuando exista necesidad real de compartir formato.
  - Usar eventos/contratos para sincronizar datos entre servicios en lugar de compartir un DbContext global.

---

## 2) Reglas de validación y números mágicos hardcodeados en controllers

- Descripción: varias validaciones de entrada y límites aparecen directamente en los controllers (por ejemplo `Quantity <= 1000` in `crud_service/src/CrudService.Api/Controllers/TicketsController.cs`), lo que dispersa reglas de negocio/contrato y complica cambios futuros.
- Ejemplos en `src/`:
  - [crud_service/src/CrudService.Api/Controllers/TicketsController.cs](crud_service/src/CrudService.Api/Controllers/TicketsController.cs#L64) — `if (request.Quantity <= 0 || request.Quantity > 1000)`
  - [crud_service/src/CrudService.Api/Controllers/EventsController.cs](crud_service/src/CrudService.Api/Controllers/EventsController.cs#L64) — validaciones de campos requeridos con mensajes literales.
- SOLID violado: **Open/Closed Principle (OCP)** — cambiar una regla (e.g. el límite máximo) requiere modificar código en múltiples controllers en lugar de extender/configurar la regla.
- Riesgo: duplicación de lógica, mayor probabilidad de inconsistencias entre endpoints, dificultad para exponer reglas al cliente (doc/validation).
- Remediación rápida: mover validaciones a DTOs con atributos (`[Range]`, `[Required]`) o usar un componente centralizado de validación (FluentValidation) y configurar límites en opciones/constantes versionadas.

---

## 3) Repositorios exponen `SaveChangesAsync()` — falta de Unit of Work por servicio

- Descripción: múltiples interfaces de repositorio en `src/` exponen `SaveChangesAsync()` y las implementaciones llaman `_context.SaveChangesAsync()` dentro del repositorio. Esto fuerza a los callers a conocer el comportamiento de persistencia y complica la coordinación transaccional.
- Ejemplos en `src/`:
  - [crud_service/src/CrudService.Application/Ports/Outbound/ITicketRepository.cs](crud_service/src/CrudService.Application/Ports/Outbound/ITicketRepository.cs#L1) incluye `Task SaveChangesAsync();`
  - [crud_service/src/CrudService.Infrastructure/Persistence/Repositories.cs](crud_service/src/CrudService.Infrastructure/Persistence/Repositories.cs#L1) implementa `SaveChangesAsync()` y lo invoca desde varios repositorios.
- SOLID violado: **Interface Segregation Principle (ISP)** — las interfaces mezclan responsabilidades de acceso a datos y control de persistencia/commit.
- Riesgo: cambios incompletos (commits parciales), pruebas más complicadas, menor claridad del flujo transaccional.
- Remediación rápida: introducir `IUnitOfWork` por microservicio que centralice `CommitAsync()`/`RollbackAsync()`; eliminar `SaveChangesAsync()` de las interfaces de repositorio y mantener repositorios centrados en operaciones 
## 3) Repositorios con responsabilidad de transacción (`SaveChangesAsync` por repo) — falta Unit of WorkCRUD.

---

+### Notas finales
+- Prioridad propuesta (teniendo en cuenta microservicios):
+  1. Asegurar `IUnitOfWork` y consistencia transaccional por microservicio (alto impacto local)
+  2. Extraer y versionar solo los contratos/DTOs compartidos cuando sea necesario (bajo acoplamiento)
+  3. Auditar que los UseCases/Services dependan únicamente de interfaces (mejora testabilidad)
+- Estas correcciones mejoran mantenibilidad, testabilidad y disminuyen deuda técnica.

Si quieres, puedo abrir PRs con:
- Un PR esqueleto que extraiga las ent
## 3) Repositorios con responsabilidad de transacción (`SaveChangesAsync` por repo) — falta Unit of Workidades a una librería `Domain/Models` compartida.
- Un PR que añada `IUnitOfWork` y refactorice un servicio pequeño para usarla (ejemplo incremental).
