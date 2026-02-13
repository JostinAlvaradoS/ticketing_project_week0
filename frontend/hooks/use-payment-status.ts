/**
 * Hook para monitorear el estado de pago
 * Usa polling para esperar a que el estado del ticket cambie a "paid"
 */

import { useEffect, useState, useRef } from "react"
import { api } from "@/lib/api"

interface UsePaymentStatusOptions {
  ticketId?: number
  onPaymentConfirmed?: () => void
  onPaymentRejected?: (reason: string) => void
  maxDuration?: number // en segundos, default 10
}

export function usePaymentStatus({
  ticketId,
  onPaymentConfirmed,
  onPaymentRejected,
  maxDuration = 10,
}: UsePaymentStatusOptions) {
  const [isPolling, setIsPolling] = useState(false)
  const [elapsedTime, setElapsedTime] = useState(0)
  const isMountedRef = useRef(true)

  useEffect(() => {
    isMountedRef.current = true
    return () => {
      isMountedRef.current = false
    }
  }, [])

  useEffect(() => {
    if (!ticketId || !isPolling) {
      return
    }

    const startTime = Date.now()
    const maxTime = maxDuration * 1000
    let pollInterval: NodeJS.Timeout | null = null

    const pollPaymentStatus = async () => {
      try {
        const ticket = await api.getTicket(ticketId)
        const elapsed = Date.now() - startTime

        if (!isMountedRef.current) return

        setElapsedTime(Math.round(elapsed / 1000))

        // Si el pago fue aprobado
        if (ticket.status === "paid") {
          if (pollInterval) clearInterval(pollInterval)
          setIsPolling(false)
          onPaymentConfirmed?.()
          return
        }

        // Si pasó el tiempo máximo, asumimos que fue rechazado
        if (elapsed > maxTime) {
          if (pollInterval) clearInterval(pollInterval)
          setIsPolling(false)
          onPaymentRejected?.("El pago tardó demasiado en confirmarse")
          return
        }
      } catch (error) {
        if (!isMountedRef.current) return
        
        if (process.env.NODE_ENV === "development") {
          console.error("Error polling payment status:", error)
        }
        if (pollInterval) clearInterval(pollInterval)
        setIsPolling(false)
        onPaymentRejected?.("Error verificando estado del pago")
      }
    }

    pollInterval = setInterval(pollPaymentStatus, 500) // Poll cada 500ms
    
    // Hacer una llamada inmediata
    pollPaymentStatus()

    return () => {
      if (pollInterval) clearInterval(pollInterval)
    }
  }, [ticketId, isPolling, onPaymentConfirmed, onPaymentRejected, maxDuration])

  return {
    isPolling,
    startPolling: () => setIsPolling(true),
    stopPolling: () => setIsPolling(false),
    elapsedTime,
  }
}
