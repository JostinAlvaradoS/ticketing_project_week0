"use client"

import useSWR from "swr"
import { getSeatmap } from "@/lib/api/catalog"
import type { SeatmapResponse } from "@/lib/types"

export function useSeatmap(eventId: string) {
  return useSWR<SeatmapResponse>(
    eventId ? `seatmap-${eventId}` : null,
    () => getSeatmap(eventId),
    {
      revalidateOnFocus: true,
      revalidateIfStale: true,
      dedupingInterval: 5000,
      refreshInterval: 10000, // Sync UI every 10s automatically
    }
  )
}
