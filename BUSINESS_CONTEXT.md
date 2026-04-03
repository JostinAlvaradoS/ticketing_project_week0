# Contexto de Negocio - Proyecto Ticketing MVP

## 1. Descripción del Proyecto
- **Nombre del Proyecto:** Ticketing Platform MVP
- **Objetivo del Proyecto:** Entregar una plataforma mínima viable para la venta de boletos que permita a los usuarios buscar eventos, seleccionar asientos en tiempo real, realizar reservas temporales y completar la compra con la generación de un boleto digital (QR) y notificación por correo electrónico.

## 2. Flujos Críticos del Negocio
- **Principales Flujos de Trabajo:**
    - **Búsqueda y Visualización:** Navegación por el catálogo de eventos y consulta de disponibilidad de asientos en mapas visuales.
    - **Reserva Temporal:** Bloqueo de asientos seleccionados por un tiempo determinado (TTL) para evitar compras duplicadas.
    - **Proceso de Compra (Checkout):** Adición de reservas al carrito, ingreso de información y procesamiento de pago (simulado en el MVP).
    - **Emisión de Boletos (Fulfillment):** Generación automática de un PDF con código QR tras la confirmación del pago.
    - **Notificación:** Envío de correos electrónicos con la confirmación y el enlace al boleto. (no se aborda en el alcance actual, solo se guardar en una tabla de emails pendientes).
- **Módulos o Funcionalidades Críticas:**
    - **Catalog Service:** Gestión de eventos, recintos y precios.
    - **Inventory Service:** Control de disponibilidad de asientos y gestión de reservas con bloqueos de concurrencia.
    - **Ordering Service:** Gestión de carritos y estados de las órdenes (vencimiento, pago, cumplimiento).
    - **Payment Service:** Simulación de transacciones y comunicación de resultados.
    - **Fulfillment Service:** Generación de archivos digitales y códigos QR.

## 3. Reglas de Negocio y Restricciones
- **Reglas de Negocio Relevantes:**
    - **Tiempo de Reserva (TTL):** Una reserva de asiento expira automáticamente después de 15 minutos si no se completa el pago.
    - **Unicidad de Asiento:** Un asiento no puede ser reservado por dos usuarios simultáneamente (Manejo de concurrencia mediante bloqueos en Redis y versiones optimistas en BD).
    - **Estados de Asiento:** Un asiento debe transicionar estrictamente entre los estados `available`, `reserved` y `sold`.
    - **Acceso a Boletos:** Solo se genera y permite el acceso al boleto digital una vez que el estado de la orden es `paid`.
- **Regulaciones o Normativas:**
    - **Protección de Datos:** Consideración de leyes de privacidad para el manejo de correos electrónicos de clientes.
    - **Integridad de Datos:** Aislamiento de datos por microservicio mediante el uso de esquemas dedicados (`bc_<name>`) en una base de datos compartida.

## 4. Perfiles de Usuario y Roles
- **Perfiles o Roles de Usuario en el Sistema:**
    - **Cliente (Customer):** Usuario final que navega, reserva y compra boletos.
    - **Organizador/Administrador (Admin):** Gestiona la creación de eventos, configuración de recintos y visualización de métricas en el dashboard.
- **Permisos y Limitaciones de Cada Perfil:**
    - **Cliente:** No tiene acceso al panel de administración ni puede crear o editar eventos.
    - **Admin:** Posee acceso total a la gestión del catálogo y configuración de asientos, pero no interviene en el flujo de pago directo del cliente.

## 5. Condiciones del Entorno Técnico
- **Plataformas Soportadas:**
    - **Web:** Aplicación desarrollada en Next.js (React) con soporte responsivo para móviles.
- **Tecnologías o Integraciones Clave:**
    - **Backend:** .NET 8 con Arquitectura Hexagonal.
    - **Base de Datos:** PostgreSQL (con esquemas por servicio) y Redis (para caché y bloqueos).
    - **Mensajería:** Kafka (para comunicación asíncrona entre servicios).
    - **Frontend:** Next.js, Tailwind CSS y componentes de Shadcn UI.
    - **Contenerización:** Docker y Docker Compose para el entorno de desarrollo e infraestructura.

## 6. Casos Especiales o Excepciones
- **Expiración de Pago:** Si el pago se confirma segundos después de que la reserva expiró, el sistema debe ejecutar una lógica de compensación o fallo seguro.
- **Fallo en Generación de QR:** En caso de error técnico al generar el PDF, la orden debe quedar en un estado pendiente de reintento o notificación a soporte técnico.
- **Alta Concurrencia:** Durante lanzamientos de eventos populares, el sistema debe garantizar que el bloqueo de asientos sea consistente incluso bajo miles de peticiones simultáneas.