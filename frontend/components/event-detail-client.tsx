"use client"

import Link from "next/link"
import { format } from "date-fns"
import { ArrowLeft, Calendar, MapPin, Loader2, Users } from "lucide-react"
import { useSeatmap } from "@/hooks/use-seatmap"
import { Seatmap } from "@/components/seatmap"
import { CartSidebar } from "@/components/cart-sidebar"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Badge } from "@/components/ui/badge"
import { useMemo } from "react"

interface EventDetailClientProps {
  eventId: string
}

export function EventDetailClient({ eventId }: EventDetailClientProps) {
  const { data: seatmap, error, isLoading, mutate } = useSeatmap(eventId)

  const availableSeats = useMemo(() =>
    seatmap?.seats.filter(s => s.status === "available").length ?? 0,
    [seatmap]
  )
  const totalSeats = seatmap?.seats.length ?? 0
  const isSoldOut = totalSeats > 0 && availableSeats === 0

  return (
    <main className="min-h-screen bg-background">
      <div className="mx-auto max-w-7xl px-4 py-8">
        {/* Back navigation */}
        <Button variant="ghost" asChild className="mb-6 text-muted-foreground hover:text-foreground -ml-2">
          <Link href="/">
            <ArrowLeft className="size-4" />
            Back to Events
          </Link>
        </Button>

        <div className="flex flex-col lg:flex-row gap-8">
          {/* Main content */}
          <div className="flex-1 flex flex-col gap-6 min-w-0">
            {/* Event header */}
            {isLoading ? (
              <div className="flex flex-col gap-3">
                <Skeleton className="h-9 w-72" />
                <Skeleton className="h-5 w-full max-w-lg" />
                <div className="flex gap-3">
                  <Skeleton className="h-5 w-48" />
                  <Skeleton className="h-5 w-32" />
                </div>
              </div>
            ) : seatmap ? (
              <div className="flex flex-col gap-3">
                <div className="flex items-start justify-between gap-4 flex-wrap">
                  <h1 className="text-3xl font-bold text-foreground tracking-tight">
                    {seatmap.eventName}
                  </h1>
                  {isSoldOut ? (
                    <Badge variant="outline" className="border-destructive/40 text-destructive bg-destructive/10 font-semibold shrink-0">
                      Sold Out
                    </Badge>
                  ) : (
                    <Badge variant="secondary" className="text-accent bg-accent/10 border-accent/20 shrink-0">
                      {availableSeats} seats available
                    </Badge>
                  )}
                </div>

                {seatmap.eventDescription && (
                  <p className="text-muted-foreground leading-relaxed max-w-2xl">
                    {seatmap.eventDescription}
                  </p>
                )}

                <div className="flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
                  <div className="flex items-center gap-1.5">
                    <Calendar className="size-4 text-accent/70" />
                    <span>{format(new Date(seatmap.eventDate), "EEEE, MMMM d, yyyy 'at' h:mm a")}</span>
                  </div>
                  {totalSeats > 0 && (
                    <div className="flex items-center gap-1.5">
                      <Users className="size-4 text-accent/70" />
                      <span>{totalSeats} total seats</span>
                    </div>
                  )}
                </div>

                {/* Availability bar */}
                {totalSeats > 0 && (
                  <div className="flex flex-col gap-1 max-w-xs">
                    <div className="h-1.5 w-full rounded-full bg-secondary overflow-hidden">
                      <div
                        className={`h-full rounded-full transition-all ${isSoldOut ? "bg-destructive/60" : "bg-accent"}`}
                        style={{ width: `${Math.round(((totalSeats - availableSeats) / totalSeats) * 100)}%` }}
                      />
                    </div>
                    <p className="text-xs text-muted-foreground">
                      {Math.round(((totalSeats - availableSeats) / totalSeats) * 100)}% filled
                    </p>
                  </div>
                )}
              </div>
            ) : null}

            {/* Seatmap */}
            {isLoading ? (
              <div className="flex flex-col items-center justify-center py-20 gap-3">
                <Loader2 className="size-8 animate-spin text-accent" />
                <p className="text-muted-foreground text-sm">Loading seat map...</p>
              </div>
            ) : error ? (
              <div className="rounded-xl border border-destructive/30 bg-destructive/10 p-6 text-center">
                <p className="text-destructive font-medium mb-2">Failed to load seat map</p>
                <p className="text-muted-foreground text-sm mb-4">
                  {error.message || "Please check the connection to the Catalog service."}
                </p>
                <Button variant="outline" onClick={() => mutate()} className="border-destructive/30 text-destructive hover:bg-destructive/10">
                  Retry
                </Button>
              </div>
            ) : seatmap ? (
              <Seatmap seatmap={seatmap} onSeatReserved={() => mutate()} />
            ) : null}
          </div>

          {/* Cart sidebar */}
          <div className="lg:w-72 shrink-0">
            <div className="lg:sticky lg:top-24">
              <CartSidebar />
            </div>
          </div>
        </div>
      </div>
    </main>
  )
}
