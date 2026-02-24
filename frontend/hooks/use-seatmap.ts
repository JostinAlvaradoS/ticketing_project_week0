"use client"

import useSWR from "swr"
import { getSeatmap } from "@/lib/api/catalog"
import type { SeatmapResponse } from "@/lib/types"

export function useSeatmap(eventId: string) {
  return useSWR<SeatmapResponse>(
    eventId ? `seatmap-${eventId}` : null,
    () => getSeatmap(eventId),
    {
      revalidateOnFocus: false,
      dedupingInterval: 30000,
    }
  )
}
