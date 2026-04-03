"use client"

import Link from "next/link"
import { ShoppingCart, Trash2, Loader2, ArrowRight } from "lucide-react"
import { useCart } from "@/context/cart-context"
import { CountdownTimer } from "@/components/countdown-timer"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { cn } from "@/lib/utils"

export function CartSidebar() {
  const { order, reservations, error, clearError, removeSeatFromCart, isAddingToCart } = useCart()
  const itemCount = order?.items.length ?? 0

  return (
    <div className="rounded-xl border border-border bg-card overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3.5 border-b border-border bg-secondary/30">
        <div className="flex items-center gap-2">
          <ShoppingCart className="size-4 text-accent" />
          <span className="font-semibold text-sm text-foreground">Your Cart</span>
        </div>
        {itemCount > 0 && (
          <span className="flex size-5 items-center justify-center rounded-full bg-accent text-accent-foreground text-xs font-bold">
            {itemCount}
          </span>
        )}
      </div>

      <div className="flex flex-col gap-4 p-4">
        {error && (
          <div className="rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
            <p>{error}</p>
            <button onClick={clearError} className="mt-1 text-xs underline hover:no-underline">
              Dismiss
            </button>
          </div>
        )}

        {itemCount === 0 ? (
          <div className="flex flex-col items-center gap-3 py-8 text-center">
            <div className="flex size-12 items-center justify-center rounded-full bg-secondary border border-border">
              <ShoppingCart className="size-5 text-muted-foreground" />
            </div>
            <div>
              <p className="text-sm font-medium text-foreground">Your cart is empty</p>
              <p className="text-xs text-muted-foreground mt-0.5">Select a seat to get started</p>
            </div>
          </div>
        ) : (
          <>
            <div className="flex flex-col gap-2">
              {order?.items.map((item) => {
                const reservation = reservations.find((r) => r.seatId === item.seatId)
                return (
                  <div
                    key={item.id}
                    className="flex items-center justify-between rounded-lg border border-border bg-secondary/40 p-3 gap-2"
                  >
                    <div className="flex flex-col gap-1 flex-1 min-w-0">
                      {reservation?.seat ? (
                        <p className="text-sm font-medium text-foreground truncate">
                          Sec {reservation.seat.sectionCode} · R{reservation.seat.rowNumber} · #{reservation.seat.seatNumber}
                        </p>
                      ) : (
                        <p className="text-sm font-medium text-foreground">Seat</p>
                      )}
                      {reservation && (
                        <div className="flex items-center gap-1">
                          <span className="text-xs text-muted-foreground">Expires in</span>
                          <CountdownTimer
                            expiresAt={reservation.expiresAt}
                            onExpired={() => removeSeatFromCart(item.seatId)}
                          />
                        </div>
                      )}
                    </div>
                    <div className="flex items-center gap-2 shrink-0">
                      <p className="text-sm font-bold text-foreground">${item.price.toFixed(2)}</p>
                      <button
                        onClick={() => removeSeatFromCart(item.seatId)}
                        className="p-1 rounded-md hover:bg-destructive/20 transition-colors"
                        aria-label="Remove item"
                      >
                        <Trash2 className="size-3.5 text-muted-foreground hover:text-destructive" />
                      </button>
                    </div>
                  </div>
                )
              })}
            </div>

            <Separator className="bg-border" />

            <div className="flex items-center justify-between">
              <span className="text-xs text-muted-foreground uppercase tracking-widest font-medium">Total</span>
              <span className="text-xl font-bold text-foreground">${order?.totalAmount.toFixed(2)}</span>
            </div>

            <Button
              asChild
              disabled={isAddingToCart}
              className="w-full bg-accent text-accent-foreground hover:bg-accent/90 gap-1.5"
            >
              <Link href="/checkout">
                {isAddingToCart ? (
                  <>
                    <Loader2 className="size-4 animate-spin" />
                    Updating...
                  </>
                ) : (
                  <>
                    Checkout
                    <ArrowRight className="size-4" />
                  </>
                )}
              </Link>
            </Button>
          </>
        )}
      </div>
    </div>
  )
}
