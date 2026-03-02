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
  const [events, setEvents] = useState<Event[]>([])
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
      // In a real implementation, this would call your backend API
      // For now, we'll simulate some data
      await new Promise(resolve => setTimeout(resolve, 1000))
      
      const mockEvents: Event[] = [
        {
          id: "1",
          name: "Concierto Rock 2026",
          description: "Gran concierto de rock en el estadio nacional",
          eventDate: "2026-06-15T20:00:00Z",
          venue: "Estadio Nacional",
          maxCapacity: 50000,
          basePrice: 75.00,
          status: "active",
          createdAt: "2026-03-01T10:00:00Z",
          seatsCount: 50000
        },
        {
          id: "2", 
          name: "Festival de Jazz",
          description: "Festival de jazz con artistas internacionales",
          eventDate: "2026-07-20T19:00:00Z",
          venue: "Centro de Convenciones",
          maxCapacity: 5000,
          basePrice: 120.00,
          status: "active",
          createdAt: "2026-02-28T14:30:00Z",
          seatsCount: 5000
        },
        {
          id: "3",
          name: "Teatro Clásico",
          description: "Obra de teatro clásico",
          eventDate: "2026-05-10T20:30:00Z",
          venue: "Teatro Municipal",
          maxCapacity: 800,
          basePrice: 45.00,
          status: "inactive",
          createdAt: "2026-02-25T09:15:00Z",
          seatsCount: 0
        }
      ]

      // Apply filters
      let filteredEvents = mockEvents
      
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
    } finally {
      setIsLoading(false)
    }
  }

  const handleFilterChange = (key: keyof EventFilters, value: string) => {
    setFilters(prev => ({ ...prev, [key]: value }))
    setCurrentPage(1) // Reset to first page when filtering
  }

  const handleEventClick = (eventId: string) => {
    router.push(`/admin/events/${eventId}`)
  }

  const getStatusBadge = (status: Event["status"]) => {
    const baseClasses = "px-2 py-1 text-xs font-medium rounded-full"
    
    switch (status) {
      case "active":
        return `${baseClasses} bg-green-100 text-green-800`
      case "inactive":
        return `${baseClasses} bg-red-100 text-red-800`
      default:
        return `${baseClasses} bg-gray-100 text-gray-800`
    }
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("es-ES", {
      year: "numeric",
      month: "short", 
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

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Eventos</h1>
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

      {/* Filters */}
      <div className="bg-white p-6 rounded-lg shadow space-y-4">
        <h3 className="text-lg font-medium text-gray-900">Filtros</h3>
        
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Buscar
            </label>
            <input
              type="text"
              placeholder="Nombre del evento o venue..."
              value={filters.search}
              onChange={(e) => handleFilterChange("search", e.target.value)}
              className="block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Estado
            </label>
            <select
              value={filters.status}
              onChange={(e) => handleFilterChange("status", e.target.value)}
              className="block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            >
              <option value="all">Todos</option>
              <option value="active">Activos</option>
              <option value="inactive">Inactivos</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Fecha desde
            </label>
            <input
              type="date"
              value={filters.dateFrom}
              onChange={(e) => handleFilterChange("dateFrom", e.target.value)}
              className="block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Fecha hasta
            </label>
            <input
              type="date"
              value={filters.dateTo}
              onChange={(e) => handleFilterChange("dateTo", e.target.value)}
              className="block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            />
          </div>
        </div>
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
                    <p className="font-medium text-gray-900">{event.name}</p>
                    <p className="text-sm text-gray-500">{event.description}</p>
                  </div>
                </AdminTableCell>
                <AdminTableCell>{event.venue}</AdminTableCell>
                <AdminTableCell>{formatDate(event.eventDate)}</AdminTableCell>
                <AdminTableCell>
                  <div className="text-center">
                    <p className="font-medium">{event.maxCapacity.toLocaleString()}</p>
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
                  <span className={getStatusBadge(event.status)}>
                    {event.status === "active" ? "Activo" : "Inactivo"}
                  </span>
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
                      ✏️
                    </AdminButton>
                    <AdminButton
                      size="sm"
                      variant="ghost"
                      onClick={(e) => {
                        e.stopPropagation()
                        router.push(`/admin/events/${event.id}/seats`)
                      }}
                    >
                      🪑
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