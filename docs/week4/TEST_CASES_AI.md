# Matriz de Casos de Prueba - IA vs Refinamiento Humano

Este documento contiene los escenarios de prueba generados por la IA (**SKAI**) en lenguaje Gherkin, junto con la validación y ajustes técnicos realizados por el equipo de QA basándose en la arquitectura del proyecto (**Ticketing Platform MVP**). Hubo casos en los cuáles el refinamiento de las HU se baso en dividir sus responsabilidades, para ello se tomo solo la primera HU dividida para generarle sus test_cases respectivos.

---

## 📌 Índice de Navegación

1.  [**HU-01: Reserva Temporal**](#hu-01)
2.  [**HU-04: Exploración y Selección**](#hu-04)
3.  [**HU-05: Crear Evento (Admin)**](#hu-05)

---

## <a name="hu-01"></a>1. HU-01: Selección y Reserva Temporal de Asiento

### Escenarios Gherkin y Matriz de Ajustes (HU-01)

| ID | Escenario (Gherkin / IA) | Ajuste del Probador (QA) | ¿Por qué se ajustó? |
| :--- | :--- | :--- | :--- |
| **TC-01** | Bloqueo de asiento exitoso y visualización de temporizador. | BLOQUEO DE ASIENTO EXITOSO Y VISUALIZACIÓN DE TEMPORIZADOR VALIDANDO EN EL MICROSERVICIO DE INVENTARIO Y EMISIÓN DE EVENTO `RESERVATION-CREATED` EN KAFKA. | La IA no contempló el flujo asíncrono necesario para la reserva de asientos. |
| **TC-02** | Reserva expira después del TTL. | EXPIRACIÓN DE RESERVA VERIFICANDO LA LIMPIEZA AUTOMÁTICA DE LA LLAVE EN **REDIS** Y LA LIBERACIÓN REACTIVA DEL STOCK POR TTL TERMINADO. | Se requiere asegurar que el inventario se libere automáticamente tras la expiración del TTL. |
| **TC-03** | Concurrencia de reserva simultánea. | VALIDACIÓN DE CONCURRENCIA SIMULTÁNEA INCLUYENDO CONTROL DE **OPTIMISTIC CONCURRENCY** Y RESPUESTA TÉCNICA HTTP 409 CONFLICT EN EL API. | Es vital prevenir que dos usuarios reserven el mismo asiento al mismo tiempo. |
| **TC-04** | El cliente B ve el asiento como reservado. | SINCRONIZACIÓN DE ESTADO PARA EL CLIENTE B VALIDANDO LA PROPAGACIÓN DE EVENTOS Y ACTUALIZACIÓN EN TIEMPO REAL DEL MAPA VÍA WEBSOCKETS. | Garantiza que todos los usuarios vean el estado real del inventario sin refrescar. |
| **TC-05** | Pago completado antes de los 15 min. | DETENCIÓN DEL TEMPORIZADOR POR PAGO COMPLETADO VALIDANDO LA RECEPCIÓN DEL EVENTO `PAYMENT-SUCCEEDED` Y ELIMINACIÓN DEL TTL EN REDIS. | Previene que el asiento sea liberado por error si el pago ya fue confirmado. |
| **TC-06** | Cliente B intenta reservar asiento C de A. | RECHAZO DE RESERVA POR SOLICITUD DE TERCEROS VALIDANDO EL ESTADO DESDE EL **DISTRIBUTED LOCK** ACTIVO ANTES DE CONSULTAR LA BASE DE DATOS. | El motor de reglas debe respetar los bloqueos existentes antes de consultar la base de datos. |
| **TC-07** | Transiciones: Disponible -> Reservado -> Vendido. | VALIDACIÓN DE CICLO DE VIDA DEL ASIENTO MEDIANTE ASERCIONES DETERMINISTAS EN DB PARA LOS ESTADOS `AVAILABLE`, `RESERVED` Y `SOLD`. | Un control estricto de estados evita inconsistencias en el ciclo de vida de la entrada. |
| **TC-08** | Pago segundos después de la expiración. | RECHAZO DE PAGO TARDÍO VALIDANDO QUE EL `PAYMENT SERVICE` CONSULTE EL TTL EN REDIS ANTES DE AUTORIZAR LA TRANSACCIÓN. | Evita cobros por reservas que ya fueron liberadas y podrían haber sido tomadas por otro. |
| **TC-09** | Límite de X asientos por usuario. | VALIDACIÓN DE LÍMITE DE ASIENTOS EN EL ENTRY-POINT DEL MICROSERVICIO `ORDERING` PARA ASEGURAR LA REGLA DE NEGOCIO ANTES DEL BLOQUEO. | La lógica de negocio debe aplicarse antes de comprometer recursos de infraestructura. |
| **TC-10** | Sincronización multi-dispositivo. | GESTIÓN DE TIEMPO CENTRALIZADO VALIDANDO EL USO DEL **TIMESTAMP UTC** DEL SERVIDOR PARA LA CONSISTENCIA DEL CRONÓMETRO GLOBAL. | El tiempo de reserva debe ser absoluto y no depender del reloj local del usuario. |
| **TC-11** | Temporizador llega a 00:00. | EXPIRACIÓN AUTOMÁTICA EN 00:00 VALIDANDO EL DISPARO DEL CALLBACK DE REDIS PARA LA LIBERACIÓN ATÓMICA Y CONSISTENTE DEL STOCK. | Asegura una liberación inmediata y consistente del stock sin intervención manual. |
| **TC-12** | Cancelación manual de la reserva. | LIBERACIÓN PROACTIVA POR CANCELACIÓN VALIDANDO LA EMISIÓN INMEDIATA DEL EVENTO `RESERVATION-CANCELLED` EN EL SISTEMA. | La cancelación proactiva mejora la rotación del inventario para otros clientes. |
| **TC-13** | Recuperación ante reinicio de App. | PERSISTENCIA DE SESIÓN TRAS REINICIO VALIDANDO LA RECUPERACIÓN DEL ESTADO DE RESERVA DESDE REDIS MEDIANTE EL `ORDER_ID`. | Mejora la resiliencia del flujo de compra ante fallos de red o cierres accidentales. |
| **TC-14** | Intento de reservar asiento vendido. | BLOQUEO DE SOLICITUD PARA STOCK VENDIDO VALIDANDO EL FILTRO DE INTEGRIDAD EN EL MICROSERVICIO `CATALOG` E `INVENTORY`. | Previene peticiones innecesarias a la capa de reserva para stock ya liquidado. |
| **TC-15** | Asignación a los primeros en llegar. | PRIORIZACIÓN POR LLEGADA VALIDANDO EL ORDENAMIENTO POR TIMESTAMP EN LA PARTICIÓN CORRESPONDIENTE DE KAFKA. | En alta demanda, el orden de los mensajes en el log de Kafka define la prioridad. |
| **TC-16** | Integridad ante caída del sistema. | RESILIENCIA DE DATOS VALIDANDO LA CONFIGURACIÓN DE `ACKS=ALL` Y EL FACTOR DE REPLICACIÓN EN LA INFRAESTRUCTURA DE KAFKA. | Garantiza que ningún cambio de estado se pierda si un nodo del clúster falla. |
| **TC-17** | Cierre de sesión y re-inicio. | RECUPERACIÓN DE INTENCIÓN DE COMPRA VALIDANDO LA IDENTIFICACIÓN DEL USUARIO VÍA JWT Y RESTAURACIÓN DE SU SESIÓN EN REDIS. | La persistencia de la intención de compra es clave para la conversión de ventas. |
| **TC-18** | Reintentos rápidos (Ráfaga). | PROTECCIÓN CONTRA RÁFAGAS VALIDANDO LA IMPLEMENTACIÓN DE **IDEMPOTENCY KEY** EN LAS CABECERAS DE LA PETICIÓN HTTP. | Evita que fallos de red generen múltiples reservas para el mismo usuario y asiento. |
| **TC-19** | Zona horaria distinta. | ESTANDARIZACIÓN TEMPORAL VALIDANDO EL USO EXCLUSIVO DEL FORMATO **ISO 8601 UTC** EN TODA LA COMUNICACIÓN DEL API. | Previene errores de cálculo en el TTL cuando el cliente y el servidor difieren de zona. |
| **TC-20** | Identificador manipulado. | VALIDACIÓN DE ESQUEMA DE ENTRADA VERIFICANDO LA RESPUESTA ESTRUCTURADA BAJO EL ESTÁNDAR **RFC 7807 (PROBLEM DETAILS)**. | Seguridad básica contra manipulación manual de IDs en las peticiones de reserva. |

---

## <a name="hu-04"></a>2. HU-04: Exploración y Selección de Asientos

### Escenarios Gherkin y Matriz de Ajustes (HU-04)

| ID | Escenario (Gherkin / IA) | Ajuste del Probador (QA) | ¿Por qué se ajustó? |
| :--- | :--- | :--- | :--- |
| **TC-01** | Filtros por fecha, tipo y ubicación. | BÚSQUEDA CON FILTROS VALIDANDO LA INTEGRACIÓN DE LA CAPA DE CACHÉ (`REDIS`) PARA OPTIMIZAR LAS CONSULTAS DEL CATÁLOGO. | Para optimizar la respuesta del sistema ante ráfagas masivas de búsqueda. |
| **TC-02** | Mapa visual de asientos y estados. | VISUALIZACIÓN DE MAPA VALIDANDO LA CARGA DIFERIDA (**LAZY LOADING**) DE ASIENTOS SEGÚN EL ÁREA DE VISUALIZACIÓN DEL CLIENTE. | Evita cargar miles de asientos de golpe para estadios de gran capacidad. |
| **TC-03** | Selección de asiento disponible. | SELECCIÓN EXITOSA CONFIRMANDO QUE EL GATEWAY PROCESE LA SOLICITUD Y RETORNE UNA RESPUESTA SATISFACTORIA INMEDIATA. | El usuario debe recibir feedback instantáneo de que su selección fue procesada. |
| **TC-04** | Selección de asiento reservado. | FEEDBACK DE ASIENTO RESERVADO VALIDANDO LA RECEPCIÓN DE NOTIFICACIONES PUSH VÍA **WEBSOCKETS** PARA CAMBIOS DE ESTADO. | Permite reflejar actualizaciones del mapa sin que el usuario lo recargue manualmente. |
| **TC-05** | Selección de asiento vendido. | RESTRICCIÓN DE ASIENTO VENDIDO VALIDANDO QUE EL COMPONENTE VISUAL SE DESHABILITE Y PROHÍBA TÉCNICAMENTE LA INTERACCIÓN. | Previene clics innecesarios en elementos que ya no son accionables en el negocio. |
| **TC-06** | Dos visitantes, misma reserva. | RESOLUCIÓN DE CONCURRENCIA VALIDANDO EL USO DEL **DISTRIBUTED LOCK** EN REDIS PARA EVITAR EL "DOUBLE BOOKING". | Previene el problema del "Double Booking" a nivel de infraestructura escalable. |
| **TC-07** | Bloqueo por 15 minutos. | RESERVA TEMPORAL VALIDANDO LA CREACIÓN DE LA LLAVE CON TTL EN REDIS VINCULADA DE FORMA ÚNICA A LA SESIÓN DEL USUARIO. | Consolida el control del tiempo en el lado servidor para asegurar su invulnerabilidad. |
| **TC-08** | Liberación tras 15 minutos. | LIMPIEZA ASÍNCRONA VALIDANDO EL TRABAJO DEL WORKER QUE LIBERA EL STOCK Y ACTUALIZA REACTIVAMENTE EL MAPA. | La limpieza del inventario debe ser automática y visible instantáneamente para otros. |
| **TC-09** | Alta concurrencia en evento popular. | PRUEBAS DE ESTRÉS VALIDANDO EL PARTICIONAMIENTO DE TÓPICOS Y ESCALABILIDAD HORIZONTAL EN LA INFRAESTRUCTURA DE KAFKA. | Asegura que el flujo escale horizontalmente sin que los datos pierdan consistencia. |
| **TC-10** | Regreso al mapa, asiento reservado. | PERSISTENCIA DE SELECCIÓN VALIDANDO QUE EL ESTADO DE LA RESERVA SE MANTENGA EN LA BASE DE DATOS TRAS NAVEGAR POR LA APP. | La sesión de reserva debe mantenerse íntegra ante cambios de pantalla del usuario. |
| **TC-11** | Selección de varios asientos. | RESERVA MÚLTIPLE VALIDANDO LA ATOMICIDAD DE LA TRANSACCIÓN (TODO O NADA) PARA EVITAR BLOQUEOS PARCIALES. | Evita reservas parciales erróneas donde solo se bloquea una fracción del pedido. |
| **TC-12** | Asociación a usuario autenticado. | VINCULACIÓN DE RESERVA VALIDANDO QUE EL `USERID` SE EXTRAIGA Y VERIFIQUE CORRECTAMENTE DESDE LOS CLAIMS DEL **JWT**. | Asegura que la reserva sea atribuida correctamente al titular verificado de la cuenta. |
| **TC-13** | Invitado (No autenticado). | TRAZABILIDAD ANÓNIMA VALIDANDO EL USO DE UN TOKEN TEMPORAL VINCULADO A LA SESIÓN DEL NAVEGADOR PARA EL INVITADO. | Permite un embudo de ventas fluido sin obligar a la autenticación inmediata. |
| **TC-14** | Más de 10 asientos (Límite). | RESTRICCIÓN DE VOLUMEN VALIDANDO EL RECHAZO DE LA PETICIÓN EN EL SERVICIO DE ÓRDENES SI EL PAYLOAD EXCEDE EL MÁXIMO. | Evita que se burlen reglas de negocio enviando payloads masivos directamente al API. |
| **TC-15** | Combinación de estados en selección. | FILTRADO DE SELECCIÓN VALIDANDO QUE EL API IGNORE O RECHACE IDs DE ASIENTOS QUE NO ESTÉN EN ESTADO `AVAILABLE`. | Previene ataques de inclusión de asientos inválidos en una petición de compra. |
| **TC-16** | Filtros restrictivos (Sin resultados). | GESTIÓN DE BÚSQUEDA VACÍA VALIDANDO RESPUESTA 200 CON ARRAY VACÍO Y METADATOS INFORMATIVOS DE LA CONSULTA. | Garantiza que el sistema gestione la ausencia de resultados sin lanzar errores técnicos. |
| **TC-17** | Búsqueda > 100 caracteres. | VALIDACIÓN DE ENTRADA MASIVA VERIFICANDO LOS LÍMITES MEDIANTE **FLUENTVALIDATION** PARA PREVENIR ABUSOS DE MEMORIA. | Evita ataques de denegación de servicio por procesamiento inneceario de texto. |
| **TC-18** | Formato de fecha incorrecto en filtros. | INTEGRIDAD DE DATOS TEMPORALES VALIDANDO EL MANEJO DE TIPOS INVÁLIDOS EN LA CAPA DE MIDDLEWARE ANTES DE CONSULTAR DB. | Asegura que el microservicio no intente procesar datos erróneos contra la base de datos. |
| **TC-19** | Error técnico de integridad. | ESTANDARIZACIÓN DE ERRORES VALIDANDO QUE LAS RESPUESTAS TÉCNICAS SIGAN EL PROTOCOLO **RFC 7807 (PROBLEM DETAILS)**. | Unifica el formato de errores para facilitar la integración por parte del equipo Frontend. |
| **TC-20** | Acceso anónimo al catálogo. | ACCESO PÚBLICO VALIDANDO LAS POLÍTICAS DE **CORS** Y EL DECORADOR `ALLOWANONYMOUS` EN LOS ENDPOINTS DEL GATEWAY. | Permite que el catálogo de eventos sea público y accesible de forma rápida. |

---

## <a name="hu-05"></a>3. HU-05: Gestión de Catálogo por Organizador

### Escenarios Gherkin y Matriz de Ajustes (HU-05)

| ID | Escenario (Gherkin / IA) | Ajuste del Probador (QA) | ¿Por qué se ajustó? |
| :--- | :--- | :--- | :--- |
| **TC-01** | Creación exitosa con ID único. | CREACIÓN DE EVENTO VALIDANDO LA GENERACIÓN AUTOMÁTICA DE UN **UUID V4** EN EL SERVICIO DE CATÁLOGO. | Un ID universal asegura la consistencia entre microservicios sin colisiones. |
| **TC-02** | Campos obligatorios vacíos. | INTEGRIDAD DE CAMPOS OBLIGATORIOS VALIDANDO EL ESQUEMA MEDIANTE **DATA ANNOTATIONS** EN EL SERVIDOR. | La integridad de datos debe ser validada en servidor para evitar registros corruptos. |
| **TC-03** | Nombre excede 256 caracteres. | RESTRICCIÓN DE LONGITUD DE NOMBRE VALIDANDO EL COMPORTAMIENTO DEL CAMPO `VARCHAR(256)` EN POSTGRESQL. | Sincroniza la capacidad física de la base de datos con los límites de negocio. |
| **TC-04** | Caracteres especiales no permitidos. | SEGURIDAD DE ENTRADA VALIDANDO LA **SANITIZACIÓN DE INPUTS** ANTES DE LA PERSISTENCIA DEL EVENTO. | Seguridad: Previene el uso de scripts en campos de texto gestionados por el admin. |
| **TC-05** | Fecha pasada o inválida. | CRONOLOGÍA DE EVENTO VALIDANDO QUE EL SISTEMA COMPARE LA FECHA CONTRA EL TIEMPO UTC DEL SERVIDOR. | Evita la creación de eventos en el pasado que ensucien el historial. |
| **TC-06** | Recinto inexistente. | INTEGRIDAD DE LUGAR VALIDANDO LA CLAVE FORÁNEA CONTRA LA TABLA DE SEDES (`VENUES`) EN LA DB. | Todo evento oficial debe ocurrir en un lugar válido y registrado previamente. |
| **TC-07** | Conflicto de ID (Duplicidad). | RECHAZO DE DUPLICADOS VALIDANDO LA CAPTURA DE EXCEPCIONES DE **UNIQUE CONSTRAINT** EN LA BASE DE DATOS. | El sistema debe rechazar de forma segura cualquier intento de registro duplicado. |
| **TC-08** | Valor mínimo en cada campo. | ANALISIS DE VALORES LÍMITE (BVA) VALIDANDO EL COMPORTAMIENTO EN LA CAPA DE PERSISTENCIA ANTE DATOS MÍNIMOS. | Asegura que el catálogo no procese valores nulos o simbólicamente inválidos. |
| **TC-09** | Cancelar operación (Descartar). | DESCARTE DE FLUJO VALIDANDO QUE LA ACCIÓN DE CANCELAR NO GATILLE LLAMADAS AL COMANDO POST EN EL API. | Confirmación técnica de que la UI no gatille efectos secundarios no deseados. |
| **TC-10** | Cliente intenta crear evento (Permisos). | SEGURIDAD DE ROLES VALIDANDO LA POLÍTICA `ADMINONLY` MEDIANTE LOS CLAIMS DEL **JWT** EN EL ENDPOINT. | El API debe ser infranqueable para usuarios sin privilegios administrativos. |
| **TC-11** | Falla técnica (Pérdida conexión). | RESILIENCIA DE DATOS VALIDANDO EL ALCANCE DE TRANSACCIONES ATÓMICAS PARA ASEGURAR CONSISTENCIA PARCIAL. | Evita registros parciales en el sistema si ocurre un error de red o de DB. |
| **TC-12** | Creación simultánea por 2 Admins. | CONCURRENCIA ADMISTRATIVA VALIDANDO QUE EL SISTEMA EMPLEE GENERADORES DE IDs LIBRES DE COLISIONES. | Garantiza la unicidad de registros incluso si ocurren peticiones al mismo segundo. |
| **TC-13** | Correo de organizador inválido. | INTEGRIDAD DE NOTIFICACIÓN VALIDANDO EL FORMATO DE CORREO MEDIANTE EXPRESIONES REGULARES EN EL BACKEND. | Asegura que las comunicaciones del sistema lleguen a un destino correcto. |
| **TC-14** | Verificación en base de datos. | INTEGRIDAD FÍSICA VALIDANDO QUE EL MAPEO ENTRE OBJETO DE NEGOCIO Y ESQUEMA FÍSICO SEA EXACTO. | Garantiza que no exista pérdida de precisión según el tipo de dato persistido. |

---

## 4. Notas Técnicas de QA
- **Técnicas ISTQB aplicadas:** Partición de Equivalencia, Valores Límite, Transición de Estados y Pruebas de Error.
- **Arquitectura:** Se validó contra .NET 8, Kafka, Redis y PostgreSQL.
- **Recomendación:** Implementar tests automatizados usando **Serenity BDD + RestAssured** para las capas de API.