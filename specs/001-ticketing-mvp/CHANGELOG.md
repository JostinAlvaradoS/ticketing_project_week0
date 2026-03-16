# Registro de Cambios en Especificaciones (SDD CHANGELOG)

Este archivo registra todas las modificaciones realizadas a las definiciones de negocio, criterios de aceptación y archivos Gherkin `.feature` del proyecto **Ticketing Platform MVP**. Los repositorios de automatización de QA deben consultar este log antes de cada ciclo de ejecución para identificar impactos en los scripts de prueba.

---

## [2026-03-16] - Inicialización de Especificaciones Gherkin (Refinamiento INVEST)

### 🚀 Nuevas Funcionalidades (Features Agregadas)
- **HU-01 Selección y Reserva:** Se creó `hu01-reserva-asiento.feature` con escenarios de éxito, expiración (TTL) y manejo de concurrencia.
- **HU-02 Carrito y Pago:** Se creó `hu02-cart-pago.feature` para validar la transición de reserva a carrito y la integridad del pago dentro del tiempo límite.
- **HU-04 Exploración:** Se creó `hu04-exploracion.feature` incluyendo filtrado de catálogo y visualización de mapa interactivo por colores para visitantes.
- **HU-05 Configuración Admin:** Se creó `hu05-admin-config.feature` para cubrir la creación de eventos y las reglas de unicidad de asientos por parte del organizador.

### ⚠️ Reglas de Negocio Oficializadas (Parámetros Críticos)
- **TTL de Reserva:** Se define un tiempo de vida (Time To Live) de **15 minutos** para toda reserva temporal. Las pruebas deben validar la liberación automática de asientos tras este periodo.
- **Límite de Compra:** Se establece un máximo de **6 asientos** por proceso de reserva/compra por cliente.
- **Estados de Asiento:** Se estandarizan los estados visuales en el mapa: `Disponible` (Verde), `Reservado` (Amarillo), `Vendido` (Rojo).

### 🛠️ Impacto en Automatización (QA Task List)
- **UI:** Los actores de Screenplay/POM ahora deben interactuar con un temporizador (countdown) visible en pantalla al reservar.
- **API:** El endpoint de `/reservations` debe ser validado contra condiciones de carrera (Race Conditions) según el escenario de concurrencia definido en HU-01.
- **Backend:** Se requiere validación de eventos asíncronos en Kafka (`reservation-expired`) al cumplirse el TTL.

---

## [2026-02-28] - Definición Base de Arquitectura
- Publicación inicial de `spec.md` y `plan.md` para el MVP original.
- Configuración de contratos iniciales en `contracts/openapi/`.
