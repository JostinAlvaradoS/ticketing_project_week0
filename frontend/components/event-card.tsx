"use client"

import Link from "next/link"
import { format } from "date-fns"
import { Calendar, MapPin, DollarSign } from "lucide-react"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import type { EventSummary } from "@/lib/types"

interface EventCardProps {
  event: EventSummary
}

export function EventCard({ event }: EventCardProps) {
  const eventDate = new Date(event.date)

  return (
    <Card className="bg-card border-border hover:border-accent/40 transition-colors group">
      <CardContent className="p-6 flex flex-col gap-4">
        <div className="flex items-start justify-between gap-4">
          <div className="flex flex-col gap-1.5">
            <h2 className="text-xl font-semibold text-foreground group-hover:text-accent transition-colors text-balance">
              {event.name}
            </h2>
            <p className="text-sm text-muted-foreground leading-relaxed">
              {event.description}
            </p>
          </div>
          <Badge variant="secondary" className="shrink-0 text-accent bg-accent/10 border-accent/20">
            From ${event.basePrice.toFixed(0)}
          </Badge>
        </div>

        <div className="flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
          <div className="flex items-center gap-1.5">
            <Calendar className="size-4" />
            <span>{format(eventDate, "MMM d, yyyy 'at' h:mm a")}</span>
          </div>
          <div className="flex items-center gap-1.5">
            <MapPin className="size-4" />
            <span>{event.venue}</span>
          </div>
          <div className="flex items-center gap-1.5">
            <DollarSign className="size-4" />
            <span>Starting at ${event.basePrice.toFixed(2)}</span>
          </div>
        </div>

        <div className="flex justify-end pt-2">
          <Button asChild className="bg-accent text-accent-foreground hover:bg-accent/90">
            <Link href={`/events/${event.id}`}>
              Select Seats
            </Link>
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}
