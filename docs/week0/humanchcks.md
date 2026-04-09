# Human Checks & IA Decision Log

- Fecha: 28 de Febrero de 2026
- Revisor: GitHub Copilot & Equipo de Desarrollo
- Checklist:
  - [x] Verificar acceptance scenarios en `specs/001-ticketing-mvp/spec.md`
  - [x] Confirmar que `AI_WORKFLOW.MD` contiene prompts y evidencias
  - [x] Revisar tests unitarios e integración para T001-T010

## 🧠 Decisiones de Diseño y Arquitectura

1. **Uso de Redis para Locks de Asientos:**
   - **Razón:** Evitar condiciones de carrera en reserva de asientos.
   - **Referencia de Código:** [CreateReservationCommandHandler.cs](services/inventory/src/Inventory.Application/UseCases/CreateReservation/CreateReservationCommandHandler.cs#L45) (Ver comentario `HUMAN CHECK`).

2. **Optimización de Lectura en Catálogo:**
   - **Razón:** Se usa `AsNoTracking()` para mejorar el rendimiento en el listado de eventos al evitar el overhead de seguimiento de EF Core.
   - **Referencia de Código:** [CatalogRepository.cs](services/catalog/src/Infrastructure/Persistence/CatalogRepository.cs#L17) (Ver comentario `HUMAN CHECK`).

3. **Concurrencia Optimista (RowVersion):**
   - **Razón:** Implementación de `IsRowVersion` en la base de datos como segunda capa de protección para la integridad de los datos de asientos.
   - **Referencia de Código:** [InventoryDbContext.cs](services/inventory/src/Inventory.Infrastructure/Persistence/InventoryDbContext.cs#L18) (Ver comentario `HUMAN CHECK`).

4. **Gestión de Configuración y Secretos (.env):**
   - **Decisión:** Se ha decidido **NO utilizar archivos `.env`** ni mecanismos de secretos externos para este proyecto.
   - **Razón (Contexto Training):** Para facilitar las revisiones de pares y el proceso de evaluación en el marco del entrenamiento, permitiendo que cualquier desarrollador pueda clonar y ejecutar `docker compose up` sin necesidad de intercambiar archivos de configuración manualmente por canales externos.
   - **Nota de Seguridad:** Esta es una decisión puramente educativa/académica; en un entorno profesional, se deben utilizar Secret Managers o variables de entorno protegidas.

5. **Arquitectura Hexagonal + CQRS:**
   - **Razón:** Desacoplamiento total de lógica de negocio e infraestructura.
   - **Referencia:** Estructura de carpetas global `Domain -> Application -> Infrastructure`.

## ⚠️ Registro de Alucinaciones o Errores de la IA

1. **Alucinación de Rutas en Inventory (Minimal APIs):**
   - **Evento:** Búsqueda fallida de controladores en `Inventory` asumiendo controladores clásicos.
   - **Corrección:** El servicio usa **Minimal APIs**.
   - **Referencia de Código:** [ReservationEndpoints.cs](services/inventory/src/Inventory.Api/Endpoints/ReservationEndpoints.cs) (Ver comentario `HUMAN CHECK IA`).

2. **Suposición de Estructura de Proyecto en Ordering (Event-Driven):**
   - **Evento:** Suposición de que todos los servicios operan por `UseCases` síncronos.
   - **Realidad:** `Ordering` opera fuertemente por eventos en segundo plano.
   - **Referencia de Código:** [ReservationEventConsumer.cs](services/ordering/src/Infrastructure/Events/ReservationEventConsumer.cs#L105) (Ver comentario `HUMAN CHECK`).

3. **Manejo de Reintentos Manuales:**
   - **Evento:** Implementación de bucles de reintento manuales en consumidores de Kafka.
   - **Nota de Deuda:** Se identifica la necesidad de migrar a Polly.
   - **Referencia de Código:** [ReservationEventConsumer.cs](services/ordering/src/Infrastructure/Events/ReservationEventConsumer.cs#L108) (Ver comentario `HUMAN CHECK`).

## 📝 Notas Adicionales
- Se recomienda que el revisor humano valide periódicamente que las referencias a líneas de código en este archivo siguen siendo precisas tras refactorizaciones de deuda técnica.
