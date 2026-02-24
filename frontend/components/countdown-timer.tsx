"use client"

import { useState, useEffect } from "react"

interface CountdownTimerProps {
  expiresAt: string
  onExpired?: () => void
}

export function CountdownTimer({ expiresAt, onExpired }: CountdownTimerProps) {
  const [timeLeft, setTimeLeft] = useState(() => getTimeLeft(expiresAt))

  useEffect(() => {
    const interval = setInterval(() => {
      const remaining = getTimeLeft(expiresAt)
      setTimeLeft(remaining)
      if (remaining <= 0) {
        clearInterval(interval)
        onExpired?.()
      }
    }, 1000)
    return () => clearInterval(interval)
  }, [expiresAt, onExpired])

  if (timeLeft <= 0) {
    return (
      <span className="text-destructive font-mono text-sm font-medium">
        Expired
      </span>
    )
  }

  const minutes = Math.floor(timeLeft / 60)
  const seconds = timeLeft % 60
  const isUrgent = minutes < 2

  return (
    <span
      className={`font-mono text-sm font-medium tabular-nums ${
        isUrgent ? "text-destructive" : "text-seat-reserved"
      }`}
    >
      {String(minutes).padStart(2, "0")}:{String(seconds).padStart(2, "0")}
    </span>
  )
}

function getTimeLeft(expiresAt: string): number {
  const diff = new Date(expiresAt).getTime() - Date.now()
  return Math.max(0, Math.floor(diff / 1000))
}
