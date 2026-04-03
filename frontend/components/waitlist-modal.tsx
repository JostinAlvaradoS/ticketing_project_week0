"use client"

import { useState } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { joinWaitlist } from "@/lib/api/waitlist"
import { Loader2, CheckCircle2, Users } from "lucide-react"

interface WaitlistModalProps {
  open: boolean
  onClose: () => void
  eventId: string
  eventName: string
}

export function WaitlistModal({ open, onClose, eventId, eventName }: WaitlistModalProps) {
  const [email, setEmail] = useState("")
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [position, setPosition] = useState<number | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setLoading(true)

    try {
      const result = await joinWaitlist({ email, eventId })
      setPosition(result.position)
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not join waitlist")
    } finally {
      setLoading(false)
    }
  }

  const handleClose = () => {
    setEmail("")
    setError(null)
    setPosition(null)
    onClose()
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Users className="size-5 text-accent" />
            Join the Waitlist
          </DialogTitle>
          <DialogDescription>
            {eventName} is sold out. Enter your email and we'll notify you when a seat becomes available.
          </DialogDescription>
        </DialogHeader>

        {position !== null ? (
          <div className="flex flex-col items-center gap-4 py-6 text-center">
            <div className="flex size-14 items-center justify-center rounded-full bg-accent/10 border border-accent/20">
              <CheckCircle2 className="size-7 text-accent" />
            </div>
            <div className="flex flex-col gap-1">
              <p className="font-semibold text-foreground">You're on the list!</p>
              <p className="text-sm text-muted-foreground">
                Your position in the queue:{" "}
                <span className="font-bold text-accent text-base">#{position}</span>
              </p>
              <p className="text-xs text-muted-foreground mt-1">
                We'll send an email to <span className="font-medium">{email}</span> when a seat opens up.
              </p>
            </div>
            <Button onClick={handleClose} className="bg-accent text-accent-foreground hover:bg-accent/90">
              Done
            </Button>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="flex flex-col gap-4 pt-2">
            <div className="flex flex-col gap-1.5">
              <label htmlFor="waitlist-email" className="text-sm font-medium text-foreground">
                Email address
              </label>
              <Input
                id="waitlist-email"
                type="email"
                placeholder="you@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                disabled={loading}
              />
            </div>

            {error && (
              <p className="text-sm text-destructive rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2">
                {error}
              </p>
            )}

            <div className="flex gap-2 justify-end">
              <Button type="button" variant="outline" onClick={handleClose} disabled={loading}>
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={loading || !email}
                className="bg-accent text-accent-foreground hover:bg-accent/90"
              >
                {loading ? (
                  <>
                    <Loader2 className="size-4 animate-spin" />
                    Joining...
                  </>
                ) : (
                  "Join Waitlist"
                )}
              </Button>
            </div>
          </form>
        )}
      </DialogContent>
    </Dialog>
  )
}
