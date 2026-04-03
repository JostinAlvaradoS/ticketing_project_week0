"use client"

import Link from "next/link"
import { format } from "date-fns"
import { Calendar, MapPin, Users, ArrowRight } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import type { EventSummary } from "@/lib/types"
import { cn } from "@/lib/utils"

interface EventCardProps {
  event: EventSummary
}

export function EventCard({ event }: EventCardProps) {
  const parseEventDate = () => {
    try {
      const date = new Date(event.eventDate ?? event.date)
      if (isNaN(date.getTime())) throw new Error("Invalid date")
      return date
    } catch {
      return new Date()
    }
  }

  const eventDate = parseEventDate()

  const formatEventDate = () => {
    try {
      return format(eventDate, "EEE, MMM d, yyyy · h:mm a")
    } catch {
      return eventDate.toLocaleDateString()
    }
  }

  const totalSeats = event.totalSeats ?? event.maxCapacity ?? 0
  const soldSeats = event.soldSeats ?? 0
  const availableSeats = Math.max(0, totalSeats - soldSeats)
  const soldPercent = totalSeats > 0 ? Math.min(100, Math.round((soldSeats / totalSeats) * 100)) : 0
  const isSoldOut = availableSeats === 0 && totalSeats > 0
  const isAlmostGone = !isSoldOut && soldPercent >= 80

  return (
    <div
      className={cn(
        "group relative flex overflow-hidden rounded-xl border bg-card transition-all duration-300",
        "hover:shadow-lg hover:shadow-black/20 hover:-translate-y-0.5",
        isSoldOut
          ? "border-border opacity-80"
          : "border-border hover:border-accent/40"
      )}
    >
      {/* Left accent bar */}
      <div
        className={cn(
          "w-1 shrink-0 transition-colors duration-300",
          isSoldOut
            ? "bg-muted-foreground/30"
            : isAlmostGone
            ? "bg-seat-reserved"
            : "bg-accent group-hover:bg-accent"
        )}
      />

      <div className="flex flex-1 flex-col gap-4 p-5">
        {/* Top row: title + badges */}
        <div className="flex items-start justify-between gap-4">
          <div className="flex flex-col gap-1.5 min-w-0">
            <h2 className={cn(
              "text-xl font-bold tracking-tight text-balance transition-colors",
              isSoldOut ? "text-muted-foreground" : "text-foreground group-hover:text-accent"
            )}>
              {event.name}
            </h2>
            {event.description && (
              <p className="text-sm text-muted-foreground leading-relaxed line-clamp-2">
                {event.description}
              </p>
            )}
          </div>

          <div className="flex flex-col items-end gap-2 shrink-0">
            {isSoldOut ? (
              <Badge variant="outline" className="border-destructive/40 text-destructive bg-destructive/10 font-semibold">
                Sold Out
              </Badge>
            ) : isAlmostGone ? (
              <Badge variant="outline" className="border-seat-reserved/40 text-seat-reserved bg-seat-reserved/10 font-semibold animate-pulse">
                Almost Gone
              </Badge>
            ) : (
              <Badge variant="secondary" className="text-accent bg-accent/10 border-accent/20 font-semibold">
                From ${event.basePrice?.toFixed(0) ?? "—"}
              </Badge>
            )}
          </div>
        </div>

        {/* Meta info */}
        <div className="flex flex-wrap items-center gap-x-4 gap-y-1.5 text-sm text-muted-foreground">
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
          {totalSeats > 0 && (
            <div className="flex items-center gap-1.5">
              <Users className="size-3.5 text-accent/70" />
              <span>{isSoldOut ? "No seats available" : `${availableSeats} seats available`}</span>
            </div>
          )}
        </div>

        {/* Availability bar */}
        {totalSeats > 0 && (
          <div className="flex flex-col gap-1">
            <div className="h-1.5 w-full rounded-full bg-secondary overflow-hidden">
              <div
                className={cn(
                  "h-full rounded-full transition-all duration-500",
                  isSoldOut
                    ? "bg-destructive/60"
                    : isAlmostGone
                    ? "bg-seat-reserved"
                    : "bg-accent"
                )}
                style={{ width: `${soldPercent}%` }}
              />
            </div>
          </div>
        )}

        {/* CTA */}
        <div className="flex justify-end pt-1">
          <Button
            asChild
            size="sm"
            className={cn(
              "gap-1.5 transition-all",
              isSoldOut
                ? "bg-secondary text-secondary-foreground hover:bg-secondary/80"
                : "bg-accent text-accent-foreground hover:bg-accent/90"
            )}
          >
            <Link href={`/events/${event.id}`}>
              {isSoldOut ? "View & Join Waitlist" : "Select Seats"}
              <ArrowRight className="size-3.5" />
            </Link>
          </Button>
        </div>
      </div>
    </div>
  )
}
