---
title: Frontend — Next.js Application
description: Interfaz de usuario de la plataforma SpecKit Ticketing
---

# Frontend — Next.js Application

## Propósito

El frontend es la capa de presentación del sistema. Permite a los usuarios explorar eventos, seleccionar asientos desde un mapa interactivo, completar el checkout y a los administradores gestionar el catálogo de eventos.

Está construido con Next.js 14 App Router y se comunica directamente con los microservicios del backend mediante llamadas REST.

---

## Stack Técnico

| Componente | Tecnología |
|-----------|-----------|
| Framework | Next.js 14 (App Router) |
| Lenguaje | TypeScript |
| Estilos | TailwindCSS 4.2 + Shadcn/UI |
| Forms | React Hook Form + Zod |
| Gráficas | Recharts (dashboard admin) |
| HTTP | `fetch` nativo (API Routes) |
| Puerto | `3000` (dev server) |

---

## Estructura de Páginas

```
app/
├── page.tsx                        # Home — listado de eventos
├── layout.tsx                      # Layout raíz (navbar, providers)
│
├── events/[eventId]/
│   └── page.tsx                    # Detalle de evento + mapa de asientos
│
├── checkout/
│   └── page.tsx                    # Resumen de orden + pago
│
└── admin/
    ├── layout.tsx                  # Layout admin (protegido por JWT)
    ├── login/page.tsx              # Login de administrador
    ├── dashboard/page.tsx          # Métricas y analíticas
    └── events/
        ├── page.tsx                # Listado de eventos (admin)
        ├── create/page.tsx         # Crear nuevo evento
        └── [eventId]/
            ├── page.tsx            # Detalle evento (admin)
            ├── edit/page.tsx       # Editar evento
            └── seats/page.tsx      # Configurar y generar asientos
```

---

## Estructura de Componentes

```
components/
├── navbar.tsx                      # Navegación superior
├── event-card.tsx                  # Tarjeta de evento en listado
├── event-detail-client.tsx         # Detalle de evento con interactividad
├── seatmap.tsx                     # Mapa de asientos interactivo
├── seat-button.tsx                 # Botón individual de asiento
├── cart-sidebar.tsx                # Panel lateral del carrito
├── checkout-client.tsx             # Formulario de checkout
├── waitlist-modal.tsx              # Modal para unirse a lista de espera
├── login-screen.tsx                # Formulario de login
├── countdown-timer.tsx             # Contador regresivo de reserva (15min)
└── admin/
    └── [componentes de administración]
```

---

## Flujo de Usuario — Compra de Boleto

### 1. Home (`/`)
- Carga eventos desde `GET /events` (Catalog)
- Muestra `EventCard` por cada evento con: nombre, fecha, venue, asientos disponibles, precio base
- Click en evento → navega a `/events/{eventId}`

### 2. Detalle de Evento (`/events/{eventId}`)
- Carga detalles con `GET /events/{id}` y mapa con `GET /events/{id}/seatmap`
- Renderiza `SeatMap` con estado visual de cada asiento:
  - Verde → Available (clickable)
  - Amarillo → Reserved (no disponible)
  - Rojo/Gris → Sold (no disponible)
- Al seleccionar un asiento:
  1. Llama `POST /reservations` (Inventory)
  2. Si exitoso → muestra `CountdownTimer` con 15 minutos
  3. Muestra `CartSidebar` con el asiento reservado
- Si el evento está agotado → botón "Unirse a Lista de Espera" → `WaitlistModal`

### 3. Checkout (`/checkout`)
- Muestra resumen de orden con ítems y total
- Formulario de pago (método, datos)
- Al confirmar:
  1. `POST /orders/checkout` → orden Draft → Pending (Ordering)
  2. `POST /payments` → procesar pago (Payment)
  3. Muestra resultado (éxito / fallo)

---

## Configuración de APIs

```typescript
// lib/api/config.ts
export const API_CONFIG = {
  catalog:   process.env.NEXT_PUBLIC_CATALOG_URL   || "http://localhost:50001",
  inventory: process.env.NEXT_PUBLIC_INVENTORY_URL || "http://localhost:50002",
  ordering:  process.env.NEXT_PUBLIC_ORDERING_URL  || "http://localhost:5003",
  payment:   process.env.NEXT_PUBLIC_PAYMENT_URL   || "http://localhost:5004",
  waitlist:  process.env.NEXT_PUBLIC_WAITLIST_URL  || "http://localhost:5006",
}
```

---

## Clientes de API

Cada servicio tiene su propio módulo de API client:

### `lib/api/catalog.ts`
```typescript
export async function getEvents(): Promise<EventSummary[]>
export async function getEvent(id: string): Promise<Event>
export async function getSeatmap(id: string): Promise<SeatMap>
```

### `lib/api/inventory.ts`
```typescript
export async function createReservation(data: {
  seatId: string
  customerId: string
  eventId?: string
}): Promise<Reservation>
```

### `lib/api/ordering.ts`
```typescript
// Intenta AddToCart con hasta 3 reintentos (3s de delay entre intentos)
// Necesario para compensar latencia de propagación Kafka
export async function addToCartWithRetry(data: {
  reservationId: string
  seatId: string
  price: number
  userId?: string
  guestToken?: string
}): Promise<Order>

export async function checkout(data: {
  orderId: string
  userId?: string
  guestToken?: string
}): Promise<Order>
```

### `lib/api/payment.ts`
```typescript
export async function processPayment(data: {
  orderId: string
  customerId: string
  reservationId?: string
  amount: number
  currency?: string
  paymentMethod?: string
}): Promise<PaymentResult>
```

---

## Manejo de Guest vs Usuario Autenticado

El frontend soporta dos modos de uso:

**Usuario autenticado:**
- Tiene JWT en localStorage/cookie
- Las llamadas a Ordering y Payment incluyen `userId` del token
- Puede ver historial de órdenes

**Guest (invitado):**
- Se genera un `guestToken` (UUID v4) al inicio de la sesión
- Se almacena en `sessionStorage`
- Se envía como `guestToken` en lugar de `userId`
- Permite completar una compra sin registro

---

## Panel de Administración

Accesible en `/admin` — requiere token JWT con rol `Admin`.

### Dashboard (`/admin/dashboard`)
- Métricas generales: eventos activos, total de ventas, asientos vendidos
- Gráfica de ingresos por evento (Recharts)
- Tabla de reservas activas y recientes

### Gestión de Eventos (`/admin/events`)
- Listado de todos los eventos (activos e inactivos)
- Acciones: editar, desactivar/reactivar, generar asientos

### Crear/Editar Evento
- Formulario con validación (React Hook Form + Zod)
- Campos: nombre, descripción, fecha, venue, capacidad máxima, precio base

### Generación de Asientos (`/admin/events/{id}/seats`)
- Configurar secciones dinámicamente:
  - Nombre de sección (ej: VIP, General, Palco)
  - Cantidad de filas y asientos por fila
  - Multiplicador de precio (ej: VIP = 3x el precio base)
- Botón "Generar Asientos" → `POST /admin/events/{id}/seats`

---

## Tipos TypeScript Principales

```typescript
// lib/types/index.ts

interface EventSummary {
  id: string
  name: string
  description: string
  eventDate: string
  venue: string
  maxCapacity: number
  basePrice: number
  totalSeats: number
  soldSeats: number
}

interface Seat {
  id: string
  section: string
  row: string
  number: number
  price: number
  status: "Available" | "Reserved" | "Sold"
}

interface Order {
  id: string
  state: "Draft" | "Pending" | "Paid" | "Fulfilled" | "Cancelled"
  totalAmount: number
  items: OrderItem[]
  createdAt: string
}
```

---

## Variables de Entorno

| Variable | Valor por defecto | Descripción |
|----------|-------------------|-------------|
| `NEXT_PUBLIC_CATALOG_URL` | `http://localhost:50001` | URL del Catalog Service |
| `NEXT_PUBLIC_INVENTORY_URL` | `http://localhost:50002` | URL del Inventory Service |
| `NEXT_PUBLIC_ORDERING_URL` | `http://localhost:5003` | URL del Ordering Service |
| `NEXT_PUBLIC_PAYMENT_URL` | `http://localhost:5004` | URL del Payment Service |
| `NEXT_PUBLIC_WAITLIST_URL` | `http://localhost:5006` | URL del Waitlist Service |
