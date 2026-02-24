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
    bgClass: "bg-seat-available/20 border-seat-available/50 hover:bg-seat-available/40 hover:border-seat-available cursor-pointer",
    label: "Available",
  },
  reserved: {
    bgClass: "bg-seat-reserved/20 border-seat-reserved/50 cursor-not-allowed opacity-60",
    label: "Reserved",
  },
  sold: {
    bgClass: "bg-seat-sold/20 border-seat-sold/50 cursor-not-allowed opacity-40",
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
      aria-label={`Seat ${seat.sectionCode}${seat.rowNumber}-${seat.seatNumber}, $${seat.price}, ${config.label}`}
      className={cn(
        "flex items-center justify-center rounded-md border text-xs font-medium transition-all size-10",
        config.bgClass,
        isSelected && isClickable && "ring-2 ring-accent bg-accent/30 border-accent"
      )}
    >
      {seat.seatNumber}
    </button>
  )
}

export function SeatLegend() {
  return (
    <div className="flex items-center gap-6 text-sm text-muted-foreground">
      <div className="flex items-center gap-2">
        <div className="size-4 rounded-sm bg-seat-available/40 border border-seat-available/60" />
        <span>Available</span>
      </div>
      <div className="flex items-center gap-2">
        <div className="size-4 rounded-sm bg-seat-reserved/40 border border-seat-reserved/60" />
        <span>Reserved</span>
      </div>
      <div className="flex items-center gap-2">
        <div className="size-4 rounded-sm bg-seat-sold/40 border border-seat-sold/60" />
        <span>Sold</span>
      </div>
      <div className="flex items-center gap-2">
        <div className="size-4 rounded-sm ring-2 ring-accent bg-accent/30 border border-accent" />
        <span>Selected</span>
      </div>
    </div>
  )
}
