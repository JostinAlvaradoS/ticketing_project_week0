---
title: Infraestructura y Despliegue
description: Docker Compose, dependencias, red interna, volúmenes y guía de inicio rápido
---

# Infraestructura y Despliegue

## Visión General

Todo el sistema corre sobre Docker Compose. Un solo comando desde el directorio `infra/` levanta los 12 contenedores: 4 de infraestructura base y 8 microservicios.

No se requieren archivos `.env` — todas las configuraciones están embebidas en `docker-compose.yml` y `appsettings.json` para simplificar el levantamiento en contexto de entrenamiento.

---

## Contenedores

### Infraestructura Base

| Servicio | Imagen | Puerto (host:container) | Propósito |
|---------|--------|--------------------------|-----------|
| `speckit-postgres` | postgres:17 | `5432:5432` | Base de datos principal |
| `speckit-redis` | redis:7 | `6379:6379` | Locks distribuidos |
| `speckit-zookeeper` | confluentinc/cp-zookeeper:7.5.0 | `2181:2181` | Coordinación Kafka |
| `speckit-kafka` | confluentinc/cp-kafka:7.5.0 | `9092:9092` | Mensajería asíncrona |
| `kafka-init` | confluentinc/cp-kafka:7.5.0 | — | Crea topics al inicio (one-shot) |

### Microservicios (.NET 9)

| Servicio | Puerto (host:container) | Imagen |
|---------|--------------------------|--------|
| `identity` | `50000:5000` | Dockerfile en `services/identity/` |
| `catalog` | `50001:5001` | Dockerfile en `services/catalog/` |
| `inventory` | `50002:5002` | Dockerfile en `services/inventory/` |
| `ordering` | `5003:5003` | Dockerfile en `services/ordering/` |
| `payment` | `5004:5004` | Dockerfile en `services/payment/` |
| `fulfillment` | `50004:5004` | Dockerfile en `services/fulfillment/` |
| `notification` | `50005:5005` | Dockerfile en `services/notification/` |
| `waitlist` | `5006:5006` | Dockerfile en `services/waitlist/` |

---

## Secuencia de Inicio

El `docker-compose.yml` define `depends_on` para garantizar el orden correcto:

```
1. postgres + redis + zookeeper (sin dependencias)
        │
2. kafka (espera: zookeeper healthy)
        │
3. kafka-init (espera: kafka healthy)
   └── Crea topics:
       - reservation-created
       - reservation-expired
       - payment-succeeded
       - payment-failed
       - ticket-issued
       - seats-generated
        │
4. Todos los microservicios (esperan: kafka-init completed)
   └── Cada servicio aplica migraciones EF Core al iniciar
```

---

## Health Checks

Cada contenedor de infraestructura tiene un health check configurado:

```yaml
# PostgreSQL
test: ["CMD-SHELL", "pg_isready -U postgres"]
interval: 10s
timeout: 5s
retries: 5

# Redis
test: ["CMD", "redis-cli", "ping"]
interval: 10s
timeout: 5s
retries: 5

# Zookeeper
test: ["CMD-SHELL", "echo ruok | nc localhost 2181 | grep imok"]
interval: 10s
timeout: 5s
retries: 5

# Kafka
test: ["CMD-SHELL", "kafka-broker-api-versions --bootstrap-server localhost:9092"]
interval: 15s
timeout: 10s
retries: 10
```

---

## Volúmenes

```yaml
volumes:
  postgres-data:     # Persistencia de PostgreSQL entre reinicios
  fulfillment-data:  # PDFs de boletos generados
```

Los datos de PostgreSQL persisten incluso al hacer `docker compose down`. Para limpiar completamente:

```bash
docker compose down -v  # Elimina también los volúmenes
```

---

## Red Interna

Todos los contenedores comparten la red `speckit-network` (bridge). Esto permite que los servicios se comuniquen usando el nombre del contenedor como hostname:

```
# Desde cualquier microservicio:
PostgreSQL:  postgres:5432
Redis:       redis:6379
Kafka:       kafka:9092
```

---

## Configuración de Base de Datos

```
Host:     postgres (Docker) / localhost (local)
Puerto:   5432
Usuario:  postgres
Password: postgres
Database: ticketing
```

Cada servicio se conecta con un connection string similar a:
```
Host=postgres;Port=5432;Database=ticketing;Username=postgres;Password=postgres;
Search Path=bc_{nombre_servicio}
```

---

## Inicio Rápido

### Levantar todo el backend:

```bash
cd infra
docker compose up -d
```

### Verificar que todo está corriendo:

```bash
docker compose ps
```

Deberías ver 12 contenedores en estado `running` (excepto `kafka-init` que termina con `exited (0)` — esto es correcto).

### Ver logs en tiempo real:

```bash
# Todos los servicios
docker compose logs -f

# Un servicio específico
docker compose logs -f catalog
docker compose logs -f inventory
```

### Levantar el frontend:

```bash
cd frontend
npm install
npm run dev
```

Acceder en: `http://localhost:3000`

---

## Comandos Útiles

```bash
# Reiniciar un servicio específico
docker compose restart catalog

# Reconstruir imagen de un servicio (tras cambios en código)
docker compose build catalog
docker compose up -d catalog

# Reconstruir todo desde cero
docker compose down
docker compose build
docker compose up -d

# Limpiar completamente (datos incluidos)
docker compose down -v
docker system prune -f
```

---

## Verificación Post-Inicio

### Verificar base de datos (schemas creados):

```bash
docker exec -it speckit-postgres psql -U postgres -d ticketing -c "\dn"
```

Deberías ver los schemas: `bc_catalog`, `bc_identity`, `bc_inventory`, `bc_notification`, `bc_ordering`, `bc_payment`, `bc_fulfillment`, `bc_waitlist`

### Verificar topics de Kafka:

```bash
docker exec -it speckit-kafka kafka-topics --bootstrap-server localhost:9092 --list
```

Deberías ver los 6 topics: `reservation-created`, `reservation-expired`, `payment-succeeded`, `payment-failed`, `ticket-issued`, `seats-generated`

### Health checks de servicios:

```bash
curl http://localhost:50001/health  # Catalog
curl http://localhost:50002/health  # Inventory
curl http://localhost:5003/health   # Ordering
curl http://localhost:5004/health   # Payment
```

---

## Troubleshooting Frecuente

### Kafka no inicia

```bash
# Verificar que Zookeeper está healthy primero
docker compose logs zookeeper

# Si hay problemas de conectividad
docker compose restart zookeeper kafka
```

### Servicio no conecta a Postgres

```bash
# Verificar que las migraciones se aplicaron
docker compose logs catalog | grep -i migration
docker compose logs catalog | grep -i error
```

### Puerto en uso

```bash
# Ver qué proceso usa un puerto
lsof -i :50001

# Cambiar el puerto en docker-compose.yml si es necesario
```
