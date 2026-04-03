# Informe de Refinamiento de Historias de Usuario

Este informe presenta el análisis comparativo entre las Historias de Usuario (HU) originales y su versión refinada mediante la herramienta **SKAI**, aplicando los principios **INVEST** y el contexto del proyecto **Ticketing Platform MVP**.

---

## 📌 Índice de Navegación

1.  **Comparativa de Refinamiento (Antes vs Después)**
    *   Flujo de Compra E2E (HU-01 a HU-03)
    *   Exploración y Descubrimiento (HU-04)
    *   Gestión de Catálogo (HU-05 a HU-08)
2.  **Complemento documental de historias refinadas y casos de prueba**

---

## 1. Comparativa de Refinamiento (Antes vs Después)

### 1.1. Flujo de Compra E2E (Épica)

| **HU Original** | **HU Refinada por SKAI (Instrucción)** | **Diferencias Detectadas e Impacto** |
| :--- | :--- | :--- |
| "Como cliente, quiero seleccionar un asiento específico, reservarlo temporalmente, agregarlo a mi carrito y completar el pago para recibir un boleto válido con código QR y confirmación por correo electrónico" | **Se desglosó en 3 Historias Atómicas:**<br><br>**[Historia de Usuario 1: Selección y Reserva Temporal de Asiento](#historia-de-usuario-1-selección-y-reserva-temporal-de-asiento)**<br>Como Cliente, quiero seleccionar un asiento específico disponible en el mapa de un evento y reservarlo temporalmente, para asegurar que el asiento esté bloqueado a mi nombre mientras decido completar la compra.<br><br>**[Historia de Usuario 2: Agregar Asiento Reservado al Carrito y Proceso de Pago](#historia-de-usuario-2-agregar-asiento-reservado-al-carrito-y-proceso-de-pago)**<br>Como Cliente, quiero agregar mi asiento reservado al carrito y realizar el pago correspondiente, para completar la compra y asegurar el acceso al evento.<br><br>**[Historia de Usuario 3: Generación de Boleto Digital y Confirmación](#historia-de-usuario-3-generación-de-boleto-digital-y-confirmación)**<br>Como Cliente, quiero recibir un boleto digital con código QR y confirmación de mi compra, para poder acceder al evento de forma segura y tener constancia de mi transacción. | 1. **Ambigüedad y Granularidad (INVEST):** Se corrigió la mezcla de acciones inconexas (reserva, pago, emisión) en un solo enunciado, permitiendo que cada fase sea **Independiente** y **Estimable**. El impacto es un flujo de desarrollo incremental y testeable.<br>2. **Reglas de Dominio Críticas:** Se integró el concepto de **TTL de 15 min** y la gestión de concurrencia ("Double Booking") que la versión original ignoraba, mitigando fallos críticos de integridad en el inventario.<br>3. **Sincronización vs. Alcance MVP:** Se alineó el impacto de las notificaciones (QR/Email) como procesos diferidos, evitando que el usuario espere tiempos de red externos y clarificando el comportamiento ante errores técnicos reales. |

---

### 1.2. Exploración y Descubrimiento

| **HU Original** | **HU Refinada por SKAI (Instrucción)** | **Diferencias Detectadas e Impacto** |
| :--- | :--- | :--- |
| "Como visitante, quiero explorar eventos, lugares y mapas de asientos para poder encontrar eventos y elegir asientos para reservar." | **[HU-04: Exploración y Selección de Asientos para Reserva](#historia-de-usuario-4-exploración-y-selección-de-asientos-para-reserva-en-eventos)**<br><br>**Como** Visitante (autenticado o no), **quiero** explorar el catálogo de eventos con filtros y mapas interactivos, **para** encontrar eventos de interés y seleccionar asientos específicos para reservarlos. | 1. **Clarificación de Perfiles y Accesibilidad:** Se definió el rol "Visitante" (anónimo vs. registrado), permitiendo diseñar políticas de **CORS** y seguridad específicas para la fase de descubrimiento sin obligar al login prematuro.<br>2. **Alcance Funcional Deteriorado (INVEST):** La versión original carecía de detalles medibles. Se desglosó el alcance en filtros (fecha, categoría, rango de precios) y niveles de visualización, permitiendo una arquitectura de búsqueda optimizada que soporta paginación y caché.<br>3. **Flujo de Pre-selección e Interacción:** Se resolvió la ambigüedad sobre cómo el usuario interactúa con el mapa. El impacto es una UI que permite pre-seleccionar asientos y visualizar disponibilidad en tiempo real, facilitando la transición al flujo de reserva una vez que el usuario toma una decisión. |

---

### 1.3. Gestión de Catálogo por Organizador

| **HU Original** | **HU Refinada por SKAI (Instrucción)** | **Diferencias Detectadas e Impacto** |
| :--- | :--- | :--- |
| "Como organizador, quiero crear eventos y configurar los asientos del lugar para que se puedan vender entradas para mis eventos." | **[Título: Creación de eventos y configuración de asientos](#historia-de-usuario-5-creación-de-eventos-y-configuración-de-asientos)**<br><br>Como organizador, quiero crear eventos y configurar los asientos del lugar para que se puedan vender entradas para mis eventos. Esto implica definir el evento con datos básicos (nombre, fecha, recinto, tipo), asignar mapas visuales de asientos con zonas, precios y disponibilidad, y garantizar que la configuración cumpla con reglas de negocio como la unicidad de asientos y la integridad de datos. | 1. **Robustez e Integridad (Testeabilidad):** Se definieron restricciones físicas (VARCHAR, tipos de datos) y reglas de unicidad, asegurando que el sistema rechace registros inconsistentes o duplicados.<br>2. **Bloqueo Operativo:** Se introdujo la restricción de edición para eventos con reservas activas, impactando directamente en la estabilidad operativa y evitando que cambios del admin alteren transacciones en curso.<br>3. **Identificación Unívoca:** Se especificó el uso de **UUID v4**, garantizando la trazabilidad del evento en toda la infraestructura de microservicios. 

**NOTA**: Como equipo tuvimos la conclusión de que SKAI tuvo una alucinación en esta parte ya que si en teoria debiamos cumplir con los principios invest, en este caso  refina la HU a una mucho más grande, por lo que no nos pareció que haya dado una respuesta óptima en esta parte.|

---

## 2. Complemento documental de historias refinadas y casos de prueba

Esta sección reúne el desarrollo narrativo de las historias refinadas y algunos casos de prueba representativos del proyecto.

### 2.1. Desarrollo narrativo de historias refinadas

#### 1.1. Historias derivadas del Flujo de Compra E2E (Épica Original)

A continuación, se presentan las tres historias de usuario atómicas resultantes del desglose de la épica original de compra, aplicando las recomendaciones de Claridad e INVEST:

---

#### Historia de Usuario 1: Selección y Reserva Temporal de Asiento

**Título:** Selección y Reserva Temporal de Asiento Específico

**Como** Cliente  
**Quiero** seleccionar un asiento específico disponible en el mapa de un evento y reservarlo temporalmente  
**Para** asegurar que el asiento esté bloqueado a mi nombre mientras decido completar la compra.

**Criterios de Aceptación:**
1. El cliente puede visualizar el mapa de asientos de un evento con estados actualizados (`disponible`, `reservado`, `vendido`).
2. Al seleccionar un asiento disponible, el sistema lo reserva exclusivamente para el cliente durante 15 minutos (TTL).
3. Si el cliente no completa el proceso de compra en ese tiempo, el asiento se libera automáticamente y vuelve a estar disponible.
4. Si dos clientes intentan seleccionar el mismo asiento simultáneamente, solo el primero en confirmar la selección lo reserva; el segundo recibe un mensaje de no disponibilidad.
5. El sistema muestra un temporizador con el tiempo restante de la reserva.

---

#### Historia de Usuario 2: Agregar Asiento Reservado al Carrito y Proceso de Pago

**Título:** Agregar Asiento Reservado al Carrito y Realizar Pago

**Como** Cliente  
**Quiero** agregar mi asiento reservado al carrito y realizar el pago correspondiente  
**Para** completar la compra y asegurar el acceso al evento.

**Criterios de Aceptación:**
1. El cliente puede agregar uno o varios asientos reservados a su carrito.
2. El sistema no permite agregar asientos cuyo tiempo de reserva ha expirado.
3. El cliente puede ingresar los datos necesarios para el pago y confirmar la transacción.
4. Si el pago se realiza dentro del TTL, el asiento pasa a estado `vendido`.
5. Si el pago se procesa después de la expiración de la reserva, el sistema muestra un mensaje de error y no finaliza la compra.
6. En caso de fallo en el pago, el asiento permanece reservado hasta que expire el TTL.

---

#### Historia de Usuario 3: Generación de Boleto Digital y Confirmación

**Título:** Emisión de Boleto Digital con QR y Confirmación de Compra

**Como** Cliente  
**Quiero** recibir un boleto digital con código QR y confirmación de mi compra  
**Para** poder acceder al evento de forma segura y tener constancia de mi transacción.

**Criterios de Aceptación:**
1. Una vez confirmado el pago, el sistema genera un boleto digital en PDF con un código QR único.
2. El boleto se asocia únicamente al cliente y al asiento comprado.
3. El sistema almacena el correo electrónico del cliente para el envío posterior de la confirmación (el envío se realiza en un proceso diferido, no en tiempo real).
4. Si ocurre un error en la generación del boleto digital, la orden queda en estado pendiente y se notifica a soporte técnico.
5. El cliente puede visualizar un mensaje de confirmación en pantalla y descargar el boleto desde su perfil.

---

#### Notas Generales del Desglose:
- Estas tres historias (HU-01 a HU-03) cubren de forma atómica lo que originalmente era una sola épica compleja.
- Cada historia incluye título, descripción y criterios de aceptación alineados con las reglas del negocio y el alcance (MVP).
- Se recomienda detallar más los flujos alternativos y excepciones en las historias técnicas o casos de prueba.

---

#### Historia de Usuario 4: Exploración y selección de asientos para reserva en eventos

**Título:** Exploración y selección de asientos para reserva en eventos

**Descripción:**  
Como visitante (usuario no autenticado o registrado), quiero explorar el catálogo de eventos, visualizar detalles de los recintos y acceder a mapas interactivos de asientos para poder encontrar eventos de interés y seleccionar asientos específicos para reservarlos de manera temporal.

**Criterios de Aceptación:**
1. El usuario puede visualizar un catálogo de eventos, con opciones de filtrado por fecha, categoría, ubicación y rango de precios.
2. Al seleccionar un evento, el usuario visualiza información relevante del evento (nombre, fecha, lugar, precio y descripción) y los detalles del recinto.
3. El usuario puede acceder a un mapa interactivo de asientos del evento seleccionado, donde se muestra la disponibilidad en tiempo real (asientos disponibles, reservados y vendidos).
4. El usuario puede seleccionar uno o varios asientos (hasta un máximo de 6 por evento) y añadirlos a una pre-reserva.
5. Al seleccionar un asiento, el sistema bloquea dicho asiento para el usuario durante 15 minutos (TTL) y muestra una cuenta regresiva visible.
6. Si el usuario no completa la compra en el tiempo asignado, la pre-reserva se libera automáticamente y los asientos vuelven a estar disponibles.
7. Si varios usuarios intentan reservar el mismo asiento simultáneamente, solo el primero en seleccionarlo lo bloquea; los demás reciben una notificación de que el asiento ya no está disponible.
8. El sistema debe mostrar mensajes claros en caso de expiración, errores de concurrencia o cambios en la disponibilidad de asientos.
9. La funcionalidad debe estar disponible tanto para usuarios autenticados como no autenticados. Si el usuario no está autenticado y desea proceder a la compra, se le solicita iniciar sesión o registrarse.

**Notas técnicas y de negocio:**
- La visualización de mapas de asientos debe ser responsiva y accesible desde dispositivos móviles.
- Deben cumplirse las reglas de negocio del TTL de la reserva y la unicidad de asientos.
- El almacenamiento y gestión de las reservas respetan las restricciones de protección de datos.

#### Historia de Usuario 5: Creación de eventos y configuración de asientos

**Título:** Creación de eventos y configuración de asientos

**Descripción:**  
Como organizador, quiero crear eventos y configurar los asientos del lugar para que se puedan vender entradas para mis eventos. Esto implica definir el evento con datos básicos (nombre, fecha, recinto, tipo), asignar mapas visuales de asientos con zonas, precios y disponibilidad, y garantizar que la configuración cumpla con reglas de negocio como la unicidad de asientos y la integridad de datos.

**Criterios de Aceptación:**  
1. **Dado** que soy un organizador,  
   **cuando** creo un evento,  
   **debo** poder ingresar los siguientes datos: nombre del evento, fecha, tipo de evento, recinto, y capacidad máxima.  
   **y** debo poder definir zonas con mapas visuales de asientos, asignando precios a cada zona y asegurando la disponibilidad de asientos.  

2. **Dado** que he creado un evento,  
   **cuando** configuro los asientos,  
   **debo** poder editar la configuración posterior (zonas, precios, disponibilidad).  
   **y** la configuración debe cumplir con la regla de unicidad de asientos (ningún asiento puede ser reservado por dos usuarios simultáneamente).  

3. **Dado** que he configurado los asientos,  
   **cuando** guardo la configuración,  
   **debo** recibir una confirmación si todo es exitoso,  
   **y** una notificación de error si hay problemas (por ejemplo, violación de reglas de negocio o errores técnicos).  

4. **Dado** que he configurado los asientos,  
   **cuando** intento reservar un asiento,  
   **debo** ver el mapa visual con las zonas y precios correspondientes,  
   **y** los asientos disponibles deben mostrarse correctamente.  

---