"use client"

import { useState } from "react"
import { useAuth } from "@/context/auth-context"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Label } from "@/components/ui/label"
import { Ticket } from "lucide-react"

export function LoginScreen() {
  const { login } = useAuth()
  const [value, setValue] = useState("")

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (value.trim()) login(value.trim())
  }

  return (
    <main className="min-h-screen bg-background flex items-center justify-center px-4">
      <Card className="w-full max-w-sm bg-card border-border">
        <CardHeader className="flex flex-col items-center gap-4 pb-2">
          <div className="size-14 rounded-full bg-accent/15 flex items-center justify-center">
            <Ticket className="size-7 text-accent" />
          </div>
          <div className="flex flex-col items-center gap-1">
            <CardTitle className="text-xl text-foreground">
              Welcome to SpecKit Tickets
            </CardTitle>
            <p className="text-sm text-muted-foreground text-center text-pretty">
              Enter your user ID to start browsing events and purchasing tickets.
            </p>
          </div>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="userId" className="text-foreground">
                User ID
              </Label>
              <Input
                id="userId"
                type="text"
                placeholder="e.g. user-123"
                value={value}
                onChange={(e) => setValue(e.target.value)}
                className="bg-secondary border-border text-foreground placeholder:text-muted-foreground"
                autoFocus
                required
              />
            </div>
            <Button
              type="submit"
              disabled={!value.trim()}
              className="w-full bg-accent text-accent-foreground hover:bg-accent/90"
            >
              Sign In
            </Button>
          </form>
        </CardContent>
      </Card>
    </main>
  )
}
