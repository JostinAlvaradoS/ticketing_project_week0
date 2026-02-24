"use client"

import { useMemo, useState, useCallback } from "react"
import type { Seat, SeatmapResponse } from "@/lib/types"
import { SeatButton, SeatLegend } from "@/components/seat-button"
import { useCart } from "@/context/cart-context"
import { Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"

interface SeatmapProps {
  seatmap: SeatmapResponse
  onSeatReserved?: () => void
}

export function Seatmap({ seatmap, onSeatReserved }: SeatmapProps) {
  const { reserveSeatAndAddToCart, isAddingToCart, reservations, isSeatInCart, removeSeatFromCart } = useCart()
  const [selectedSeat, setSelectedSeat] = useState<Seat | null>(null)
  const [localError, setLocalError] = useState<string | null>(null)

  // Group seats by section, then by row
  const sections = useMemo(() => {
    const grouped = new Map<string, Map<number, Seat[]>>()

    for (const seat of seatmap.seats) {
      if (!grouped.has(seat.sectionCode)) {
        grouped.set(seat.sectionCode, new Map())
      }
      const sectionRows = grouped.get(seat.sectionCode)!
      if (!sectionRows.has(seat.rowNumber)) {
        sectionRows.set(seat.rowNumber, [])
      }
      sectionRows.get(seat.rowNumber)!.push(seat)
    }

    // Sort seats within each row
    for (const sectionRows of grouped.values()) {
      for (const [rowNum, seats] of sectionRows) {
        sectionRows.set(
          rowNum,
          seats.sort((a, b) => a.seatNumber - b.seatNumber)
        )
      }
    }

    return grouped
  }, [seatmap.seats])

  const handleSelect = useCallback((seat: Seat) => {
    // Don't allow selection of seats already in cart
    if (isSeatInCart(seat.id)) {
      setSelectedSeat(null)
      return
    }
    setSelectedSeat((prev) => (prev?.id === seat.id ? null : seat))
    setLocalError(null)
  }, [isSeatInCart])

  const handleReserve = useCallback(async () => {
    if (!selectedSeat) return
    
    // If seat is already in cart, remove it
    if (isSeatInCart(selectedSeat.id)) {
      removeSeatFromCart(selectedSeat.id)
      setSelectedSeat(null)
      return
    }
    
    setLocalError(null)

    try {
      await reserveSeatAndAddToCart(selectedSeat)
      setSelectedSeat(null)
      onSeatReserved?.()
    } catch (err) {
      setLocalError(
        err instanceof Error ? err.message : "Failed to reserve seat"
      )
    }
  }, [selectedSeat, reserveSeatAndAddToCart, isSeatInCart, removeSeatFromCart, onSeatReserved])

  return (
    <div className="flex flex-col gap-6">
      {/* Stage visual */}
      <div className="flex justify-center">
        <div className="bg-secondary rounded-lg px-16 py-3 text-muted-foreground text-sm font-medium tracking-widest uppercase">
          Stage
        </div>
      </div>

      {/* Seat grid */}
      <div className="flex flex-col gap-6">
        {Array.from(sections.entries())
          .sort(([a], [b]) => a.localeCompare(b))
          .map(([sectionCode, rows]) => (
            <div key={sectionCode} className="flex flex-col gap-2">
              <h3 className="text-sm font-semibold text-accent uppercase tracking-wider">
                Section {sectionCode}
              </h3>
              <div className="flex flex-col gap-1.5">
                {Array.from(rows.entries())
                  .sort(([a], [b]) => a - b)
                  .map(([rowNumber, seats]) => (
                    <div key={rowNumber} className="flex items-center gap-2">
                      <span className="text-xs text-muted-foreground w-8 text-right font-mono shrink-0">
                        R{rowNumber}
                      </span>
                      <div className="flex items-center gap-1.5 flex-wrap">
                        {seats.map((seat) => {
                          const isInUserCart = isSeatInCart(seat.id)
                          // Show seat from server, but mark as reserved if in user's cart
                          const displaySeat = isInUserCart
                            ? { ...seat, status: "reserved" as const }
                            : seat

                          return (
                            <SeatButton
                              key={seat.id}
                              seat={displaySeat}
                              isSelected={selectedSeat?.id === seat.id}
                              onSelect={handleSelect}
                            />
                          )
                        })}
                      </div>
                    </div>
                  ))}
              </div>
            </div>
          ))}
      </div>

      {/* Legend */}
      <SeatLegend />

      {/* Selected seat action */}
      {selectedSeat && (
        <div className="flex items-center justify-between rounded-lg border border-accent/30 bg-accent/5 p-4">
          <div className="flex flex-col gap-0.5">
            <p className="text-sm font-medium text-foreground">
              Section {selectedSeat.sectionCode}, Row{" "}
              {selectedSeat.rowNumber}, Seat {selectedSeat.seatNumber}
            </p>
            <p className="text-lg font-semibold text-accent">
              ${selectedSeat.price.toFixed(2)}
            </p>
            {isSeatInCart(selectedSeat.id) && (
              <p className="text-xs text-accent mt-1">Already in your cart</p>
            )}
          </div>
          <Button
            onClick={handleReserve}
            disabled={isAddingToCart}
            variant={isSeatInCart(selectedSeat.id) ? "destructive" : "default"}
            className={isSeatInCart(selectedSeat.id) ? "" : "bg-accent text-accent-foreground hover:bg-accent/90"}
          >
            {isAddingToCart ? (
              <>
                <Loader2 className="size-4 animate-spin" />
                {isSeatInCart(selectedSeat.id) ? "Removing..." : "Reserving..."}
              </>
            ) : isSeatInCart(selectedSeat.id) ? (
              "Remove from Cart"
            ) : (
              "Reserve & Add to Cart"
            )}
          </Button>
        </div>
      )}

      {/* Error */}
      {localError && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
          {localError}
        </div>
      )}
    </div>
  )
}
