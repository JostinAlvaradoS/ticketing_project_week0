"use client"

import { useState, useEffect, use } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { AdminButton } from "@/components/admin/AdminButton"
import { getEvent, catalogAdminApi, type Event } from "@/lib/api/catalog"
import { useToast } from "@/hooks/use-toast"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { Calendar, MapPin, Users, DollarSign, Edit, Settings } from "lucide-react"

export default function EventDetailPage({ 
  params 
}: { 
  params: Promise<{ eventId: string }> 
}) {
  const { eventId } = use(params)
  const router = useRouter()
  const { toast } = useToast()
  const [event, setEvent] = useState<Event | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string>("")

  useEffect(() => {
    fetchEvent()
  }, [eventId])

  const fetchEvent = async () => {
    setIsLoading(true)
    try {
      const fetchedEvent = await getEvent(eventId)
      
      if (!fetchedEvent) {
        setError("Evento no encontrado")
        toast({
          title: "Error",
          description: "Evento no encontrado.",
          variant: "destructive",
        })
        router.push("/admin/events")
        return
      }

      setEvent(fetchedEvent)
    } catch (error) {
      console.error("Error fetching event:", error)
      setError("Error al cargar el evento")
      toast({
        title: "Error",
        description: "Error al cargar el evento.",
        variant: "destructive",
      })
    } finally {
      setIsLoading(false)
    }
  }

  const handleStatusToggle = async () => {
    if (!event) return

    try {
      if (event.isActive) {
        await catalogAdminApi.deactivateEvent(event.id)
        toast({
          title: "Éxito",
          description: "Evento desactivado correctamente.",
        })
      } else {
        await catalogAdminApi.reactivateEvent(event.id)
        toast({
          title: "Éxito",
          description: "Evento reactivado correctamente.",
        })
      }
      
      // Refresh event data
      fetchEvent()
    } catch (error) {
      console.error("Error updating event status:", error)
      toast({
        title: "Error",
        description: "Error al cambiar el estado del evento.",
        variant: "destructive",
      })
    }
  }

  const handleFeatureNotImplemented = (feature: string) => {
    toast({
      title: "Funcionalidad en desarrollo",
      description: `La funcionalidad de ${feature} está en desarrollo y estará disponible pronto.`,
    })
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

  if (isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <Skeleton className="h-8 w-64 mb-2" />
            <Skeleton className="h-4 w-96" />
          </div>
          <div className="flex space-x-3">
            <Skeleton className="h-10 w-24" />
            <Skeleton className="h-10 w-24" />
          </div>
        </div>
        
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2">
            <Card>
              <CardContent className="p-6">
                <Skeleton className="h-4 w-full mb-4" />
                <Skeleton className="h-4 w-3/4 mb-4" />
                <Skeleton className="h-4 w-1/2" />
              </CardContent>
            </Card>
          </div>
          <div>
            <Card>
              <CardContent className="p-6">
                <Skeleton className="h-6 w-32 mb-4" />
                <Skeleton className="h-4 w-24 mb-2" />
                <Skeleton className="h-4 w-24 mb-2" />
                <Skeleton className="h-4 w-24" />
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    )
  }

  if (error || !event) {
    return (
      <div className="max-w-6xl mx-auto space-y-6">
        <div className="text-center">
          <h1 className="text-2xl font-bold mb-4">Error</h1>
          <p className="text-muted-foreground">{error || "Evento no encontrado"}</p>
          <Link href="/admin/events" className="mt-4 inline-block">
            <AdminButton>Volver a Eventos</AdminButton>
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{event.name}</h1>
          <p className="mt-2 text-muted-foreground">
            Gestiona la información y configuración del evento
          </p>
        </div>
        <div className="flex space-x-3">
          <AdminButton variant="outline" asChild>
            <Link href="/admin/events">
              ← Volver a Eventos
            </Link>
          </AdminButton>
          <AdminButton asChild>
            <Link href={`/admin/events/${event.id}/edit`}>
              <Edit className="h-4 w-4 mr-2" />
              Editar
            </Link>
          </AdminButton>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Content */}
        <div className="lg:col-span-2 space-y-6">
          {/* Event Information */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                Información del Evento
                <Badge variant={event.isActive ? "default" : "secondary"}>
                  {event.isActive ? "Activo" : "Inactivo"}
                </Badge>
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <h3 className="font-medium mb-2">Descripción</h3>
                <p className="text-muted-foreground">{event.description}</p>
              </div>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="flex items-center">
                  <Calendar className="h-5 w-5 text-muted-foreground mr-2" />
                  <div>
                    <p className="font-medium">Fecha del Evento</p>
                    <p className="text-sm text-muted-foreground">{formatDate(event.eventDate)}</p>
                  </div>
                </div>
                
                <div className="flex items-center">
                  <MapPin className="h-5 w-5 text-muted-foreground mr-2" />
                  <div>
                    <p className="font-medium">Venue</p>
                    <p className="text-sm text-muted-foreground">{event.venue}</p>
                  </div>
                </div>
                
                <div className="flex items-center">
                  <Users className="h-5 w-5 text-muted-foreground mr-2" />
                  <div>
                    <p className="font-medium">Capacidad Máxima</p>
                    <p className="text-sm text-muted-foreground">{event.maxCapacity?.toLocaleString() || 'N/A'} personas</p>
                  </div>
                </div>
                
                <div className="flex items-center">
                  <DollarSign className="h-5 w-5 text-muted-foreground mr-2" />
                  <div>
                    <p className="font-medium">Precio Base</p>
                    <p className="text-sm text-muted-foreground">{formatPrice(event.basePrice)}</p>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Actions */}
          <Card>
            <CardHeader>
              <CardTitle>Gestión del Evento</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex flex-wrap gap-3">
                
                <AdminButton 
                  variant="outline"
                    onClick={(e) => {
                        e.stopPropagation()
                        router.push(`/admin/events/${event.id}/seats`)
                    }}   
                >
                  <Settings className="h-4 w-4 mr-2" />
                  Generar Asientos
                </AdminButton>

              </div>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Quick Stats */}
          <Card>
            <CardHeader>
              <CardTitle>Estadísticas Rápidas</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <Card>
                <CardContent className="text-center p-4">
                  <p className="text-2xl font-bold">{event.totalSeats ?? 0}</p>
                  <p className="text-sm text-muted-foreground">Asientos Generados</p>
                </CardContent>
              </Card>
              
              <Card>
                <CardContent className="text-center p-4">
                  <p className="text-2xl font-bold">{event.soldSeats ?? 0}</p>
                  <p className="text-sm text-muted-foreground">Tickets Vendidos</p>
                </CardContent>
              </Card>
              
              <Card>
                <CardContent className="text-center p-4">
                  <p className="text-2xl font-bold">{formatPrice(event.revenue ?? 0)}</p>
                  <p className="text-sm text-muted-foreground">Ingresos</p>
                </CardContent>
              </Card>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}