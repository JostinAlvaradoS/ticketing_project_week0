export const API_CONFIG = {
  catalog: process.env.NEXT_PUBLIC_CATALOG_URL || "http://localhost:50001",
  inventory: process.env.NEXT_PUBLIC_INVENTORY_URL || "http://localhost:50002",
  ordering: process.env.NEXT_PUBLIC_ORDERING_URL || "http://localhost:5003",
  payment: process.env.NEXT_PUBLIC_PAYMENT_URL || "http://localhost:5004",
  waitlist: process.env.NEXT_PUBLIC_WAITLIST_URL || "http://localhost:5006",
} as const

export const RETRY_DELAY_MS = 3000
export const RETRY_ADD_TO_CART_ATTEMPTS = 3
export const RESERVATION_MINUTES = 1
