import { API_CONFIG } from "./config"
import type { JoinWaitlistRequest, JoinWaitlistResponse } from "@/lib/types"

export async function joinWaitlist(data: JoinWaitlistRequest): Promise<JoinWaitlistResponse> {
  const res = await fetch(`${API_CONFIG.waitlist}/api/v1/waitlist/join`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  })

  if (res.status === 409) {
    throw new Error("You are already on the waitlist for this event.")
  }
  if (res.status === 503) {
    throw new Error("Service temporarily unavailable. Please try again.")
  }
  if (!res.ok) {
    throw new Error(`Failed to join waitlist: ${res.status}`)
  }
  return res.json()
}
