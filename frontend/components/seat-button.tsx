"use client"

import type { Seat } from "@/lib/types"
import { cn } from "@/lib/utils"

interface SeatButtonProps {
  seat: Seat
  isSelected: boolean
  onSelect: (seat: Seat) => void
}

const statusConfig = {
  available: {
    bgClass: "bg-seat-available/30 border-seat-available/60 hover:bg-seat-available hover:border-seat-available cursor-pointer",
    label: "Available",
  },
  reserved: {
    bgClass: "bg-seat-reserved/30 border-seat-reserved/50 cursor-not-allowed opacity-50",
    label: "Reserved",
  },
  sold: {
    bgClass: "bg-seat-sold/30 border-seat-sold/50 cursor-not-allowed opacity-40",
    label: "Sold",
  },
} as const

export function SeatButton({ seat, isSelected, onSelect }: SeatButtonProps) {
  const config = statusConfig[seat.status]
  const isClickable = seat.status === "available"

  return (
    <button
      type="button"
      disabled={!isClickable}
      onClick={() => isClickable && onSelect(seat)}
      title={`${seat.sectionCode} · Row ${seat.rowNumber} · Seat ${seat.seatNumber} · $${seat.price.toLocaleString()}`}
      aria-label={`Seat ${seat.sectionCode}${seat.rowNumber}-${seat.seatNumber}, $${seat.price}, ${config.label}`}
      className={cn(
        "shrink-0 rounded-sm border transition-all size-5",
        config.bgClass,
        isSelected && isClickable && "ring-1 ring-accent ring-offset-0 bg-accent border-accent"
      )}
    />
  )
}

export function SeatLegend() {
  return (
    <div className="flex items-center gap-5 text-xs text-muted-foreground">
      <div className="flex items-center gap-1.5">
        <div className="size-5 rounded-sm bg-seat-available/30 border border-seat-available/60" />
        <span>Available</span>
      </div>
      <div className="flex items-center gap-1.5">
        <div className="size-5 rounded-sm bg-seat-reserved/30 border border-seat-reserved/50 opacity-50" />
        <span>Reserved</span>
      </div>
      <div className="flex items-center gap-1.5">
        <div className="size-5 rounded-sm bg-seat-sold/30 border border-seat-sold/50 opacity-40" />
        <span>Sold</span>
      </div>
      <div className="flex items-center gap-1.5">
        <div className="size-5 rounded-sm ring-1 ring-accent bg-accent border-accent" />
        <span>Selected</span>
      </div>
    </div>
  )
}
