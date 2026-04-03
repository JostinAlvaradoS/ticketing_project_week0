"use client"

import { useMemo, useState, useCallback } from "react"
import type { Seat, SeatmapResponse } from "@/lib/types"
import { SeatButton, SeatLegend } from "@/components/seat-button"
import { WaitlistModal } from "@/components/waitlist-modal"
import { useCart } from "@/context/cart-context"
import { Loader2, Users, AlertCircle } from "lucide-react"
import { Button } from "@/components/ui/button"

interface SeatmapProps {
  seatmap: SeatmapResponse
  onSeatReserved?: () => void
}

export function Seatmap({ seatmap, onSeatReserved }: SeatmapProps) {
  const { reserveSeatAndAddToCart, isAddingToCart, reservations, isSeatInCart, removeSeatFromCart } = useCart()
  const [selectedSeat, setSelectedSeat] = useState<Seat | null>(null)
  const [localError, setLocalError] = useState<string | null>(null)
  const [showWaitlistModal, setShowWaitlistModal] = useState(false)

  // Detect sold out: no available seats (excluding ones in user's cart)
  const isSoldOut = useMemo(() =>
    seatmap.seats.every(s => s.status !== "available"),
    [seatmap.seats]
  )

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

    for (const sectionRows of grouped.values()) {
      for (const [rowNum, seats] of sectionRows) {
        sectionRows.set(rowNum, seats.sort((a, b) => a.seatNumber - b.seatNumber))
      }
    }

    return grouped
  }, [seatmap.seats])

  const handleSelect = useCallback((seat: Seat) => {
    if (isSeatInCart(seat.id)) {
      setSelectedSeat(null)
      return
    }
    setSelectedSeat((prev) => (prev?.id === seat.id ? null : seat))
    setLocalError(null)
  }, [isSeatInCart])

  const handleReserve = useCallback(async () => {
    if (!selectedSeat) return

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
      setLocalError(err instanceof Error ? err.message : "Failed to reserve seat")
    }
  }, [selectedSeat, reserveSeatAndAddToCart, isSeatInCart, removeSeatFromCart, onSeatReserved])

  return (
    <div className="flex flex-col gap-6">
      {/* Sold-out banner */}
      {isSoldOut && (
        <div className="flex flex-col items-center gap-4 rounded-xl border border-destructive/30 bg-destructive/5 px-6 py-8 text-center">
          <div className="flex size-12 items-center justify-center rounded-full bg-destructive/10 border border-destructive/20">
            <AlertCircle className="size-6 text-destructive" />
          </div>
          <div className="flex flex-col gap-1">
            <p className="font-semibold text-foreground text-lg">This event is sold out</p>
            <p className="text-sm text-muted-foreground max-w-xs">
              All seats are currently reserved or sold. Join the waitlist and we'll notify you when one becomes available.
            </p>
          </div>
          <Button
            onClick={() => setShowWaitlistModal(true)}
            className="bg-accent text-accent-foreground hover:bg-accent/90 gap-2"
          >
            <Users className="size-4" />
            Join the Waitlist
          </Button>
        </div>
      )}

      {/* Stage visual */}
      <div className="relative">
        <div className="bg-secondary/80 border border-border rounded-lg py-2.5 text-center text-muted-foreground text-xs font-semibold tracking-[0.2em] uppercase w-full">
          Stage
        </div>
        <div className="absolute inset-x-0 -bottom-3 h-3 bg-gradient-to-b from-accent/10 to-transparent rounded-b-lg" />
      </div>

      {/* Seat map — scrolls horizontally; sections are side-by-side columns */}
      <div className="overflow-x-auto rounded-lg border border-border/40 bg-secondary/20 p-4">
        {/* min-w-max forces this div to be as wide as its content, enabling x-scroll */}
        <div className="flex items-start gap-6 min-w-max">
          {Array.from(sections.entries())
            .sort(([a], [b]) => a.localeCompare(b))
            .map(([sectionCode, rows], idx, arr) => {
              const allSeatsInSection = Array.from(rows.values()).flat()
              const availableInSection = allSeatsInSection.filter(s => s.status === "available").length
              return (
                <div key={sectionCode} className="flex items-start">
                  {/* Section column */}
                  <div className="flex flex-col gap-1">
                    {/* Section header */}
                    <div className="flex flex-col items-center pb-2 mb-1 border-b border-border/50">
                      <span className="text-[10px] font-bold text-accent uppercase tracking-widest whitespace-nowrap">
                        {sectionCode}
                      </span>
                      {!isSoldOut && (
                        <span className="text-[9px] text-muted-foreground whitespace-nowrap">
                          {availableInSection} avail.
                        </span>
                      )}
                    </div>
                    {/* Rows */}
                    <div className="flex flex-col gap-0.5">
                      {Array.from(rows.entries())
                        .sort(([a], [b]) => a - b)
                        .map(([rowNumber, seats]) => (
                          <div key={rowNumber} className="flex items-center gap-1">
                            <span className="text-[9px] text-muted-foreground/60 w-4 text-right font-mono shrink-0 leading-none select-none">
                              {rowNumber}
                            </span>
                            {/* flex-nowrap ensures seats never wrap to next line */}
                            <div className="flex flex-nowrap items-center gap-px">
                              {seats.map((seat) => {
                                const isInUserCart = isSeatInCart(seat.id)
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
                  {/* Divider between sections */}
                  {idx < arr.length - 1 && (
                    <div className="w-px self-stretch bg-border/50 mx-5" />
                  )}
                </div>
              )
            })}
        </div>
      </div>

      {/* Legend */}
      <SeatLegend />

      {/* Selected seat action */}
      {selectedSeat && !isSoldOut && (
        <div className="flex items-center justify-between rounded-xl border border-accent/30 bg-accent/5 p-4">
          <div className="flex flex-col gap-0.5">
            <p className="text-sm font-semibold text-foreground">
              Section {selectedSeat.sectionCode}, Row {selectedSeat.rowNumber}, Seat {selectedSeat.seatNumber}
            </p>
            <p className="text-xl font-bold text-accent">
              ${selectedSeat.price.toFixed(2)}
            </p>
            {isSeatInCart(selectedSeat.id) && (
              <p className="text-xs text-accent mt-0.5">Already in your cart</p>
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

      {/* Waitlist Modal */}
      <WaitlistModal
        open={showWaitlistModal}
        onClose={() => setShowWaitlistModal(false)}
        eventId={seatmap.eventId}
        eventName={seatmap.eventName}
      />
    </div>
  )
}
