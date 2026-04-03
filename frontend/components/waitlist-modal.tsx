"use client"

import { useState } from "react"
import { Mail, CheckCircle2, Loader2, Users, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog"
import { joinWaitlist } from "@/lib/api/waitlist"

interface WaitlistModalProps {
  open: boolean
  onClose: () => void
  eventId: string
  eventName: string
}

export function WaitlistModal({ open, onClose, eventId, eventName }: WaitlistModalProps) {
  const [email, setEmail] = useState("")
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<{ position: number } | null>(null)

  const isValidEmail = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!isValidEmail) return

    setIsLoading(true)
    setError(null)

    try {
      const res = await joinWaitlist({ email, eventId })
      setResult({ position: res.position })
    } catch (err) {
      setError(err instanceof Error ? err.message : "Something went wrong")
    } finally {
      setIsLoading(false)
    }
  }

  const handleClose = () => {
    setEmail("")
    setError(null)
    setResult(null)
    onClose()
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="bg-card border-border sm:max-w-md">
        {result ? (
          // Success state
          <div className="flex flex-col items-center gap-6 py-4 text-center">
            <div className="flex size-16 items-center justify-center rounded-full bg-accent/10 border border-accent/30">
              <CheckCircle2 className="size-8 text-accent" />
            </div>
            <div className="flex flex-col gap-2">
              <h2 className="text-xl font-bold text-foreground">You're on the list!</h2>
              <p className="text-muted-foreground text-sm leading-relaxed">
                We'll email <span className="text-foreground font-medium">{email}</span> as soon as a seat opens up.
              </p>
            </div>
            <div className="flex items-center gap-3 rounded-xl border border-accent/20 bg-accent/5 px-6 py-4 w-full justify-center">
              <Users className="size-5 text-accent" />
              <div className="text-left">
                <p className="text-xs text-muted-foreground uppercase tracking-wider">Queue position</p>
                <p className="text-2xl font-bold text-accent">#{result.position}</p>
              </div>
            </div>
            <p className="text-xs text-muted-foreground">
              You have 30 minutes to complete your purchase once a seat is assigned to you.
            </p>
            <Button onClick={handleClose} className="w-full bg-accent text-accent-foreground hover:bg-accent/90">
              Done
            </Button>
          </div>
        ) : (
          // Form state
          <>
            <DialogHeader>
              <div className="flex size-12 items-center justify-center rounded-full bg-accent/10 border border-accent/20 mb-2">
                <Users className="size-5 text-accent" />
              </div>
              <DialogTitle className="text-xl font-bold text-foreground">Join the Waitlist</DialogTitle>
              <DialogDescription className="text-muted-foreground">
                <span className="font-medium text-foreground">{eventName}</span> is sold out.
                Enter your email and we'll notify you the moment a seat becomes available.
              </DialogDescription>
            </DialogHeader>

            <form onSubmit={handleSubmit} className="flex flex-col gap-4 mt-2">
              <div className="flex flex-col gap-1.5">
                <label htmlFor="waitlist-email" className="text-sm font-medium text-foreground">
                  Email address
                </label>
                <div className="relative">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
                  <Input
                    id="waitlist-email"
                    type="email"
                    placeholder="you@example.com"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="pl-9 bg-secondary border-border focus:border-accent"
                    disabled={isLoading}
                    autoFocus
                  />
                </div>
              </div>

              {error && (
                <p className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-3 py-2">
                  {error}
                </p>
              )}

              <div className="flex flex-col gap-2">
                <Button
                  type="submit"
                  disabled={!isValidEmail || isLoading}
                  className="w-full bg-accent text-accent-foreground hover:bg-accent/90 disabled:opacity-50"
                >
                  {isLoading ? (
                    <>
                      <Loader2 className="size-4 animate-spin" />
                      Joining waitlist...
                    </>
                  ) : (
                    "Notify me when available"
                  )}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  onClick={handleClose}
                  className="w-full text-muted-foreground hover:text-foreground"
                >
                  Cancel
                </Button>
              </div>
            </form>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
