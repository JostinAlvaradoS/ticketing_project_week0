import { EventCard } from "@/components/event-card"
import { getEvents } from "@/lib/api/catalog"
import { Ticket, AlertCircle } from "lucide-react"
import { Alert, AlertDescription } from "@/components/ui/alert"

export default async function EventsPage() {
  let events = []
  let error = null

  try {
    events = await getEvents()
    console.log("Events:", events);
    
  } catch (err) {
    error = err instanceof Error ? err.message : "Failed to load events"
    console.error("Error loading events:", error)
  }

  return (
    <main className="min-h-screen bg-background">
      <div className="mx-auto max-w-4xl px-4 py-12">
        {/* Header */}
        <div className="flex flex-col gap-2 pb-8">
          <div className="flex items-center gap-3">
            <Ticket className="size-8 text-accent" />
            <h1 className="text-3xl font-bold text-foreground tracking-tight">
              Events
            </h1>
          </div>
          <p className="text-muted-foreground text-lg">
            Browse upcoming events and reserve your seats.
          </p>
        </div>

        {/* Error Alert */}
        {error && (
          <Alert variant="destructive" className="mb-6">
            <AlertCircle className="size-4" />
            <AlertDescription>
              Failed to load events. Make sure the Catalog service is running on localhost:50001. Error: {error}
            </AlertDescription>
          </Alert>
        )}

        {/* Event List */}
        {events.length > 0 ? (
          <div className="flex flex-col gap-4">
            {events.map((event) => (
              <EventCard key={event.id} event={event} />
            ))}
          </div>
        ) : !error ? (
          <div className="text-center py-12">
            <p className="text-muted-foreground">No events available</p>
          </div>
        ) : null}
      </div>
    </main>
  )
}
