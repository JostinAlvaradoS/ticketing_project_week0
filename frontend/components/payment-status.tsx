/**
 * Payment Status Component
 * Muestra el estado del pago (procesando, éxito, error)
 */

"use client"

import { CheckCircle2, XCircle, Loader2, Mail } from "lucide-react"
import { Card } from "@/components/ui/card"
import { Button } from "@/components/ui/button"

type PaymentStatus = "idle" | "processing" | "pending" | "error"

interface PaymentStatusProps {
  status: PaymentStatus
  error?: string
  email?: string
  onReset: () => void
}

export function PaymentStatus({ status, error, email, onReset }: PaymentStatusProps) {
  if (status === "idle") {
    return null
  }

  return (
    <Card className="border-border bg-card p-6">
      {status === "processing" && (
        <div className="space-y-4 text-center">
          <div className="flex justify-center">
            <Loader2 className="h-12 w-12 animate-spin text-blue-500" />
          </div>
          <div>
            <h3 className="text-lg font-semibold text-foreground">Procesando Pago</h3>
            <p className="mt-2 text-sm text-muted-foreground">
              Por favor espera mientras procesamos tu pago...
            </p>
            <p className="mt-4 text-xs text-muted-foreground">
              ⏱️ Esto puede tomar algunos segundos
            </p>
          </div>
        </div>
      )}

      {status === "pending" && (
        <div className="space-y-4 text-center">
          <div className="flex justify-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-emerald-500/10">
              <CheckCircle2 className="h-10 w-10 text-emerald-500" />
            </div>
          </div>
          <div>
            <h3 className="text-lg font-semibold text-foreground">Pago Procesado</h3>
            <p className="mt-2 text-sm text-muted-foreground">
              Tu pago ha sido recibido y está siendo procesado
            </p>
            <div className="mt-4 flex items-center justify-center gap-2 rounded-lg bg-blue-500/10 px-4 py-3">
              <Mail className="h-5 w-5 text-blue-500" />
              <p className="text-sm text-blue-600 dark:text-blue-400">
                Tus boletos llegarán a <strong>{email}</strong> cuando se confirme el pago
              </p>
            </div>
            <p className="mt-4 text-xs text-muted-foreground">
              ✓ Si todo es correcto, podrás descargar tus boletos en 2-5 minutos
            </p>
          </div>
          <Button onClick={onReset} className="mt-4 w-full">
            Volver a Eventos
          </Button>
        </div>
      )}

      {status === "error" && (
        <div className="space-y-4 text-center">
          <div className="flex justify-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10">
              <XCircle className="h-10 w-10 text-destructive" />
            </div>
          </div>
          <div>
            <h3 className="text-lg font-semibold text-foreground">Pago Rechazado</h3>
            <p className="mt-2 text-sm text-muted-foreground">{error || "Ocurrió un error al procesar el pago"}</p>
            <p className="mt-1 text-xs text-muted-foreground">
              El ticket ha sido liberado y está disponible para otros usuarios
            </p>
          </div>
          <Button onClick={onReset} variant="outline" className="mt-4 w-full">
            Intentar de Nuevo
          </Button>
        </div>
      )}
    </Card>
  )
}
