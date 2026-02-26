"use client"

import { useState } from "react"
import Link from "next/link"
import { ArrowLeft, CheckCircle2, Loader2, ShoppingCart, AlertCircle } from "lucide-react"
import { useCart } from "@/context/cart-context"
import { CountdownTimer } from "@/components/countdown-timer"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import type { Order } from "@/lib/types"

export function CheckoutClient() {
  const { order, reservations, doCheckout, isCheckingOut, processOrderPayment, isProcessingPayment, error, clearCart } =
    useCart()
  const [completedOrder, setCompletedOrder] = useState<Order | null>(null)
  const [checkoutError, setCheckoutError] = useState<string | null>(null)
  const [paymentStep, setPaymentStep] = useState<'checkout' | 'processing-payment' | 'completed'>('checkout')

  const handleCheckout = async () => {
    setCheckoutError(null)
    try {
      // Step 1: Checkout (draft -> pending)
      const checkoutResult = await doCheckout()
      setCompletedOrder(checkoutResult)
      setPaymentStep('processing-payment')
      
      // Step 2: Process Payment (pending -> paid)
      const paidOrder = await processOrderPayment()
      setCompletedOrder(paidOrder)
      setPaymentStep('completed')
    } catch (err) {
      setCheckoutError(
        err instanceof Error ? err.message : "Checkout or payment failed"
      )
    }
  }

  // Processing payment
  if (paymentStep === 'processing-payment') {
    return (
      <main className="min-h-screen bg-background flex items-center justify-center px-4">
        <Card className="bg-card border-border max-w-lg w-full">
          <CardContent className="p-8 flex flex-col items-center gap-6 text-center">
            <div className="size-16 rounded-full bg-accent/20 flex items-center justify-center">
              <Loader2 className="size-8 text-accent animate-spin" />
            </div>
            <div className="flex flex-col gap-2">
              <h1 className="text-2xl font-bold text-foreground">
                Processing Payment
              </h1>
              <p className="text-muted-foreground leading-relaxed">
                Your order has been confirmed. We're now processing your payment...
              </p>
            </div>
          </CardContent>
        </Card>
      </main>
    )
  }

  // Payment completed successfully
  if (paymentStep === 'completed' && completedOrder && completedOrder.state === 'paid') {
    return (
      <main className="min-h-screen bg-background flex items-center justify-center px-4">
        <Card className="bg-card border-border max-w-lg w-full">
          <CardContent className="p-8 flex flex-col items-center gap-6 text-center">
            <div className="size-16 rounded-full bg-green-500/20 flex items-center justify-center">
              <CheckCircle2 className="size-8 text-green-500" />
            </div>
            <div className="flex flex-col gap-2">
              <h1 className="text-2xl font-bold text-foreground">
                Payment Successful!
              </h1>
              <p className="text-muted-foreground leading-relaxed">
                Your payment has been processed successfully. Your tickets are confirmed!
              </p>
            </div>

            <div className="w-full rounded-lg border border-border bg-secondary/30 p-4 text-left">
              <div className="flex flex-col gap-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Order ID</span>
                  <span className="font-mono text-foreground">
                    {completedOrder.id}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Status</span>
                  <span className="font-medium text-green-500 capitalize">
                    {completedOrder.state}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Items</span>
                  <span className="text-foreground">
                    {completedOrder.items.length}
                  </span>
                </div>
                <Separator className="bg-border my-1" />
                <div className="flex justify-between">
                  <span className="font-medium text-foreground">Total</span>
                  <span className="font-bold text-foreground">
                    ${completedOrder.totalAmount.toFixed(2)}
                  </span>
                </div>
              </div>
            </div>

            <Button
              asChild
              className="w-full bg-accent text-accent-foreground hover:bg-accent/90"
              onClick={() => clearCart()}
            >
              <Link href="/">Browse More Events</Link>
            </Button>
          </CardContent>
        </Card>
      </main>
    )
  }

  // Checkout completed (legacy state - should not show in normal flow)
  if (completedOrder && completedOrder.state !== "draft") {
    return (
      <main className="min-h-screen bg-background flex items-center justify-center px-4">
        <Card className="bg-card border-border max-w-lg w-full">
          <CardContent className="p-8 flex flex-col items-center gap-6 text-center">
            <div className="size-16 rounded-full bg-accent/20 flex items-center justify-center">
              <CheckCircle2 className="size-8 text-accent" />
            </div>
            <div className="flex flex-col gap-2">
              <h1 className="text-2xl font-bold text-foreground">
                Order Confirmed
              </h1>
              <p className="text-muted-foreground leading-relaxed">
                Your order has been placed successfully. The order is now{" "}
                <span className="font-medium text-seat-reserved">
                  {completedOrder.state}
                </span>{" "}
                and awaiting payment processing.
              </p>
            </div>

            <div className="w-full rounded-lg border border-border bg-secondary/30 p-4 text-left">
              <div className="flex flex-col gap-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Order ID</span>
                  <span className="font-mono text-foreground">
                    {completedOrder.id}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Status</span>
                  <span className="font-medium text-accent capitalize">
                    {completedOrder.state}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Items</span>
                  <span className="text-foreground">
                    {completedOrder.items.length}
                  </span>
                </div>
                <Separator className="bg-border my-1" />
                <div className="flex justify-between">
                  <span className="font-medium text-foreground">Total</span>
                  <span className="font-bold text-foreground">
                    ${completedOrder.totalAmount.toFixed(2)}
                  </span>
                </div>
              </div>
            </div>

            <Button
              asChild
              className="w-full bg-accent text-accent-foreground hover:bg-accent/90"
              onClick={() => clearCart()}
            >
              <Link href="/">Browse More Events</Link>
            </Button>
          </CardContent>
        </Card>
      </main>
    )
  }

  // No order in cart
  if (!order || order.items.length === 0) {
    return (
      <main className="min-h-screen bg-background flex items-center justify-center px-4">
        <Card className="bg-card border-border max-w-lg w-full">
          <CardContent className="p-8 flex flex-col items-center gap-6 text-center">
            <div className="size-16 rounded-full bg-secondary flex items-center justify-center">
              <ShoppingCart className="size-8 text-muted-foreground" />
            </div>
            <div className="flex flex-col gap-2">
              <h1 className="text-2xl font-bold text-foreground">
                Your Cart is Empty
              </h1>
              <p className="text-muted-foreground">
                Add some seats to your cart before checking out.
              </p>
            </div>
            <Button asChild variant="outline">
              <Link href="/">Browse Events</Link>
            </Button>
          </CardContent>
        </Card>
      </main>
    )
  }

  // Checkout form
  return (
    <main className="min-h-screen bg-background">
      <div className="mx-auto max-w-3xl px-4 py-8">
        {/* Back */}
        <Button
          variant="ghost"
          asChild
          className="mb-6 text-muted-foreground hover:text-foreground"
        >
          <Link href="/">
            <ArrowLeft className="size-4" />
            Back to Events
          </Link>
        </Button>

        <div className="flex flex-col gap-6">
          <h1 className="text-3xl font-bold text-foreground tracking-tight">
            Checkout
          </h1>

          {/* Order summary */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-foreground">Order Summary</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-4">
              {order.items.map((item) => {
                const reservation = reservations.find(
                  (r) => r.seatId === item.seatId
                )
                return (
                  <div
                    key={item.id}
                    className="flex items-center justify-between rounded-md border border-border bg-secondary/50 p-4"
                  >
                    <div className="flex flex-col gap-1">
                      {reservation?.seat ? (
                        <p className="text-sm font-medium text-foreground">
                          Section {reservation.seat.sectionCode}, Row{" "}
                          {reservation.seat.rowNumber}, Seat{" "}
                          {reservation.seat.seatNumber}
                        </p>
                      ) : (
                        <p className="text-sm font-medium text-foreground">
                          Ticket
                        </p>
                      )}
                      {reservation && (
                        <div className="flex items-center gap-1.5">
                          <span className="text-xs text-muted-foreground">
                            Reservation expires:
                          </span>
                          <CountdownTimer
                            expiresAt={reservation.expiresAt}
                          />
                        </div>
                      )}
                    </div>
                    <p className="text-base font-semibold text-foreground">
                      ${item.price.toFixed(2)}
                    </p>
                  </div>
                )
              })}

              <Separator className="bg-border" />

              <div className="flex items-center justify-between">
                <span className="text-sm text-muted-foreground uppercase tracking-wider font-medium">
                  Total
                </span>
                <span className="text-2xl font-bold text-foreground">
                  ${order.totalAmount.toFixed(2)}
                </span>
              </div>

              {/* Errors */}
              {(checkoutError || error) && (
                <div className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive flex items-start gap-2">
                  <AlertCircle className="size-4 shrink-0 mt-0.5" />
                  <p>{checkoutError || error}</p>
                </div>
              )}

              {/* Checkout action */}
              <Button
                onClick={handleCheckout}
                disabled={isCheckingOut}
                className="w-full bg-accent text-accent-foreground hover:bg-accent/90 h-12 text-base"
              >
                {isCheckingOut ? (
                  <>
                    <Loader2 className="size-5 animate-spin" />
                    Processing...
                  </>
                ) : (
                  `Pay $${order.totalAmount.toFixed(2)}`
                )}
              </Button>

              <p className="text-xs text-muted-foreground text-center">
                Your order will be placed in pending status ready for payment
                processing.
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </main>
  )
}
