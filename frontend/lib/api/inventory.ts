import { API_CONFIG } from "./config"
import type {
  CreateReservationRequest,
  CreateReservationResponse,
} from "@/lib/types"

export async function createReservation(
  data: CreateReservationRequest
): Promise<CreateReservationResponse> {
  const res = await fetch(`${API_CONFIG.inventory}/reservations`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  })
  if (res.status === 404) {
    throw new Error("Seat not found")
  }
  if (res.status === 409) {
    throw new Error("Seat is already reserved")
  }
  if (!res.ok) {
    throw new Error(
      `Failed to create reservation: ${res.status} ${res.statusText}`
    )
  }
  return res.json()
}
