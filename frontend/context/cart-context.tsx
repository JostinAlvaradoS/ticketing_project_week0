"use client"

import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  type ReactNode,
} from "react"
import type { Order, ReservationInfo, Seat } from "@/lib/types"
import { addToCartWithRetry, checkout } from "@/lib/api/ordering"
import { createReservation } from "@/lib/api/inventory"
import { processPayment } from "@/lib/api/payment"
import { useAuth } from "@/context/auth-context"

const CART_STORAGE_KEY = "ticketing_cart"
const RESERVATIONS_STORAGE_KEY = "ticketing_reservations"

interface CartContextType {
  order: Order | null
  reservations: ReservationInfo[]
  isAddingToCart: boolean
  isCheckingOut: boolean
  isProcessingPayment: boolean
  error: string | null
  reserveSeatAndAddToCart: (seat: Seat) => Promise<void>
  removeSeatFromCart: (seatId: string) => void
  isSeatInCart: (seatId: string) => boolean
  doCheckout: () => Promise<Order>
  processOrderPayment: () => Promise<Order>
  clearError: () => void
  clearCart: () => void
}

const CartContext = createContext<CartContextType | null>(null)

export function useCart() {
  const ctx = useContext(CartContext)
  if (!ctx) throw new Error("useCart must be used within a CartProvider")
  return ctx
}

export function CartProvider({ children }: { children: ReactNode }) {
  const { userId } = useAuth()
  const [order, setOrder] = useState<Order | null>(null)
  const [reservations, setReservations] = useState<ReservationInfo[]>([])
  const [isAddingToCart, setIsAddingToCart] = useState(false)
  const [isCheckingOut, setIsCheckingOut] = useState(false)
  const [isProcessingPayment, setIsProcessingPayment] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [isHydrated, setIsHydrated] = useState(false)

  // Hydrate from localStorage on mount
  useEffect(() => {
    try {
      const storedOrder = localStorage.getItem(CART_STORAGE_KEY)
      const storedReservations = localStorage.getItem(RESERVATIONS_STORAGE_KEY)

      if (storedOrder) {
        const parsedOrder = JSON.parse(storedOrder)
        setOrder(parsedOrder)
        console.log("[CartProvider] Loaded order from localStorage:", parsedOrder)
      }

      if (storedReservations) {
        const parsedReservations = JSON.parse(storedReservations)
        setReservations(parsedReservations)
        console.log("[CartProvider] Loaded reservations from localStorage:", parsedReservations)
      }
    } catch (err) {
      console.error("[CartProvider] Error loading from localStorage:", err)
    }
    setIsHydrated(true)
  }, [])

  // Persist order to localStorage whenever it changes
  useEffect(() => {
    if (!isHydrated) return
    try {
      if (order) {
        localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(order))
      } else {
        localStorage.removeItem(CART_STORAGE_KEY)
      }
    } catch (err) {
      console.error("[CartProvider] Error saving order to localStorage:", err)
    }
  }, [order, isHydrated])

  // Persist reservations to localStorage whenever they change
  useEffect(() => {
    if (!isHydrated) return
    try {
      if (reservations.length > 0) {
        localStorage.setItem(RESERVATIONS_STORAGE_KEY, JSON.stringify(reservations))
      } else {
        localStorage.removeItem(RESERVATIONS_STORAGE_KEY)
      }
    } catch (err) {
      console.error("[CartProvider] Error saving reservations to localStorage:", err)
    }
  }, [reservations, isHydrated])

  const clearError = useCallback(() => setError(null), [])
  const clearCart = useCallback(() => {
    setOrder(null)
    setReservations([])
    setError(null)
    localStorage.removeItem(CART_STORAGE_KEY)
    localStorage.removeItem(RESERVATIONS_STORAGE_KEY)
    console.log("[CartProvider] Cart cleared and localStorage cleaned")
  }, [])

  const reserveSeatAndAddToCart = useCallback(
    async (seat: Seat) => {
      if (!userId) throw new Error("User not authenticated")
      
      // Check if seat is already in cart
      const seatAlreadyInCart = reservations.some(r => r.seatId === seat.id)
      if (seatAlreadyInCart) {
        throw new Error(`Seat ${seat.sectionCode}${seat.rowNumber}-${seat.seatNumber} is already in your cart`)
      }
      
      setIsAddingToCart(true)
      setError(null)

      try {
        // Step 1: Create reservation
        console.log(`[reserveSeatAndAddToCart] Creating reservation for seat ${seat.id}`)
        const reservation = await createReservation({
          seatId: seat.id,
          customerId: userId,
        })
        console.log(`[reserveSeatAndAddToCart] Reservation created:`, reservation)

        const reservationInfo: ReservationInfo = {
          reservationId: reservation.reservationId,
          seatId: seat.id,
          expiresAt: reservation.expiresAt,
          seat,
        }

        setReservations((prev) => [...prev, reservationInfo])

        // Step 2: Add to cart with retry (Kafka delay)
        console.log(`[reserveSeatAndAddToCart] Adding to cart with retry...`)
        const response = await addToCartWithRetry({
          reservationId: reservation.reservationId,
          seatId: seat.id,
          price: seat.price,
          userId: userId,
        })
        console.log(`[reserveSeatAndAddToCart] Add to cart response:`, response)

        // The server returns the Order directly, not a wrapper object
        if (response && response.id) {
          // Response is the Order object itself
          const order = response as Order
          setOrder(order)
          console.log(`[reserveSeatAndAddToCart] Order created successfully:`, order)
        } else if (response.success && response.order) {
          // Fallback for wrapped response format
          setOrder(response.order)
          console.log(`[reserveSeatAndAddToCart] Order created successfully:`, response.order)
        } else {
          throw new Error(response.errorMessage || "Failed to add to cart")
        }
      } catch (err) {
        // Remove reservation from state if cart add failed
        console.error(`[reserveSeatAndAddToCart] Error:`, err)
        setReservations((prev) => prev.filter(r => r.seatId !== seat.id))
        
        const message =
          err instanceof Error ? err.message : "An unexpected error occurred"
        setError(message)
        throw err
      } finally {
        setIsAddingToCart(false)
      }
    },
    [userId, reservations]
  )

  const isSeatInCart = useCallback(
    (seatId: string) => reservations.some(r => r.seatId === seatId),
    [reservations]
  )

  const removeSeatFromCart = useCallback((seatId: string) => {
    setReservations((prev) => prev.filter(r => r.seatId !== seatId))
    if (order && order.items) {
      // Filter out the item with this seatId from the order
      const updatedItems = order.items.filter(item => item.seatId !== seatId)
      const newTotal = updatedItems.reduce((sum, item) => sum + item.price, 0)
      setOrder({
        ...order,
        items: updatedItems,
        totalAmount: newTotal,
      })
    }
  }, [order])

  const doCheckout = useCallback(async () => {
    if (!order) throw new Error("No order to checkout")
    if (!userId) throw new Error("User not authenticated")
    setIsCheckingOut(true)
    setError(null)
    try {
      const result = await checkout({
        orderId: order.id,
        userId: userId,
      })
      setOrder(result)
      
      // Clear cart and reservations after successful checkout
      if (result && result.state && result.state !== "draft") {
        setReservations([])
        localStorage.removeItem(RESERVATIONS_STORAGE_KEY)
        console.log("[doCheckout] Cart reservations cleared after successful checkout")
      }
      
      return result
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Checkout failed"
      setError(message)
      throw err
    } finally {
      setIsCheckingOut(false)
    }
  }, [order, userId])

  const processOrderPayment = useCallback(async () => {
    if (!order) throw new Error("No order to pay")
    if (!userId) throw new Error("User not authenticated")
    setIsProcessingPayment(true)
    setError(null)
    try {
      console.log("[processOrderPayment] Processing payment for order", order.id)
      const paymentResponse = await processPayment({
        orderId: order.id,
        customerId: userId,
        amount: order.totalAmount / 100, // Convert from cents to dollars
        currency: "USD",
        paymentMethod: "credit_card"
      })
      
      console.log("[processOrderPayment] Payment successful:", paymentResponse)
      
      // Update the existing order with payment information
      const updatedOrder: Order = {
        ...order,
        state: "paid",
        paidAt: new Date().toISOString()
      }
      
      setOrder(updatedOrder)
      
      // Clear cart after successful payment
      setReservations([])
      localStorage.removeItem(RESERVATIONS_STORAGE_KEY)
      console.log("[processOrderPayment] Cart cleared after successful payment")
      
      return updatedOrder
    } catch (err) {
      const message = err instanceof Error ? err.message : "Payment processing failed"
      setError(message)
      throw err
    } finally {
      setIsProcessingPayment(false)
    }
  }, [order, userId])

  return (
    <CartContext.Provider
      value={{
        order,
        reservations,
        isAddingToCart,
        isCheckingOut,
        isProcessingPayment,
        error,
        reserveSeatAndAddToCart,
        removeSeatFromCart,
        isSeatInCart,
        doCheckout,
        processOrderPayment,
        clearError,
        clearCart,
      }}
    >
      {children}
    </CartContext.Provider>
  )
}
