"use client"

import useSWR from "swr"
import { api } from "@/lib/api"

export function useEvents() {
  return useSWR("events", () => api.getEvents(), {
    refreshInterval: 10000,
    errorRetryCount: 3,
    errorRetryInterval: 5000,
  })
}

export function useEvent(id: number | null) {
  return useSWR(id != null ? `event-${id}` : null, () => api.getEvent(id!), {
    refreshInterval: 5000,
    errorRetryCount: 3,
    errorRetryInterval: 5000,
  })
}

export function useTickets(eventId: number | null) {
  return useSWR(
    eventId != null ? `tickets-${eventId}` : null,
    () => api.getTicketsByEvent(eventId!),
    {
      refreshInterval: 3000,
      errorRetryCount: 3,
      errorRetryInterval: 5000,
    }
  )
}
