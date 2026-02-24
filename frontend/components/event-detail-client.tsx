"use client"

import Link from "next/link"
import { format } from "date-fns"
import { ArrowLeft, Calendar, Loader2 } from "lucide-react"
import { useSeatmap } from "@/hooks/use-seatmap"
import { Seatmap } from "@/components/seatmap"
import { CartSidebar } from "@/components/cart-sidebar"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"

interface EventDetailClientProps {
  eventId: string
}

export function EventDetailClient({ eventId }: EventDetailClientProps) {
  const { data: seatmap, error, isLoading, mutate } = useSeatmap(eventId)

  return (
    <main className="min-h-screen bg-background">
      <div className="mx-auto max-w-7xl px-4 py-8">
        {/* Back navigation */}
        <Button variant="ghost" asChild className="mb-6 text-muted-foreground hover:text-foreground">
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
                <Skeleton className="h-8 w-64" />
                <Skeleton className="h-5 w-96" />
                <div className="flex gap-4">
                  <Skeleton className="h-5 w-40" />
                  <Skeleton className="h-5 w-32" />
                </div>
              </div>
            ) : seatmap ? (
              <div className="flex flex-col gap-2">
                <h1 className="text-3xl font-bold text-foreground tracking-tight">
                  {seatmap.eventName}
                </h1>
                <p className="text-muted-foreground leading-relaxed">
                  {seatmap.eventDescription}
                </p>
                <div className="flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
                  <div className="flex items-center gap-1.5">
                    <Calendar className="size-4" />
                    <span>
                      {format(
                        new Date(seatmap.eventDate),
                        "EEEE, MMMM d, yyyy 'at' h:mm a"
                      )}
                    </span>
                  </div>
                </div>
              </div>
            ) : null}

            {/* Seatmap */}
            {isLoading ? (
              <div className="flex flex-col items-center justify-center py-20 gap-3">
                <Loader2 className="size-8 animate-spin text-accent" />
                <p className="text-muted-foreground text-sm">
                  Loading seat map...
                </p>
              </div>
            ) : error ? (
              <div className="rounded-lg border border-destructive/30 bg-destructive/10 p-6 text-center">
                <p className="text-destructive font-medium mb-2">
                  Failed to load seat map
                </p>
                <p className="text-muted-foreground text-sm mb-4">
                  {error.message || "Please check the connection to the Catalog service."}
                </p>
                <Button
                  variant="outline"
                  onClick={() => mutate()}
                  className="border-destructive/30 text-destructive hover:bg-destructive/10"
                >
                  Retry
                </Button>
              </div>
            ) : seatmap ? (
              <Seatmap seatmap={seatmap} onSeatReserved={() => mutate()} />
            ) : null}
          </div>

          {/* Cart sidebar */}
          <div className="lg:w-80 shrink-0">
            <div className="lg:sticky lg:top-8">
              <CartSidebar />
            </div>
          </div>
        </div>
      </div>
    </main>
  )
}
