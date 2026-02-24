"use client"

import Link from "next/link"
import { ShoppingCart, Trash2 } from "lucide-react"
import { useCart } from "@/context/cart-context"
import { CountdownTimer } from "@/components/countdown-timer"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"

export function CartSidebar() {
  const { order, reservations, error, clearError, removeSeatFromCart } = useCart()
  const itemCount = order?.items.length ?? 0

  return (
    <Card className="bg-card border-border">
      <CardHeader className="pb-3">
        <CardTitle className="flex items-center gap-2 text-foreground">
          <ShoppingCart className="size-5" />
          <span>Cart</span>
          {itemCount > 0 && (
            <span className="ml-auto text-sm font-normal text-muted-foreground">
              {itemCount} {itemCount === 1 ? "item" : "items"}
            </span>
          )}
        </CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        {error && (
          <div className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
            <p>{error}</p>
            <button
              onClick={clearError}
              className="mt-1 text-xs underline hover:no-underline"
            >
              Dismiss
            </button>
          </div>
        )}

        {itemCount === 0 ? (
          <p className="text-sm text-muted-foreground py-4 text-center">
            No items in cart. Select a seat to get started.
          </p>
        ) : (
          <>
            {/* Cart items */}
            <div className="flex flex-col gap-3">
              {order?.items.map((item) => {
                const reservation = reservations.find(
                  (r) => r.seatId === item.seatId
                )
                return (
                  <div
                    key={item.id}
                    className="flex items-center justify-between rounded-md border border-border bg-secondary/50 p-3"
                  >
                    <div className="flex flex-col gap-0.5 flex-1">
                      {reservation?.seat ? (
                        <p className="text-sm font-medium text-foreground">
                          Sec {reservation.seat.sectionCode}, Row{" "}
                          {reservation.seat.rowNumber}, Seat{" "}
                          {reservation.seat.seatNumber}
                        </p>
                      ) : (
                        <p className="text-sm font-medium text-foreground">
                          Seat
                        </p>
                      )}
                      {reservation && (
                        <div className="flex items-center gap-1.5">
                          <span className="text-xs text-muted-foreground">
                            Expires:
                          </span>
                          <CountdownTimer expiresAt={reservation.expiresAt} />
                        </div>
                      )}
                    </div>
                    <div className="flex items-center gap-2 ml-2">
                      <p className="text-sm font-semibold text-foreground">
                        ${item.price.toFixed(2)}
                      </p>
                      <button
                        onClick={() => removeSeatFromCart(item.seatId)}
                        className="p-1 hover:bg-destructive/20 rounded transition-colors"
                        aria-label="Remove item"
                      >
                        <Trash2 className="size-4 text-destructive" />
                      </button>
                    </div>
                  </div>
                )
              })}
            </div>

            <Separator className="bg-border" />

            {/* Total */}
            <div className="flex items-center justify-between">
              <span className="text-sm text-muted-foreground uppercase tracking-wider font-medium">
                Subtotal
              </span>
              <span className="text-lg font-bold text-foreground">
                ${order?.totalAmount.toFixed(2)}
              </span>
            </div>

            {/* Checkout button */}
            <Button
              asChild
              className="w-full bg-accent text-accent-foreground hover:bg-accent/90"
            >
              <Link href="/checkout">Checkout</Link>
            </Button>
          </>
        )}
      </CardContent>
    </Card>
  )
}
