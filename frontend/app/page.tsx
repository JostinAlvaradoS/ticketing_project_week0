import { EventCard } from "@/components/event-card"
import { getEvents } from "@/lib/api/catalog"
import { Ticket, AlertCircle, Calendar } from "lucide-react"
import { Alert, AlertDescription } from "@/components/ui/alert"

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
      {/* Hero header */}
      <div className="border-b border-border bg-card/50">
        <div className="mx-auto max-w-5xl px-4 py-10">
          <div className="flex flex-col gap-3">
            <div className="flex items-center gap-2.5">
              <div className="flex size-10 items-center justify-center rounded-lg bg-accent/10 border border-accent/20">
                <Ticket className="size-5 text-accent" />
              </div>
              <h1 className="text-3xl font-bold tracking-tight text-foreground">
                Upcoming Events
              </h1>
            </div>
            <p className="text-muted-foreground max-w-xl">
              Discover live experiences and secure your seat before they sell out.
            </p>
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-5xl px-4 py-8">
        {error && (
          <Alert variant="destructive" className="mb-6">
            <AlertCircle className="size-4" />
            <AlertDescription>
              Could not load events — make sure the Catalog service is running. {error}
            </AlertDescription>
          </Alert>
        )}

        {events.length > 0 ? (
          <div className="flex flex-col gap-3">
            {events.map((event) => (
              <EventCard key={event.id} event={event} />
            ))}
          </div>
        ) : !error ? (
          <div className="flex flex-col items-center justify-center py-20 gap-4 text-center">
            <div className="flex size-16 items-center justify-center rounded-full bg-secondary border border-border">
              <Calendar className="size-7 text-muted-foreground" />
            </div>
            <div>
              <p className="font-medium text-foreground">No events available</p>
              <p className="text-sm text-muted-foreground mt-1">Check back soon for upcoming shows.</p>
            </div>
          </div>
        ) : null}
      </div>
    </main>
  )
}
