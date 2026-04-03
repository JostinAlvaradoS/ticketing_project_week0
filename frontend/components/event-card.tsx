"use client"

import Link from "next/link"
import { format } from "date-fns"
import { Calendar, MapPin, ArrowRight } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import type { EventSummary } from "@/lib/types"

interface EventCardProps {
  event: EventSummary
}

export function EventCard({ event }: EventCardProps) {
  const parseEventDate = () => {
    try {
      const date = new Date(event.eventDate ?? event.date)
      return isNaN(date.getTime()) ? new Date() : date
    } catch {
      return new Date()
    }
  }

  const eventDate = parseEventDate()

  const formatEventDate = () => {
    try {
      return format(eventDate, "MMM d, yyyy 'at' h:mm a")
    } catch {
      return eventDate.toLocaleDateString()
    }
  }

  return (
    <div className="group relative flex overflow-hidden rounded-xl border border-border bg-card transition-all duration-200 hover:border-accent/50 hover:shadow-lg hover:-translate-y-0.5">
      {/* Left accent bar */}
      <div className="w-1 shrink-0 bg-accent/30 group-hover:bg-accent transition-colors duration-200" />

      <div className="flex flex-1 flex-col gap-4 p-5">
        <div className="flex items-start justify-between gap-4">
          <div className="flex flex-col gap-1">
            <h2 className="text-lg font-semibold text-foreground group-hover:text-accent transition-colors leading-snug">
              {event.name}
            </h2>
            {event.description && (
              <p className="text-sm text-muted-foreground leading-relaxed line-clamp-2">
                {event.description}
              </p>
            )}
          </div>
          <Badge variant="secondary" className="shrink-0 text-accent bg-accent/10 border border-accent/20 font-semibold">
            From ${event.basePrice.toFixed(0)}
          </Badge>
        </div>

        <div className="flex items-center justify-between gap-4 flex-wrap">
          <div className="flex flex-wrap items-center gap-4 text-xs text-muted-foreground">
            <div className="flex items-center gap-1.5">
              <Calendar className="size-3.5 text-accent/70" />
              <span>{formatEventDate()}</span>
            </div>
            {event.venue && (
              <div className="flex items-center gap-1.5">
                <MapPin className="size-3.5 text-accent/70" />
                <span>{event.venue}</span>
              </div>
            )}
          </div>

          <Button
            asChild
            size="sm"
            className="bg-accent text-accent-foreground hover:bg-accent/90 gap-1.5 shrink-0"
          >
            <Link href={`/events/${event.id}`}>
              Select Seats
              <ArrowRight className="size-3.5" />
            </Link>
          </Button>
        </div>
      </div>
    </div>
  )
}
