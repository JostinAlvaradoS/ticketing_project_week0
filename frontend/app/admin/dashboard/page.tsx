"use client"

import Link from "next/link"
import { useState, useEffect } from "react"
import { AdminButton } from "@/components/admin/AdminButton"
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Progress } from "@/components/ui/progress"
import { Skeleton } from "@/components/ui/skeleton"
import { getEvents } from "@/lib/api/catalog"
import { useToast } from "@/hooks/use-toast"
import { 
  Theater, 
  Armchair, 
  DollarSign, 
  Clock, 
  BarChart3, 
  Ticket,
  Plus,
  List,
  CheckCircle,
  FileText,
  Calendar,
  Edit,
  Users,
  CheckSquare
} from "lucide-react"

interface DashboardStats {
  totalEvents: number
  activeEvents: number
  totalSeats: number
  soldSeats: number
  totalRevenue: number
  pendingOrders: number
}

interface RecentActivity {
  id: string
  type: "event_created" | "event_updated" | "seats_generated" | "order_completed"
  message: string
  timestamp: string
  eventName?: string
}

export default function AdminDashboard() {
  const { toast } = useToast()
  const [stats, setStats] = useState<DashboardStats>({
    totalEvents: 0,
    activeEvents: 0,
    totalSeats: 0,
    soldSeats: 0,
    totalRevenue: 0,
    pendingOrders: 0
  })

  const [recentActivity, setRecentActivity] = useState<RecentActivity[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    fetchDashboardData()
  }, [])

  const fetchDashboardData = async () => {
    setIsLoading(true)
    try {
      // Fetch real events from catalog API
      const events = await getEvents()
      
      // Calculate real statistics from events
      const totalEvents = events.length
      const activeEvents = events.filter(e => e.isActive).length
      
      // Aggregate real stats from API response
      const totalSeats = events.reduce((sum, e) => sum + (e.totalSeats ?? 0), 0)
      const soldSeats = events.reduce((sum, e) => sum + (e.soldSeats ?? 0), 0)
      const totalRevenue = events.reduce((sum, e) => sum + (e.revenue ?? 0), 0)

      const realStats: DashboardStats = {
        totalEvents,
        activeEvents,
        totalSeats,
        soldSeats,
        totalRevenue,
        pendingOrders: 0
      }

      const mockActivity: RecentActivity[] = [
        {
          id: "1",
          type: "event_created",
          message: "Nuevo evento creado",
          timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
          eventName: events[0]?.name || "Evento reciente"
        },
        {
          id: "2",
          type: "seats_generated", 
          message: "Asientos generados para evento",
          timestamp: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(),
          eventName: events[1]?.name || "Evento con asientos"
        },
        {
          id: "3",
          type: "order_completed",
          message: "Orden completada - 15 tickets vendidos",
          timestamp: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
          eventName: events[0]?.name || "Evento con ventas"
        },
        {
          id: "4",
          type: "event_updated",
          message: "Información del evento actualizada",
          timestamp: new Date(Date.now() - 8 * 60 * 60 * 1000).toISOString(),
          eventName: events[1]?.name || "Evento actualizado"
        }
      ]

      setStats(realStats)
      setRecentActivity(mockActivity)
      
      // Show info toast about limited functionality
      if (events.length === 0) {
        toast({
          title: "Sin eventos",
          description: "No hay eventos creados aún. Crea tu primer evento para comenzar.",
        })
      }
    } catch (error) {
      console.error("Error fetching dashboard data:", error)
      toast({
        title: "Error",
        description: "No se pudieron cargar las estadísticas del dashboard.",
        variant: "destructive",
      })
    } finally {
      setIsLoading(false)
    }
  }

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat("es-PE", {
      style: "currency",
      currency: "PEN"
    }).format(amount)
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString("es-ES", {
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit"
    })
  }

  const getActivityIcon = (type: RecentActivity["type"]) => {
    switch (type) {
      case "event_created": return <Calendar className="h-4 w-4" />
      case "event_updated": return <Edit className="h-4 w-4" />
      case "seats_generated": return <Armchair className="h-4 w-4" />
      case "order_completed": return <Ticket className="h-4 w-4" />
      default: return <FileText className="h-4 w-4" />
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div>
          <Skeleton className="h-8 w-1/3 mb-2" />
          <Skeleton className="h-4 w-2/3 mb-6" />
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
            {Array.from({ length: 6 }).map((_, i) => (
              <Card key={i}>
                <CardContent className="p-6">
                  <Skeleton className="h-4 w-1/2 mb-2" />
                  <Skeleton className="h-8 w-2/3" />
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </div>
    )
  }

  const occupancyRate = stats.totalSeats > 0 ? Math.round((stats.soldSeats / stats.totalSeats) * 100) : 0

  return (
    <div className="space-y-8">
      {/* Welcome Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold">Panel de Administración</h1>
          <p className="mt-2 text-muted-foreground">
            Gestiona eventos, asientos y ventas desde una sola plataforma
          </p>
        </div>
        <div className="flex space-x-3">
          <AdminButton asChild>
            <Link href="/admin/events/create">
              <Plus className="h-4 w-4 mr-2" />
              Crear Evento
            </Link>
          </AdminButton>
        </div>
      </div>

      {/* Key Performance Indicators */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {/* Events Stats */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center">
              <div className="p-3 bg-primary/10 rounded-lg">
                <Theater className="h-6 w-6 text-primary" />
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-muted-foreground">Eventos Totales</p>
                <div className="flex items-baseline">
                  <p className="text-2xl font-bold">{stats.totalEvents}</p>
                  <Badge variant="secondary" className="ml-2">
                    {stats.activeEvents} activos
                  </Badge>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Capacity Stats */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center">
              <div className="p-3 bg-green-100 rounded-lg">
                <Armchair className="h-6 w-6 text-green-600" />
              </div>
              <div className="ml-4 flex-1">
                <p className="text-sm font-medium text-muted-foreground">Asientos</p>
                <div className="flex items-baseline">
                  <p className="text-2xl font-bold">
                    {stats.soldSeats.toLocaleString()}
                  </p>
                  <p className="ml-1 text-sm text-muted-foreground">
                    / {stats.totalSeats.toLocaleString()}
                  </p>
                </div>
                <Progress value={occupancyRate} className="mt-2" />
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Revenue Stats */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center">
              <div className="p-3 bg-yellow-100 rounded-lg">
                <DollarSign className="h-6 w-6 text-yellow-600" />
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-muted-foreground">Ingresos Totales</p>
                <p className="text-2xl font-bold">
                  {formatCurrency(stats.totalRevenue)}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Pending Orders */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center">
              <div className="p-3 bg-orange-100 rounded-lg">
                <Clock className="h-6 w-6 text-orange-600" />
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-muted-foreground">Órdenes Pendientes</p>
                <p className="text-2xl font-bold text-orange-600">{stats.pendingOrders}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Occupancy Rate */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center">
              <div className="p-3 bg-purple-100 rounded-lg">
                <BarChart3 className="h-6 w-6 text-purple-600" />
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-muted-foreground">Ocupación Promedio</p>
                <p className="text-2xl font-bold text-purple-600">
                  {occupancyRate}%
                </p>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Revenue per Seat */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center">
              <div className="p-3 bg-indigo-100 rounded-lg">
                <Ticket className="h-6 w-6 text-indigo-600" />
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-muted-foreground">Precio Promedio</p>
                <p className="text-2xl font-bold text-indigo-600">
                  {stats.totalRevenue / stats.soldSeats}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Quick Actions & Recent Activity */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Quick Actions */}
        <div className="lg:col-span-1">
          <Card>
            <CardHeader>
              <CardTitle>Acciones Rápidas</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <AdminButton className="w-full justify-start" asChild>
                <Link href="/admin/events/create">
                  <Theater className="h-4 w-4 mr-2" />
                  Crear Evento
                </Link>
              </AdminButton>
              
              <AdminButton variant="ghost" className="w-full justify-start" asChild>
                <Link href="/admin/events">
                  <List className="h-4 w-4 mr-2" />
                  Ver Todos los Eventos
                </Link>
              </AdminButton>
              
              <AdminButton variant="ghost" className="w-full justify-start" asChild>
                <Link href="/admin/events?status=active">
                  <CheckCircle className="h-4 w-4 mr-2" />
                  Eventos Activos
                </Link>
              </AdminButton>
              
              <AdminButton variant="ghost" className="w-full justify-start" asChild>
                <Link href="/admin/reports">
                  <BarChart3 className="h-4 w-4 mr-2" />
                  Ver Reportes
                </Link>
              </AdminButton>
            </CardContent>
          </Card>
        </div>

        {/* Recent Activity */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>Actividad Reciente</CardTitle>
              <AdminButton variant="ghost" size="sm">
                Ver todas
              </AdminButton>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {recentActivity.map((activity) => (
                  <div key={activity.id} className="flex items-center space-x-4">
                    <div className="p-2 bg-muted rounded-lg">
                      {getActivityIcon(activity.type)}
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium">
                        {activity.message}
                      </p>
                      {activity.eventName && (
                        <p className="text-sm text-muted-foreground">
                          {activity.eventName}
                        </p>
                      )}
                    </div>
                    <div className="text-sm text-muted-foreground">
                      {formatDate(activity.timestamp)}
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Quick Stats Grid */}
      <Card>
        <CardHeader>
          <CardTitle>Resumen de Performance</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <Card>
              <CardContent className="text-center p-4">
                <p className="text-3xl font-bold text-primary">
                  {Math.round((stats.activeEvents / stats.totalEvents) * 100)}%
                </p>
                <p className="text-sm text-muted-foreground mt-1">Eventos Activos</p>
              </CardContent>
            </Card>
            
            <Card>
              <CardContent className="text-center p-4">
                <p className="text-3xl font-bold text-green-600">
                  {occupancyRate}%
                </p>
                <p className="text-sm text-muted-foreground mt-1">Asientos Vendidos</p>
              </CardContent>
            </Card>
            
            <Card>
              <CardContent className="text-center p-4">
                <p className="text-3xl font-bold text-yellow-600">
                  {formatCurrency(stats.totalRevenue / stats.totalEvents)}
                </p>
                <p className="text-sm text-muted-foreground mt-1">Ingreso por Evento</p>
              </CardContent>
            </Card>
            
            <Card>
              <CardContent className="text-center p-4">
                <p className="text-3xl font-bold text-purple-600">
                  {stats.totalEvents > 0 ? Math.round(stats.totalSeats / stats.totalEvents) : 0}
                </p>
                <p className="text-sm text-muted-foreground mt-1">Capacidad Promedio</p>
              </CardContent>
            </Card>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}