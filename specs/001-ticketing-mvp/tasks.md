```markdown
# Tasks: Ticketing Platform MVP (Purchase Flow)

Notes:
- Formato: `- [ ] T### Descripción (Prioridad Px, Est: Yh) [Dependencias: Txxx]`
- Prioridad: P0 (foundation/infrastructure), P1 (P1 user story work), P2 (optional/backlog)

---

## Phase 0 — Foundation (infra, schemas, identity)

- [X] T001 Crear `infra/docker-compose.yml` con `postgres`, `redis`, `zookeeper`, `kafka` y healthchecks (Prioridad P0, Est: 4h)
- [X] T002 Añadir volumen y `infra/db/init-schemas.sql` que crea schemas `bc_identity, bc_catalog, bc_inventory, bc_ordering, bc_payment, bc_fulfillment, bc_notification` (P0, Est: 2h)
- [X] T003 Añadir script de inicialización en compose para ejecutar `init-schemas.sql` al arrancar Postgres (P0, Est: 1h) [Dependencias: T001, T002]
- [X] T004 Crear README ops corto describiendo connection string y roles por schema (P0, Est: 1h) [Dependencias: T002]
- [X] T005 Crear Identity Service skeleton (services/identity/src) - Minimal API con endpoints `/health` y `/token` (dev JWT) (P0, Est: 6h) [Dependencias: T001]
- [X] T006 Añadir EF Core `DbContext` base en `services/identity/src/Infrastructure` y migration inicial en `migrations/identity` (P0, Est: 3h) [Dependencias: T002, T005]
- [X] T007 Crear CI job (GitHub Action) para levantar infra con Testcontainers y ejecutar smoke migrations (P0, Est: 4h) [Dependencias: T001, T003]
- [X] T008 Crear contratos iniciales (Carpeta `contracts/openapi`) placeholder para `identity`, `catalog`, `inventory`, `ordering`, `payment`, `fulfillment` (P0, Est: 3h)
- [X] T009 Crear carpeta `contracts/kafka/` y añadir JSON schemas iniciales: `reservation-created.json`, `reservation-expired.json`, `payment-succeeded.json`, `payment-failed.json`, `ticket-issued.json` (P0, Est: 3h)
- [X] T010 Smoke test infra: ejecutar `docker compose` y verificar `postgres`, `redis`, `kafka` y `identity` health endpoints (P0, Est: 2h) [Dependencias: T001, T005]

---

## Phase 1 — Core services (Catalog, Inventory, Ordering)

Catalog (read side)
- [ ] T011 Crear proyecto `services/catalog/src` con estructura Hexagonal (Domain/Application/Infrastructure/Api) (P1, Est: 2h) [Dependencias: T005]
- [ ] T012 Crear `CatalogDbContext` configurado con schema `bc_catalog` y migration inicial en `migrations/catalog` (P1, Est: 3h) [Dependencias: T002]
- [ ] T013 Implementar endpoint `GET /events/{id}/seatmap` en `catalog` que retorna seats & base prices (P1, Est: 6h) [Dependencias: T011, T012]
- [ ] T014 Añadir unit tests domain para `Event` y `Seat` agregados (xUnit) (P1, Est: 3h) [Dependencias: T011]

Inventory (reservation core)
- [ ] T015 Crear proyecto `services/inventory/src` con estructura Hexagonal (P1, Est: 2h) [Dependencias: T005]
- [ ] T016 Crear `InventoryDbContext` con schema `bc_inventory` y migration inicial (P1, Est: 3h) [Dependencias: T002]
- [ ] T017 Crear entidad `Seat` y columna `version` (rowversion/timestamp) en `bc_catalog`/`bc_inventory` modelo (P1, Est: 3h) [Dependencias: T012, T016]
- [ ] T018 Implementar Redis lock helper (adapter) en `Infrastructure` (StackExchange.Redis) (P1, Est: 4h) [Dependencias: T001]
- [ ] T019 Implementar endpoint `POST /reservations` (Inventory) que: adquiere Redis lock, verifica seat version, crea `Reservation` con `expires_at = now + 15m`, actualiza seat status `reserved`, publica `reservation-created` en Kafka (P1, Est: 12h) [Dependencias: T016, T017, T018, T009]
- [ ] T020 Implementar background worker en Inventory para expirar reservations (cron/poll every minute) y publicar `reservation-expired` (P1, Est: 6h) [Dependencias: T019]
- [ ] T021 Unit tests para lógica de reserva y expiración (P1, Est: 6h) [Dependencias: T019, T020]
- [ ] T022 Integration test (Testcontainers) que simula dos clientes intentando reservar el mismo seat y verifica sólo 1 reserva se crea (P1, Est: 10h) [Dependencias: T010, T019]

Ordering (cart + order persistence)
- [ ] T023 Crear proyecto `services/ordering/src` con estructura Hexagonal (P1, Est: 2h) [Dependencias: T005]
- [ ] T024 Crear `OrderingDbContext` con schema `bc_ordering` y migration inicial (P1, Est: 3h) [Dependencias: T002]
- [ ] T025 Implementar modelo `Order` y endpoints: `POST /cart/add`, `POST /orders/checkout` (P1, Est: 10h) [Dependencias: T023, T024, T019]
- [ ] T026 Implementar listeners/validation in Ordering to validate reservation existence on add-to-cart (consumes `reservation-created` o consulta directa a Inventory) (P1, Est: 6h) [Dependencias: T019, T023]
- [ ] T027 Unit tests for Ordering domain and cart flows (P1, Est: 6h) [Dependencias: T025]
- [ ] T028 Integration test: full flow reserve → add to cart → create order draft (Testcontainers) (P1, Est: 8h) [Dependencias: T022, T025]

- [ ] T029 Phase 1 smoke test: run infra + catalog + inventory + ordering, execute end-to-end reservation → cart scenario (P1, Est: 3h) [Dependencias: T010, T013, T019, T025]

---

## Phase 2 — Payment (simulated), Fulfillment, Notification

Payment (simulado)
- [ ] T030 Crear proyecto `services/payment/src` (P1, Est: 2h) [Dependencias: T005]
- [ ] T031 Crear `PaymentDbContext` with schema `bc_payment` and initial migration (P1, Est: 3h) [Dependencias: T002]
- [ ] T032 Implementar endpoint `POST /payments` que valida order state, re-checks reservation, simula charge, persiste Payment record, y publica `payment-succeeded` o `payment-failed` (P1, Est: 10h) [Dependencias: T025, T019]
- [ ] T033 Unit tests for Payment service (P1, Est: 4h) [Dependencias: T032]

Fulfillment & Ticketing
- [ ] T034 Crear proyecto `services/fulfillment/src` (P1, Est: 2h) [Dependencias: T005]
- [ ] T035 Crear `FulfillmentDbContext` schema `bc_fulfillment` + migration (P1, Est: 3h) [Dependencias: T002]
- [ ] T036 Implementar consumer para `payment-succeeded` → crea `Ticket` entidad, genera PDF+QR (QRCoder + PdfSharpCore), guarda `ticket_pdf_path`, publica `ticket-issued` (P1, Est: 12h) [Dependencias: T032]
- [ ] T037 Unit tests for Ticket generation (mock PDF/QR libs) (P1, Est: 4h) [Dependencias: T036]

Notification
- [ ] T038 Crear proyecto `services/notification/src` con consumer de `ticket-issued` y envío via SMTP adapter (dev) (P1, Est: 6h) [Dependencias: T036]
- [ ] T039 Unit tests for Notification consumer (P1, Est: 3h) [Dependencias: T038]
- [ ] T040 Integration test: end-to-end purchase flow (reserve → cart → payment → ticket → email queued) usando Testcontainers (P1, Est: 12h) [Dependencias: T028, T032, T036, T038]
- [ ] T041 Phase 2 smoke test: run full infra + services and execute a successful payment → ticket issuance scenario (P1, Est: 3h) [Dependencias: T040]

---

## Phase 3 — Polish & Hardening

- [ ] T042 Add OpenAPI contract files para endpoints implementados y colocarlos en `contracts/openapi/` (P2, Est: 6h) [Dependencias: T013, T019, T025, T032, T036]
- [ ] T043 Añadir validación de schemas Kafka en tests de integración (cargar `/contracts/kafka/*.json` y validar mensajes) (P2, Est: 6h) [Dependencias: T009, T022, T040]
- [ ] T044 Implementar observability completo (trace propagation, Jaeger, metrics) across services (P2, Est: 8h) [Dependencias: T005]
- [ ] T045 Añadir CI contract tests y ejecutar suite de integración en pipeline (P2, Est: 8h) [Dependencias: T007, T042]
- [ ] T046 Implementar Outbox pattern para 1 servicio (inventory u ordering) como experimento (P2, Est: 16h) [Dependencias: T022]
- [ ] T047 Security hardening: forzar validación JWT y añadir rate limiting al endpoint de reservas (P2, Est: 6h) [Dependencias: T005, T019]
- [ ] T048 Realizar cargas/chaos test para la ruta de reserva y evaluar double-sell bajo presión (P2, Est: 12h) [Dependencies: T022, T040]
- [ ] T049 Final E2E smoke test y actualización de docs (P2, Est: 4h) [Dependencias: T041, T044]

---

## Notes on dependencies and estimates
- Estimates are rough and assume one developer familiar with .NET and project conventions.
- Marcar tareas como completadas secuencialmente; dividir en subtareas más pequeñas según se avance.

```
