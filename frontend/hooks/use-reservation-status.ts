/**
 * Hook para monitorear el estado de una reserva asincrónica
 * Valida que los tickets fueron reservados exitosamente
 */

"use client"

import { useState, useEffect } from "react"

interface UseReservationStatusOptions {
  ticketIds?: number[]
  onAllReserved?: () => void
  onQueued?: () => void // Cuando no se confirma pero está en cola
  onReservationFailed?: (reason: string) => void
  maxDuration?: number // en segundos, default 15
  queueTimeout?: number // tiempo antes de mostrar "en cola" (default 8s)
}

export function useReservationStatus({
  ticketIds,
  onAllReserved,
  onQueued,
  onReservationFailed,
  maxDuration = 15,
  queueTimeout = 8,
}: UseReservationStatusOptions) {
  const [isPolling, setIsPolling] = useState(false)
  const [elapsedTime, setElapsedTime] = useState(0)
  const [confirmedCount, setConfirmedCount] = useState(0)
  const [queuedCalled, setQueuedCalled] = useState(false)

  useEffect(() => {
    if (!ticketIds || ticketIds.length === 0 || !isPolling) {
      return
    }

    console.log("[DEBUG] Starting polling for tickets:", ticketIds, "queuedCalled:", queuedCalled)
    
    const startTime = Date.now()
    const maxTime = maxDuration * 1000
    const queueTime = queueTimeout * 1000
    let pollInterval: NodeJS.Timeout | null = null
    let hasCalledCallback = false

    const pollReservationStatus = async () => {
      try {
        const responses = await Promise.all(
          ticketIds.map((id) =>
            fetch(`/api/tickets/${id}`).then((res) =>
              res.ok ? res.json() : null
            )
          )
        )

        const tickets = responses.filter(Boolean)
        const elapsed = Date.now() - startTime

        setElapsedTime(Math.round(elapsed / 1000))
        console.log(`[DEBUG] Polling: ${elapsed}ms elapsed, ${tickets.length} tickets found`)

        // Contar cuántos están reservados
        const reserved = tickets.filter((t) => t.status === "reserved").length
        setConfirmedCount(reserved)

        // Si todos están reservados
        if (reserved === ticketIds.length) {
          console.log("[DEBUG] All tickets reserved! Clearing interval and calling onAllReserved")
          if (pollInterval) clearInterval(pollInterval)
          setIsPolling(false)
          setQueuedCalled(false)
          onAllReserved?.()
          return
        }

        // Si pasó el tiempo de cola pero no se confirmó, mostrar que está en cola
        if (!hasCalledCallback && elapsed > queueTime) {
          console.log("[DEBUG] Queue timeout reached after", elapsed, "ms. Calling onQueued")
          hasCalledCallback = true
          setQueuedCalled(true)
          if (pollInterval) clearInterval(pollInterval)
          setIsPolling(false)
          onQueued?.()
          return
        }

        // Si pasó el tiempo máximo, detener pero el mensaje YA está en RabbitMQ
        if (elapsed > maxTime) {
          console.log("[DEBUG] Max duration reached. Clearing interval")
          if (pollInterval) clearInterval(pollInterval)
          setIsPolling(false)
          return
        }
      } catch (error) {
        console.error("[DEBUG] Error polling reservation status:", error)
        if (pollInterval) clearInterval(pollInterval)
        setIsPolling(false)
        onReservationFailed?.("Error verificando estado de la reserva")
      }
    }

    pollInterval = setInterval(pollReservationStatus, 500) // Poll cada 500ms

    // Hacer una llamada inmediata
    pollReservationStatus()

    return () => {
      console.log("[DEBUG] Cleanup: clearing interval")
      if (pollInterval) clearInterval(pollInterval)
    }
  }, [ticketIds, isPolling, onAllReserved, onQueued, onReservationFailed, maxDuration, queueTimeout])

  return {
    isPolling,
    startPolling: () => {
      setQueuedCalled(false) // Reset cuando se reinicia
      setIsPolling(true)
    },
    stopPolling: () => setIsPolling(false),
    elapsedTime,
    confirmedCount,
  }
}

// Legacy exports for backwards compatibility
export interface UseReservationStatusOptionsLegacy {
  onSuccess?: () => void
  onError?: (error: Error) => void
}
