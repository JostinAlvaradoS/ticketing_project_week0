# Especificación de Pruebas de Sistema (System Testing)

A diferencia de las pruebas E2E (que validan el viaje del usuario), las **Pruebas de Sistema** en esta arquitectura de microservicios se enfocan en validar que el sistema completo cumple con los requisitos funcionales y no funcionales bajo su configuración final.

## 1. Alcance de la Prueba de Sistema
- **Integridad de Infraestructura**: Verificación de que todos los "sidecars" (Kafka, Redis, Postgres) están operando y accesibles por los microservicios.
- **Resiliencia de Red**: Validación de que los servicios pueden comunicarse entre sí a través de sus nombres de red de Docker.
- **Consistencia de Datos (Cross-Service)**: Asegurar que los esquemas de base de datos (`bc_catalog`, `bc_ordering`, etc.) mantienen la integridad referencial lógica (aunque sean bases separadas).
- **Manejo de Transacciones Distribuidas**: Validar que la coreografía de eventos no deja el sistema en estados inconsistentes ante fallos parciales.

## 2. Casos de Prueba de Sistema (System Test Suite)

### ST-01: Verificación de Topología y Conectividad
- **Objetivo**: Asegurar que el "Service Discovery" (vía DNS de Docker) funciona.
- **Acción**: Ejecutar comandos `ping` y `curl` desde el interior de un contenedor hacia otros.
- **Resultado Esperado**: Resolución de nombres exitosa (ej. `catalog` resuelve a su IP interna).

### ST-02: Integridad de Esquemas y Seed Data
- **Objetivo**: Validar que la base de datos `ticketing` tiene todas las tablas y permisos necesarios.
- **Acción**: Consultar `information_schema` para asegurar que las tablas de cada microservicio existen.
- **Resultado Esperado**: Presencia de esquemas `bc_catalog`, `bc_inventory`, `bc_ordering`, `bc_payment`.

### ST-03: Validación de Broker de Mensajería (Kafka)
- **Objetivo**: Confirmar que los tópicos necesarios existen y son accesibles.
- **Acción**: Listar tópicos desde el contenedor `speckit-kafka`.
- **Resultado Esperado**: Existencia de `payment-succeeded`, `payment-failed`, `order-created`.

### ST-04: Prueba de Persistencia y Caché (Redis)
- **Objetivo**: Validar que el almacenamiento de estado temporal funciona.
- **Acción**: Intentar escribir y leer una clave de prueba en Redis.
- **Resultado Esperado**: Lectura exitosa del valor persistido.
