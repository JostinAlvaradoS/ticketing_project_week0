import { EventCard } from "@/components/event-card"
import { getEvents } from "@/lib/api/catalog"
import { Ticket, AlertCircle, CalendarX } from "lucide-react"

export default async function EventsPage() {
  let events = []
  let error = null

  try {
    events = await getEvents()
  } catch (err) {
    error = err instanceof Error ? err.message : "Failed to load events"
  }

  return (
    <main className="min-h-screen bg-background">
      <div className="mx-auto max-w-4xl px-4 py-12">
        {/* Hero header */}
        <div className="flex flex-col gap-3 pb-10">
          <div className="flex items-center gap-3">
            <div className="flex size-10 items-center justify-center rounded-xl bg-accent/10 border border-accent/20">
              <Ticket className="size-5 text-accent" />
            </div>
            <h1 className="text-3xl font-bold text-foreground tracking-tight">
              Upcoming Events
            </h1>
          </div>
          <p className="text-muted-foreground text-base pl-[52px]">
            Browse and reserve your seats before they sell out.
          </p>
        </div>
      </div>

        {/* Error state */}
        {error && (
          <div className="flex items-start gap-3 rounded-xl border border-destructive/30 bg-destructive/5 p-4 mb-8">
            <AlertCircle className="size-5 text-destructive shrink-0 mt-0.5" />
            <div className="flex flex-col gap-0.5">
              <p className="text-sm font-medium text-destructive">Could not load events</p>
              <p className="text-xs text-muted-foreground">
                Make sure the Catalog service is running. {error}
              </p>
            </div>
          </div>
        )}

        {/* Event list */}
        {events.length > 0 ? (
          <div className="flex flex-col gap-3">
            {events.map((event) => (
              <EventCard key={event.id} event={event} />
            ))}
          </div>
        ) : !error ? (
          <div className="flex flex-col items-center gap-3 py-20 text-center">
            <div className="flex size-14 items-center justify-center rounded-full bg-secondary border border-border">
              <CalendarX className="size-6 text-muted-foreground" />
            </div>
            <p className="font-medium text-foreground">No events available</p>
            <p className="text-sm text-muted-foreground max-w-xs">
              Check back soon — new events are added regularly.
            </p>
          </div>
        ) : null}
      </div>
    </main>
  )
}
