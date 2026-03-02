"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { AdminButton } from "@/components/admin/AdminButton"

interface Event {
  id: string
  name: string  
  description: string
  eventDate: string
  venue: string
  maxCapacity: number
  basePrice: number
  status: "active" | "inactive"
  createdAt: string
  imageUrl?: string
  tags: string[]
  seatsCount?: number
  availableSeats?: number
  soldSeats?: number
}

export default function EventDetailPage({ 
  params 
}: { 
  params: { eventId: string } 
}) {
  const router = useRouter()
  const [event, setEvent] = useState<Event | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string>("")

  useEffect(() => {
    fetchEvent()
  }, [params.eventId])

  const fetchEvent = async () => {
    setIsLoading(true)
    try {
      // In a real implementation, this would call your backend API
      await new Promise(resolve => setTimeout(resolve, 1000))
      
      // Mock event data
      const mockEvent: Event = {
        id: params.eventId,
        name: "Concierto Rock 2026",
        description: "Gran concierto de rock en el estadio nacional con artistas internacionales. Un evento que promete ser inolvidable con las mejores bandas del género.",
        eventDate: "2026-06-15T20:00:00Z",
        venue: "Estadio Nacional",
        maxCapacity: 50000,
        basePrice: 75.00,
        status: "active",
        createdAt: "2026-03-01T10:00:00Z",
        imageUrl: "https://example.com/concert-image.jpg",
        tags: ["rock", "música", "concierto", "nacional"],
        seatsCount: 50000,
        availableSeats: 47500,
        soldSeats: 2500
      }

      setEvent(mockEvent)
    } catch (error) {
      console.error("Error fetching event:", error)
      setError("Error al cargar el evento")
    } finally {
      setIsLoading(false)
    }
  }

  const handleStatusToggle = async () => {
    if (!event) return

    try {
      // In a real implementation, this would call your backend API
      const newStatus = event.status === "active" ? "inactive" : "active"
      
      setEvent(prev => prev ? { ...prev, status: newStatus } : null)
      
      console.log(`Event status changed to: ${newStatus}`)
    } catch (error) {
      console.error("Error updating event status:", error)
    }
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("es-ES", {
      weekday: "long",
      year: "numeric",
      month: "long",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit"
    })
  }

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat("es-PE", {
      style: "currency", 
      currency: "PEN"
    }).format(price)
  }

  const getStatusBadge = (status: Event["status"]) => {
    const baseClasses = "px-3 py-1 text-sm font-medium rounded-full"
    
    switch (status) {
      case "active":
        return `${baseClasses} bg-green-100 text-green-800`
      case "inactive":
        return `${baseClasses} bg-red-100 text-red-800`
      default:
        return `${baseClasses} bg-gray-100 text-gray-800`
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="animate-pulse">
          <div className="h-8 bg-gray-200 rounded w-1/3 mb-4"></div>
          <div className="h-4 bg-gray-200 rounded w-2/3 mb-6"></div>
          <div className="bg-white shadow rounded-lg p-6 space-y-4">
            <div className="h-4 bg-gray-200 rounded w-full"></div>
            <div className="h-4 bg-gray-200 rounded w-3/4"></div>
            <div className="h-4 bg-gray-200 rounded w-1/2"></div>
          </div>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <p className="text-red-600 mb-4">{error}</p>
        <Link href="/admin/events">
          <AdminButton>Volver a Eventos</AdminButton>
        </Link>
      </div>
    )
  }

  if (!event) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-600 mb-4">Evento no encontrado</p>
        <Link href="/admin/events">
          <AdminButton>Volver a Eventos</AdminButton>
        </Link>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center space-x-3">
            <h1 className="text-3xl font-bold text-gray-900">{event.name}</h1>
            <span className={getStatusBadge(event.status)}>
              {event.status === "active" ? "Activo" : "Inactivo"}
            </span>
          </div>
          <p className="mt-2 text-gray-600">ID: {event.id}</p>
        </div>
        <div className="flex space-x-3">
          <Link href="/admin/events">
            <AdminButton variant="ghost">
              ← Volver
            </AdminButton>
          </Link>
          <Link href={`/admin/events/${event.id}/edit`}>
            <AdminButton variant="secondary">
              Editar
            </AdminButton>  
          </Link>
          <AdminButton onClick={handleStatusToggle}>
            {event.status === "active" ? "Desactivar" : "Activar"}
          </AdminButton>
        </div>
      </div>

      {/* Main Info */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Event Details */}
        <div className="lg:col-span-2 space-y-6">
          {/* Basic Information */}
          <div className="bg-white shadow rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4">
              Información del Evento
            </h3>
            <dl className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <dt className="text-sm font-medium text-gray-500">Descripción</dt>
                <dd className="mt-1 text-sm text-gray-900">{event.description}</dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">Venue</dt>
                <dd className="mt-1 text-sm text-gray-900">{event.venue}</dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">Fecha y Hora</dt>
                <dd className="mt-1 text-sm text-gray-900">{formatDate(event.eventDate)}</dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">Precio Base</dt>
                <dd className="mt-1 text-sm text-gray-900 font-medium">
                  {formatPrice(event.basePrice)}
                </dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">Capacidad Máxima</dt>
                <dd className="mt-1 text-sm text-gray-900">
                  {event.maxCapacity.toLocaleString()} personas
                </dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">Creado</dt>
                <dd className="mt-1 text-sm text-gray-900">
                  {formatDate(event.createdAt)}
                </dd>
              </div>
            </dl>

            {event.tags.length > 0 && (
              <div className="mt-4">
                <dt className="text-sm font-medium text-gray-500 mb-2">Tags</dt>
                <div className="flex flex-wrap gap-2">
                  {event.tags.map((tag, index) => (
                    <span
                      key={index}
                      className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800"
                    >
                      {tag}
                    </span>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Image */}
          {event.imageUrl && (
            <div className="bg-white shadow rounded-lg p-6">
              <h3 className="text-lg font-medium text-gray-900 mb-4">
                Imagen del Evento
              </h3>
              <img
                src={event.imageUrl}
                alt={event.name}
                className="w-full h-64 object-cover rounded-lg"
                onError={(e) => {
                  const target = e.target as HTMLImageElement
                  target.src = "/placeholder-event.jpg"
                }}
              />
            </div>
          )}
        </div>

        {/* Sidebar - Stats & Actions */}
        <div className="space-y-6">
          {/* Capacity Stats */}
          <div className="bg-white shadow rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4">
              Estadísticas de Asientos
            </h3>
            <div className="space-y-4">
              <div>
                <div className="flex justify-between items-center">
                  <span className="text-sm text-gray-600">Total</span>
                  <span className="text-sm font-medium">
                    {event.seatsCount?.toLocaleString() || 0}
                  </span>
                </div>
              </div>
              
              <div>
                <div className="flex justify-between items-center">
                  <span className="text-sm text-gray-600">Disponibles</span>
                  <span className="text-sm font-medium text-green-600">
                    {event.availableSeats?.toLocaleString() || 0}
                  </span>
                </div>
                <div className="mt-1 bg-gray-200 rounded-full h-2">
                  <div 
                    className="bg-green-600 h-2 rounded-full" 
                    style={{
                      width: `${event.seatsCount ? (event.availableSeats! / event.seatsCount) * 100 : 0}%`
                    }}
                  ></div>
                </div>
              </div>

              <div>
                <div className="flex justify-between items-center">
                  <span className="text-sm text-gray-600">Vendidos</span>
                  <span className="text-sm font-medium text-blue-600">
                    {event.soldSeats?.toLocaleString() || 0}
                  </span>
                </div>
                <div className="mt-1 bg-gray-200 rounded-full h-2">
                  <div 
                    className="bg-blue-600 h-2 rounded-full" 
                    style={{
                      width: `${event.seatsCount ? (event.soldSeats! / event.seatsCount) * 100 : 0}%`
                    }}
                  ></div>
                </div>
              </div>
            </div>
          </div>

          {/* Quick Actions */}
          <div className="bg-white shadow rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4">
              Acciones Rápidas
            </h3>
            <div className="space-y-3">
              <AdminButton 
                className="w-full justify-center" 
                onClick={() => router.push(`/admin/events/${event.id}/seats`)}
              >
                🪑 Gestionar Asientos
              </AdminButton>
              
              <AdminButton 
                variant="ghost" 
                className="w-full justify-center"
                onClick={() => router.push(`/admin/events/${event.id}/sales`)}
              >
                📊 Ver Ventas
              </AdminButton>
              
              <AdminButton 
                variant="ghost" 
                className="w-full justify-center"
                onClick={() => router.push(`/admin/events/${event.id}/reports`)}
              >
                📋 Reportes
              </AdminButton>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}