// --- Catalog Service Types ---

export interface SeatmapResponse {
  eventId: string
  eventName: string
  eventDescription: string
  eventDate: string
  basePrice: number
  seats: Seat[]
}

export interface Seat {
  id: string
  sectionCode: string
  rowNumber: number
  seatNumber: number
  price: number
  status: "available" | "reserved" | "sold"
}

// --- Inventory Service Types ---

export interface CreateReservationRequest {
  seatId: string
  customerId: string
}

export interface CreateReservationResponse {
  reservationId: string
  seatId: string
  customerId: string
  expiresAt: string
  status: string
}

// --- Ordering Service Types ---

export interface AddToCartRequest {
  reservationId: string | null
  seatId: string
  price: number
  userId?: string | null
  guestToken?: string | null
}

// Server returns the Order directly, or wrapped in a success response
export type AddToCartResponse = Order | {
  success: boolean
  errorMessage?: string
  order?: Order
}

export interface Order {
  id: string
  userId: string | null
  guestToken: string | null
  totalAmount: number
  state: "draft" | "pending" | "paid" | "fulfilled" | "cancelled"
  createdAt: string
  paidAt: string | null
  items: OrderItem[]
}

export interface OrderItem {
  id: string
  seatId: string
  price: number
}

export interface CheckoutRequest {
  orderId: string
  userId?: string | null
  guestToken?: string | null
}

// --- App-level types ---

export interface ReservationInfo {
  reservationId: string
  seatId: string
  expiresAt: string
  seat: Seat
}

export interface EventSummary {
  id: string
  name: string
  description: string
  date: string
  basePrice: number
  venue: string
  imageUrl?: string
}
