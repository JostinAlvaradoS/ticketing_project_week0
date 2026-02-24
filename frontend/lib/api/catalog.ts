import { API_CONFIG } from "./config"
import type { SeatmapResponse, EventSummary } from "@/lib/types"

export async function getEvents(): Promise<EventSummary[]> {
  const res = await fetch(`${API_CONFIG.catalog}/events`)
  if (!res.ok) {
    throw new Error(`Failed to fetch events: ${res.status} ${res.statusText}`)
  }
  return res.json()
}

export async function getSeatmap(eventId: string): Promise<SeatmapResponse> {
  const res = await fetch(`${API_CONFIG.catalog}/events/${eventId}/seatmap`)
  if (!res.ok) {
    throw new Error(`Failed to fetch seatmap: ${res.status} ${res.statusText}`)
  }
  return res.json()
}
