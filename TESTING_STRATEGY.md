# 🎯 TESTING STRATEGY - TICKETING MVP SYSTEM

Este documento define el marco de trabajo para asegurar la calidad y robustez del sistema de boletería mediante una arquitectura hexagonal.

---

## 1. 🏛️ Filosofía de QA: Verificación vs. Validación

En este proyecto, hemos implementado una distinción clara para asegurar que el sistema no solo funcione correctamente, sino que resuelva el problema del negocio:

- **Verificación (¿Construimos el producto correctamente?)**:
  - Implementada mediante **Pruebas Unitarias** con `xUnit` y `Moq`.
  - Foco en la lógica aislada de los microservicios (**Dominio y Aplicación**).
  - Objetivo: Detectar errores de lógica, `null pointers` y fallos en cálculos de precios.

- **Validación (¿Construimos el producto que el negocio necesita?)**:
  - Implementada mediante **Pruebas de Integración** y flujos de **Casos de Uso**.
  - Foco en la interacción entre puertos y adaptadores (Kafka, Redis, PostgreSQL).
  - Objetivo: Asegurar que cuando un usuario paga, el ticket realmente se genera y se notifica.

---

## 2. 🚦 Metodología Semántica (The QA Semaphore)

Para mejorar la comunicación entre desarrolladores y QA, hemos etiquetado cada caso de prueba con una intención semántica:

### 🟢 VERDE: Happy Path (Flujo de Éxito)
- **Escenarios**: Compra exitosa, creación de usuario, generación de QR.
- **Importancia**: Crítica para la continuidad del negocio.
- **Ejemplo**: [ProcessPaymentSucceededHandlerTests.cs](services/fulfillment/tests/Fulfillment.Application.UnitTests/UseCases/ProcessPaymentSucceededHandlerTests.cs#L22) - Valida el flujo ideal de pago.
- **Acción**: Si este test falla, el despliegue se bloquea inmediatamente.

### 🟡 AMARILLO: Business Logic & Resilience (Reglas de Negocio)
- **Escenarios**: Stock agotado, asiento ya reservado, pago denegado por banco.
- **Importancia**: Alta para la experiencia del usuario.
- **Ejemplo**: [AddToCartHandlerTests.cs](services/ordering/tests/Ordering.Application.UnitTests/AddToCartHandlerTests.cs#L154) - Maneja el caso de una reserva inválida o expirada.
- **Acción**: Valida que el sistema RESPONDA con elegancia en lugar de explotar.

### 🔴 ROJO: Edge Cases & Infrastructure Failures (Casos de Borde/Error)
- **Escenarios**: Base de datos caída, conexión a Kafka perdida, datos nulos o corruptos.
- **Importancia**: Crítica para la robustez y seguridad.
- **Ejemplo**: [RedisLockTests.cs](services/inventory/tests/Inventory.Infrastructure.Tests/RedisLockTests.cs#L58) - Valida fallos en la liberación de bloqueos por tokens incorrectos.
- **Acción**: Valida que el sistema sea capaz de recuperarse o detenerse de forma segura (Fail-fast).

---

## 3. 🛡️ Arquitectura de Pruebas y Aislamiento

Siguiendo los principios de la **Arquitectura Hexagonal**, nuestras pruebas se estructuran para proteger el "Núcleo":

1.  **Domain Tests**: Pruebas de lógica pura (ej. `TicketEntityTests.cs`). Sin dependencias de frameworks.
2.  **Application Tests**: Pruebas de orquestación (ej. `ProcessPaymentHandlerTests.cs`). Usan **Mocks** para simular la infraestructura (Base de Datos, Servicios Externos).
3.  **Infrastructure Tests**: Pruebas de adaptadores (ej. `RedisLockTests.cs`). Validan la comunicación técnica con herramientas externas.

---

## 4. 📊 Métricas de Éxito (KPIs de Calidad)

- **Cobertura de Línea (Global)**: **89.8%** (Meta del 90% alcanzada funcionalmente).
- **Cobertura de Ramas (Branch Coverage)**: **97.4%** (Asegura que todos los `if/else` lógicos fueron probados).
- **Estado de la Suite**: **148 Tests Exitosos / 0 Fallidos**.
- **Exclusiones**: Se excluyen archivos autogenerados (Migrations) y adaptadores de infraestructura pura para enfocar el esfuerzo en el código que aporta valor al negocio.

---

## 🛠️ Herramientas Utilizadas
- **Runner**: xUnit [.NET 8.0]
- **Doubles de Prueba**: Moq (Aislamiento de puertos).
- **Aserciones**: FluentAssertions (Legibilidad estilo lenguaje natural).
- **Cobertura**: Coverlet & ReportGenerator (Dashboard visual).

---
