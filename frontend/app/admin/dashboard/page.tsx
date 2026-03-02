"use client"

import Link from "next/link"
import { useState, useEffect } from "react"
import { AdminButton } from "@/components/admin/AdminButton"

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
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000))
      
      // Mock dashboard data
      const mockStats: DashboardStats = {
        totalEvents: 15,
        activeEvents: 12,
        totalSeats: 125000,
        soldSeats: 47500,
        totalRevenue: 3750000,
        pendingOrders: 8
      }

      const mockActivity: RecentActivity[] = [
        {
          id: "1",
          type: "event_created",
          message: "Nuevo evento creado",
          timestamp: "2026-03-10T14:30:00Z",
          eventName: "Festival de Jazz 2026"
        },
        {
          id: "2",
          type: "seats_generated", 
          message: "Asientos generados para evento",
          timestamp: "2026-03-10T13:15:00Z",
          eventName: "Concierto Rock 2026"
        },
        {
          id: "3",
          type: "order_completed",
          message: "Orden completada - 15 tickets vendidos",
          timestamp: "2026-03-10T12:45:00Z",
          eventName: "Teatro Clásico"
        },
        {
          id: "4",
          type: "event_updated",
          message: "Información del evento actualizada",
          timestamp: "2026-03-10T11:20:00Z",
          eventName: "Concierto Rock 2026"
        }
      ]

      setStats(mockStats)
      setRecentActivity(mockActivity)
    } catch (error) {
      console.error("Error fetching dashboard data:", error)
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
      case "event_created": return "🎭"
      case "event_updated": return "✏️"
      case "seats_generated": return "🪑"
      case "order_completed": return "🎫"
      default: return "📋"
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="animate-pulse">
          <div className="h-8 bg-gray-200 rounded w-1/3 mb-6"></div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="bg-white p-6 rounded-lg shadow">
                <div className="h-4 bg-gray-200 rounded w-1/2 mb-2"></div>
                <div className="h-8 bg-gray-200 rounded w-2/3"></div>
              </div>
            ))}
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-8">
      {/* Welcome Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Panel de Administración</h1>
          <p className="mt-2 text-gray-600">
            Gestiona eventos, asientos y ventas desde una sola plataforma
          </p>
        </div>
        <div className="flex space-x-3">
          <Link href="/admin/events/create">
            <AdminButton>
              + Crear Evento
            </AdminButton>
          </Link>
        </div>
      </div>

      {/* Key Performance Indicators */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {/* Events Stats */}
        <div className="bg-white p-6 rounded-lg shadow">
          <div className="flex items-center">
            <div className="p-3 bg-blue-100 rounded-lg">
              <span className="text-2xl">🎭</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600">Eventos Totales</p>
              <div className="flex items-baseline">
                <p className="text-2xl font-bold text-gray-900">{stats.totalEvents}</p>
                <p className="ml-2 text-sm text-green-600">
                  {stats.activeEvents} activos
                </p>
              </div>
            </div>
          </div>
        </div>

        {/* Capacity Stats */}
        <div className="bg-white p-6 rounded-lg shadow">
          <div className="flex items-center">
            <div className="p-3 bg-green-100 rounded-lg">
              <span className="text-2xl">🪑</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600">Asientos</p>
              <div className="flex items-baseline">
                <p className="text-2xl font-bold text-gray-900">
                  {stats.soldSeats.toLocaleString()}
                </p>
                <p className="ml-1 text-sm text-gray-500">
                  / {stats.totalSeats.toLocaleString()}
                </p>
              </div>
              <div className="mt-1 bg-gray-200 rounded-full h-2">
                <div 
                  className="bg-green-600 h-2 rounded-full" 
                  style={{ width: `${(stats.soldSeats / stats.totalSeats) * 100}%` }}
                ></div>
              </div>
            </div>
          </div>
        </div>

        {/* Revenue Stats */}
        <div className="bg-white p-6 rounded-lg shadow">
          <div className="flex items-center">
            <div className="p-3 bg-yellow-100 rounded-lg">
              <span className="text-2xl">💰</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600">Ingresos Totales</p>
              <p className="text-2xl font-bold text-gray-900">
                {formatCurrency(stats.totalRevenue)}
              </p>
            </div>
          </div>
        </div>

        {/* Pending Orders */}
        <div className="bg-white p-6 rounded-lg shadow">
          <div className="flex items-center">
            <div className="p-3 bg-orange-100 rounded-lg">
              <span className="text-2xl">⏳</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600">Órdenes Pendientes</p>
              <p className="text-2xl font-bold text-orange-600">{stats.pendingOrders}</p>
            </div>
          </div>
        </div>

        {/* Occupancy Rate */}
        <div className="bg-white p-6 rounded-lg shadow">
          <div className="flex items-center">
            <div className="p-3 bg-purple-100 rounded-lg">
              <span className="text-2xl">📊</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600">Ocupación Promedio</p>
              <p className="text-2xl font-bold text-purple-600">
                {Math.round((stats.soldSeats / stats.totalSeats) * 100)}%
              </p>
            </div>
          </div>
        </div>

        {/* Revenue per Seat */}
        <div className="bg-white p-6 rounded-lg shadow">
          <div className="flex items-center">
            <div className="p-3 bg-indigo-100 rounded-lg">
              <span className="text-2xl">🎟️</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600">Precio Promedio</p>
              <p className="text-2xl font-bold text-indigo-600">
                {formatCurrency(stats.totalRevenue / stats.soldSeats)}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Quick Actions & Recent Activity */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Quick Actions */}
        <div className="lg:col-span-1">
          <div className="bg-white shadow rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4">
              Acciones Rápidas
            </h3>
            <div className="space-y-3">
              <Link href="/admin/events/create" className="block">
                <AdminButton className="w-full justify-start">
                  🎭 Crear Evento
                </AdminButton>
              </Link>
              
              <Link href="/admin/events" className="block">
                <AdminButton variant="ghost" className="w-full justify-start">
                  📋 Ver Todos los Eventos
                </AdminButton>
              </Link>
              
              <Link href="/admin/events?status=active" className="block">
                <AdminButton variant="ghost" className="w-full justify-start">
                  ✅ Eventos Activos
                </AdminButton>
              </Link>
              
              <Link href="/admin/reports" className="block">
                <AdminButton variant="ghost" className="w-full justify-start">
                  📊 Ver Reportes
                </AdminButton>
              </Link>
            </div>
          </div>
        </div>

        {/* Recent Activity */}
        <div className="lg:col-span-2">
          <div className="bg-white shadow rounded-lg p-6">
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-medium text-gray-900">
                Actividad Reciente
              </h3>
              <AdminButton variant="ghost" size="sm">
                Ver todas
              </AdminButton>
            </div>
            
            <div className="flow-root">
              <ul className="divide-y divide-gray-200">
                {recentActivity.map((activity) => (
                  <li key={activity.id} className="py-3">
                    <div className="flex items-center space-x-4">
                      <div className="text-2xl">{getActivityIcon(activity.type)}</div>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-gray-900">
                          {activity.message}
                        </p>
                        {activity.eventName && (
                          <p className="text-sm text-gray-500">
                            {activity.eventName}
                          </p>
                        )}
                      </div>
                      <div className="text-sm text-gray-500">
                        {formatDate(activity.timestamp)}
                      </div>
                    </div>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </div>
      </div>

      {/* Quick Stats Grid */}
      <div className="bg-white shadow rounded-lg p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">
          Resumen de Performance
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="text-center p-4 border rounded-lg">
            <p className="text-3xl font-bold text-blue-600">
              {Math.round((stats.activeEvents / stats.totalEvents) * 100)}%
            </p>
            <p className="text-sm text-gray-600 mt-1">Eventos Activos</p>
          </div>
          
          <div className="text-center p-4 border rounded-lg">
            <p className="text-3xl font-bold text-green-600">
              {Math.round((stats.soldSeats / stats.totalSeats) * 100)}%
            </p>
            <p className="text-sm text-gray-600 mt-1">Asientos Vendidos</p>
          </div>
          
          <div className="text-center p-4 border rounded-lg">
            <p className="text-3xl font-bold text-yellow-600">
              {formatCurrency(stats.totalRevenue / stats.totalEvents)}
            </p>
            <p className="text-sm text-gray-600 mt-1">Ingreso por Evento</p>
          </div>
          
          <div className="text-center p-4 border rounded-lg">
            <p className="text-3xl font-bold text-purple-600">
              {Math.round(stats.totalSeats / stats.totalEvents)}
            </p>
            <p className="text-sm text-gray-600 mt-1">Capacidad Promedio</p>
          </div>
        </div>
      </div>
    </div>
  )
}