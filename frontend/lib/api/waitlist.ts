import { API_CONFIG } from "./config"

export interface JoinWaitlistRequest {
  email: string
  eventId: string
}

export interface JoinWaitlistResponse {
  waitlistEntryId: string
  position: number
  email: string
  eventId: string
}

export async function joinWaitlist(request: JoinWaitlistRequest): Promise<JoinWaitlistResponse> {
  const res = await fetch(`${API_CONFIG.waitlist}/api/v1/waitlist/join`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  })

  if (!res.ok) {
    const text = await res.text().catch(() => "")
    throw new Error(text || `Error ${res.status}`)
  }

  return res.json()
}
