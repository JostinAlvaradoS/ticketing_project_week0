"use client"

import { use } from "react"
import { EventDetailClient } from "@/components/event-detail-client"

export default function EventDetailPage({
  params,
}: {
  params: Promise<{ eventId: string }>
}) {
  const { eventId } = use(params)
  return <EventDetailClient eventId={eventId} />
}
