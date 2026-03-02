# Reporte de Deuda Técnica

- **Periodo:** Semana 0 - Febrero 2026
- **Estado General:** Arquitectura Hexagonal y SOLID implementados. Los puntos a continuación representan mejoras técnicas necesarias para un entorno productivo robusto.

## Acciones para Reducir Deuda Técnica

1. **Consistencia Eventual (Patrón Outbox):**
   - **Problema:** Se persiste en DB y luego se publica en Kafka. Si Kafka falla, la DB queda inconsistente.
   - **Referencia:** [CreateReservationCommandHandler.cs](services/inventory/src/Inventory.Application/UseCases/CreateReservation/CreateReservationCommandHandler.cs#L92)
   - **Mejora:** Guardar el evento en una tabla `Outbox` dentro de la misma transacción de base de datos.

2. **Validación de Datos Desacoplada (FluentValidation):**
   - **Problema:** Validaciones manuales y repetitivas dentro de los Handlers de MediatR.
   - **Referencia:** [ReservationEndpoints.cs](services/inventory/src/Inventory.Api/Endpoints/ReservationEndpoints.cs#L30-L33)
   - **Mejora:** Implementar `IPipelineBehavior` para validar automáticamete antes de entrar al Handler.

3. **Manejo Global de Excepciones (Middleware):**
   - **Problema:** Uso extensivo de bloques `try-catch` manuales en los controladores que ensucian la lógica de presentación.
   - **Referencia:** [ReservationEndpoints.cs](services/inventory/src/Inventory.Api/Endpoints/ReservationEndpoints.cs#L35-L51)
   - **Mejora:** Crear un Middleware que capture excepciones y devuelva un formato estándar **ProblemDetails**.

4. **Resiliencia de Conexiones (Polly):**
   - **Problema:** Reintentos manuales básicos que pueden causar saturación o fallos en cascada.
   - **Referencia:** [ReservationEventConsumer.cs](services/ordering/src/Infrastructure/Events/ReservationEventConsumer.cs#L45-L68)
   - **Mejora:** Configurar políticas de **Circuit Breaker** y **Exponential Backoff** usando la librería Polly.

5. **Exposición de Servicios (API Gateway):**
   - **Problema:** El frontend tiene hardcodeados los puertos de cada microservicio.
   - **Referencia:** [catalog.ts](frontend/lib/api/catalog.ts#L4)
   - **Mejora:** Implementar un único punto de entrada (YARP/Ocelot) para unificar la URL base.
