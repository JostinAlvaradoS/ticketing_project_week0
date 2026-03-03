"use client"

import Link from "next/link"
import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { AdminButton } from "@/components/admin/AdminButton"
import { 
  AdminTable, 
  AdminTableHeader, 
  AdminTableBody, 
  AdminTableRow, 
  AdminTableCell,
  AdminTableLoading,
  AdminTableEmpty
} from "@/components/admin/AdminTable"
import { getEvents, catalogAdminApi, type Event } from "@/lib/api/catalog"
import { useToast } from "@/hooks/use-toast"
import { Badge } from "@/components/ui/badge"

interface EventDisplay {
  id: string
  name: string
  description: string
  date: string
  basePrice: number
  venue: string
  imageUrl?: string
  maxCapacity?: number // Made optional since EventSummary doesn't include it
  eventDate: string
  status: "active" | "inactive"
  seatsCount?: number
}

interface EventFilters {
  search: string
  status: "all" | "active" | "inactive"
  dateFrom: string
  dateTo: string
}

export default function AdminEventsPage() {
  const router = useRouter()
  const { toast } = useToast()
  const [events, setEvents] = useState<EventDisplay[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [filters, setFilters] = useState<EventFilters>({
    search: "",
    status: "all",
    dateFrom: "",
    dateTo: ""
  })
  const [currentPage, setCurrentPage] = useState(1)
  const itemsPerPage = 10

  useEffect(() => {
    fetchEvents()
  }, [filters, currentPage])

  const fetchEvents = async () => {
    setIsLoading(true)
    try {
      const apiEvents = await getEvents()

      // Transform API events to display format
      let filteredEvents: EventDisplay[] = apiEvents.map(event => ({
        id: event.id,
        name: event.name,
        description: event.description,
        date: event.eventDate,
        basePrice: event.basePrice,
        venue: event.venue,
        imageUrl: event.imageUrl,
        maxCapacity: event.maxCapacity,
        eventDate: event.eventDate,
        status: event.isActive ? "active" : "inactive",
        seatsCount: 0 // TODO: Get from seats endpoint if needed
      }))

      // Apply filters
      if (filters.search) {
        filteredEvents = filteredEvents.filter(event =>
          event.name.toLowerCase().includes(filters.search.toLowerCase()) ||
          event.venue.toLowerCase().includes(filters.search.toLowerCase())
        )
      }

      if (filters.status !== "all") {
        filteredEvents = filteredEvents.filter(event => event.status === filters.status)
      }

      setEvents(filteredEvents)
    } catch (error) {
      console.error("Error fetching events:", error)
      toast({
        title: "Error",
        description: "No se pudieron cargar los eventos. Por favor, intenta nuevamente.",
        variant: "destructive",
      })
    } finally {
      setIsLoading(false)
    }
  }

  const handleToggleEventStatus = async (eventId: string, currentStatus: "active" | "inactive") => {
    try {
      if (currentStatus === "active") {
        await catalogAdminApi.deactivateEvent(eventId)
        toast({
          title: "Éxito",
          description: "Evento desactivado correctamente.",
        })
      } else {
        await catalogAdminApi.reactivateEvent(eventId)
        toast({
          title: "Éxito",
          description: "Evento reactivado correctamente.",
        })
      }
      // Refresh events list
      fetchEvents()
    } catch (error) {
      toast({
        title: "Error",
        description: `No se pudo ${currentStatus === "active" ? "desactivar" : "reactivar"} el evento.`,
        variant: "destructive",
      })
    }
  }

  const handleFeatureNotImplemented = (feature: string) => {
    toast({
      title: "Funcionalidad en desarrollo",
      description: `La funcionalidad de ${feature} está en desarrollo y estará disponible pronto.`,
      variant: "default",
    })
  }

  const getStatusBadge = (status: EventDisplay["status"]) => {
    switch (status) {
      case "active":
        return <Badge className="bg-green-100 text-green-800 hover:bg-green-100">Activo</Badge>
      case "inactive":
        return <Badge variant="secondary" className="bg-red-100 text-red-800 hover:bg-red-100">Inactivo</Badge>
      default:
        return <Badge variant="secondary">Desconocido</Badge>
    }
  }

  const formatDate = (dateString: string) => {
    try {
      // Handle various date formats
      const date = new Date(dateString)
      
      // Check if date is valid
      if (isNaN(date.getTime())) {
        return "Fecha inválida"
      }
      
      return date.toLocaleDateString("es-ES", {
        year: "numeric",
        month: "short", 
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit"
      })
    } catch (error) {
      console.error('Error formatting date:', dateString, error)
      return "Fecha inválida"
    }
  }

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat("es-PE", {
      style: "currency",
      currency: "PEN"
    }).format(price)
  }

  const handleFilterChange = (field: keyof EventFilters, value: string) => {
    setFilters(prevFilters => ({
      ...prevFilters,
      [field]: value
    }))
  }

  const handleEventClick = (eventId: string) => {
    router.push(`/admin/events/${eventId}`)
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-gray-300">Eventos</h1>
          <p className="mt-2 text-gray-600">
            Gestiona el catálogo de eventos del sistema
          </p>
        </div>
        <Link href="/admin/events/create">
          <AdminButton>
            + Crear Evento
          </AdminButton>
        </Link>
      </div>

      {/* Events Table */}
      {isLoading ? (
        <AdminTableLoading columns={7} rows={5} />
      ) : events.length === 0 ? (
        <AdminTableEmpty 
          columns={7} 
          message="No se encontraron eventos que coincidan con los filtros"
          icon="🎭"
        />
      ) : (
        <AdminTable>
          <AdminTableHeader>
            <AdminTableRow>
              <AdminTableCell header>Evento</AdminTableCell>
              <AdminTableCell header>Venue</AdminTableCell>
              <AdminTableCell header>Fecha</AdminTableCell>
              <AdminTableCell header>Capacidad</AdminTableCell>
              <AdminTableCell header>Precio Base</AdminTableCell>
              <AdminTableCell header>Estado</AdminTableCell>
              <AdminTableCell header>Acciones</AdminTableCell>
            </AdminTableRow>
          </AdminTableHeader>
          <AdminTableBody>
            {events.map((event) => (
              <AdminTableRow 
                key={event.id}
                onClick={() => handleEventClick(event.id)}
              >
                <AdminTableCell>
                  <div>
                    <p className="font-medium text-gray-300">{event.name}</p>
                    <p className="text-sm text-gray-500">{event.description}</p>
                  </div>
                </AdminTableCell>
                <AdminTableCell>{event.venue}</AdminTableCell>
                <AdminTableCell>{formatDate(event.eventDate)}</AdminTableCell>
                <AdminTableCell>
                  <div className="text-center">
                    <p className="font-medium">{event.maxCapacity?.toLocaleString() || 'N/A'}</p>
                    {event.seatsCount !== undefined && (
                      <p className="text-xs text-gray-500">
                        {event.seatsCount.toLocaleString()} asientos
                      </p>
                    )}
                  </div>
                </AdminTableCell>
                <AdminTableCell className="font-medium">
                  {formatPrice(event.basePrice)}
                </AdminTableCell>
                <AdminTableCell>
                  {getStatusBadge(event.status)}
                </AdminTableCell>
                <AdminTableCell>
                  <div className="flex space-x-2">
                    <AdminButton
                      size="sm"
                      variant="ghost"
                      onClick={(e) => {
                        e.stopPropagation()
                        router.push(`/admin/events/${event.id}/edit`)
                      }}
                    >
                      Editar
                    </AdminButton>
                    <AdminButton
                      size="sm"
                      variant="ghost"
                      onClick={(e) => {
                        e.stopPropagation()
                        router.push(`/admin/events/${event.id}/seats`)
                      }}
                    >
                      Generar Asientos
                    </AdminButton>
                  </div>
                </AdminTableCell>
              </AdminTableRow>
            ))}
          </AdminTableBody>
        </AdminTable>
      )}

      {/* Pagination placeholder */}
      {!isLoading && events.length > 0 && (
        <div className="flex justify-center">
          <div className="bg-white px-4 py-2 rounded-lg shadow text-sm text-gray-600">
            Mostrando {events.length} eventos
          </div>
        </div>
      )}
    </div>
  )
}