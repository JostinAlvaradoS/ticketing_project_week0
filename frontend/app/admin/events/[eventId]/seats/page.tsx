"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { AdminButton } from "@/components/admin/AdminButton"
import { AdminForm, AdminFormSection } from "@/components/admin/AdminForm"

interface Seat {
  id: string
  section: string
  row: string
  number: string
  price: number
  status: "available" | "reserved" | "sold"
}

interface SeatStats {
  total: number
  available: number
  reserved: number
  sold: number
}

interface Event {
  id: string
  name: string
  venue: string
  maxCapacity: number
  basePrice: number
}

export default function EventSeatsPage({ 
  params 
}: { 
  params: { eventId: string } 
}) {
  const router = useRouter()
  const [event, setEvent] = useState<Event | null>(null)
  const [seats, setSeats] = useState<Seat[]>([])
  const [stats, setStats] = useState<SeatStats>({ total: 0, available: 0, reserved: 0, sold: 0 })
  const [isLoading, setIsLoading] = useState(true)
  const [isGenerating, setIsGenerating] = useState(false)
  const [selectedSeats, setSelectedSeats] = useState<Set<string>>(new Set())
  
  // Seat generation form
  const [generateForm, setGenerateForm] = useState({
    sections: [
      { name: "VIP", rows: 5, seatsPerRow: 10, basePrice: 150 },
      { name: "Platea", rows: 20, seatsPerRow: 25, basePrice: 100 },
      { name: "General", rows: 30, seatsPerRow: 30, basePrice: 75 }
    ]
  })

  useEffect(() => {
    fetchEvent()
    fetchSeats()
  }, [params.eventId])

  const fetchEvent = async () => {
    try {
      // Mock event data
      const mockEvent: Event = {
        id: params.eventId,
        name: "Concierto Rock 2026",
        venue: "Estadio Nacional",
        maxCapacity: 50000,
        basePrice: 75.00
      }
      setEvent(mockEvent)
    } catch (error) {
      console.error("Error fetching event:", error)
    }
  }

  const fetchSeats = async () => {
    setIsLoading(true)
    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000))
      
      // Mock seats data - empty to start with
      const mockSeats: Seat[] = []
      
      setSeats(mockSeats)
      
      // Calculate stats
      const statsData: SeatStats = {
        total: mockSeats.length,
        available: mockSeats.filter(s => s.status === "available").length,
        reserved: mockSeats.filter(s => s.status === "reserved").length,
        sold: mockSeats.filter(s => s.status === "sold").length
      }
      setStats(statsData)
      
    } catch (error) {
      console.error("Error fetching seats:", error)
    } finally {
      setIsLoading(false)
    }
  }

  const generateSeats = async () => {
    setIsGenerating(true)
    try {
      // Simulate seat generation
      await new Promise(resolve => setTimeout(resolve, 2000))
      
      const newSeats: Seat[] = []
      let seatId = 1
      
      generateForm.sections.forEach(section => {
        for (let row = 1; row <= section.rows; row++) {
          for (let seatNum = 1; seatNum <= section.seatsPerRow; seatNum++) {
            newSeats.push({
              id: seatId.toString(),
              section: section.name,
              row: row.toString().padStart(2, "0"),
              number: seatNum.toString().padStart(2, "0"),
              price: section.basePrice,
              status: "available"
            })
            seatId++
          }
        }
      })
      
      setSeats(newSeats)
      
      // Update stats
      setStats({
        total: newSeats.length,
        available: newSeats.length,
        reserved: 0,
        sold: 0
      })
      
    } catch (error) {
      console.error("Error generating seats:", error)
    } finally {
      setIsGenerating(false)
    }
  }

  const handleSeatClick = (seatId: string) => {
    setSelectedSeats(prev => {
      const newSet = new Set(prev)
      if (newSet.has(seatId)) {
        newSet.delete(seatId)
      } else {
        newSet.add(seatId)
      }
      return newSet
    })
  }

  const updateSeatPrices = async (newPrice: number) => {
    try {
      const updatedSeats = seats.map(seat => 
        selectedSeats.has(seat.id) ? { ...seat, price: newPrice } : seat
      )
      setSeats(updatedSeats)
      setSelectedSeats(new Set())
      
      console.log(`Updated ${selectedSeats.size} seats to price: ${newPrice}`)
    } catch (error) {
      console.error("Error updating seat prices:", error)
    }
  }

  const getSeatStatusColor = (status: Seat["status"]) => {
    switch (status) {
      case "available": return "bg-green-500 hover:bg-green-600"
      case "reserved": return "bg-yellow-500"
      case "sold": return "bg-red-500"
      default: return "bg-gray-500"
    }
  }

  const groupSeatsBySection = () => {
    const grouped = seats.reduce((acc, seat) => {
      if (!acc[seat.section]) {
        acc[seat.section] = {}
      }
      if (!acc[seat.section][seat.row]) {
        acc[seat.section][seat.row] = []
      }
      acc[seat.section][seat.row].push(seat)
      return acc
    }, {} as Record<string, Record<string, Seat[]>>)

    // Sort seats by number within each row
    Object.keys(grouped).forEach(section => {
      Object.keys(grouped[section]).forEach(row => {
        grouped[section][row].sort((a, b) => parseInt(a.number) - parseInt(b.number))
      })
    })

    return grouped
  }

  if (!event) {
    return <div className="text-center py-12">Cargando evento...</div>
  }

  const groupedSeats = groupSeatsBySection()

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Gestión de Asientos</h1>
          <p className="mt-2 text-gray-600">{event.name} - {event.venue}</p>
        </div>
        <div className="flex space-x-3">
          <Link href={`/admin/events/${params.eventId}`}>
            <AdminButton variant="ghost">
              ← Volver al Evento
            </AdminButton>
          </Link>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white p-6 rounded-lg shadow">
          <div className="flex items-center">
            <div className="p-2 bg-blue-100 rounded-lg">
              <span className="text-2xl">🎟️</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600">Total</p>
              <p className="text-2xl font-bold text-gray-900">{stats.total.toLocaleString()}</p>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-lg shadow">
          <div className="flex items-center">
            <div className="p-2 bg-green-100 rounded-lg">
              <span className="text-2xl">✅</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600">Disponibles</p>
              <p className="text-2xl font-bold text-green-600">{stats.available.toLocaleString()}</p>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-lg shadow">
          <div className="flex items-center">
            <div className="p-2 bg-yellow-100 rounded-lg">
              <span className="text-2xl">⏳</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600">Reservados</p>
              <p className="text-2xl font-bold text-yellow-600">{stats.reserved.toLocaleString()}</p>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-lg shadow">
          <div className="flex items-center">
            <div className="p-2 bg-red-100 rounded-lg">
              <span className="text-2xl">🎫</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600">Vendidos</p>
              <p className="text-2xl font-bold text-red-600">{stats.sold.toLocaleString()}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Seat Management */}
      {seats.length === 0 ? (
        /* Seat Generation Form */
        <div className="bg-white shadow rounded-lg p-6">
          <h3 className="text-lg font-medium text-gray-900 mb-6">Generar Asientos</h3>
          
          <AdminForm onSubmit={(e) => { e.preventDefault(); generateSeats(); }}>
            <AdminFormSection 
              title="Configuración de Secciones" 
              description="Define las secciones y precios para el venue"
            >
              <div className="space-y-6">
                {generateForm.sections.map((section, index) => (
                  <div key={index} className="grid grid-cols-1 md:grid-cols-4 gap-4 p-4 border rounded-lg">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Sección
                      </label>
                      <input
                        type="text"
                        value={section.name}
                        onChange={(e) => {
                          const newSections = [...generateForm.sections]
                          newSections[index].name = e.target.value
                          setGenerateForm({ ...generateForm, sections: newSections })
                        }}
                        className="block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Filas
                      </label>
                      <input
                        type="number"
                        value={section.rows}
                        onChange={(e) => {
                          const newSections = [...generateForm.sections]
                          newSections[index].rows = parseInt(e.target.value) || 0
                          setGenerateForm({ ...generateForm, sections: newSections })
                        }}
                        className="block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Asientos por Fila
                      </label>
                      <input
                        type="number"
                        value={section.seatsPerRow}
                        onChange={(e) => {
                          const newSections = [...generateForm.sections]
                          newSections[index].seatsPerRow = parseInt(e.target.value) || 0
                          setGenerateForm({ ...generateForm, sections: newSections })
                        }}
                        className="block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Precio Base (PEN)
                      </label>
                      <input
                        type="number"
                        step="0.01"
                        value={section.basePrice}
                        onChange={(e) => {
                          const newSections = [...generateForm.sections]
                          newSections[index].basePrice = parseFloat(e.target.value) || 0
                          setGenerateForm({ ...generateForm, sections: newSections })
                        }}
                        className="block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
                      />
                    </div>
                  </div>
                ))}
              </div>

              <div className="flex justify-between items-center pt-4">
                <p className="text-sm text-gray-600">
                  Total de asientos a generar: {generateForm.sections.reduce((sum, s) => sum + s.rows * s.seatsPerRow, 0).toLocaleString()}
                </p>
                <AdminButton type="submit" disabled={isGenerating}>
                  {isGenerating ? "Generando..." : "Generar Asientos"}
                </AdminButton>
              </div>
            </AdminFormSection>
          </AdminForm>
        </div>
      ) : (
        /* Seat Layout Display */
        <div className="space-y-6">
          {/* Selected Seats Actions */}
          {selectedSeats.size > 0 && (
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <div className="flex items-center justify-between">
                <p className="text-sm text-blue-800">
                  {selectedSeats.size} asientos seleccionados
                </p>
                <div className="flex space-x-3">
                  <input
                    type="number"
                    step="0.01"
                    placeholder="Nuevo precio"
                    className="px-3 py-1 border border-blue-300 rounded text-sm"
                    onKeyPress={(e) => {
                      if (e.key === "Enter") {
                        const newPrice = parseFloat((e.target as HTMLInputElement).value)
                        if (newPrice > 0) {
                          updateSeatPrices(newPrice)
                          ;(e.target as HTMLInputElement).value = ""
                        }
                      }
                    }}
                  />
                  <AdminButton 
                    size="sm" 
                    variant="ghost"
                    onClick={() => setSelectedSeats(new Set())}
                  >
                    Limpiar Selección
                  </AdminButton>
                </div>
              </div>
            </div>
          )}

          {/* Legend */}
          <div className="bg-white shadow rounded-lg p-4">
            <div className="flex items-center space-x-6">
              <div className="flex items-center space-x-2">
                <div className="w-4 h-4 bg-green-500 rounded"></div>
                <span className="text-sm text-gray-600">Disponible</span>
              </div>
              <div className="flex items-center space-x-2">
                <div className="w-4 h-4 bg-yellow-500 rounded"></div>
                <span className="text-sm text-gray-600">Reservado</span>
              </div>
              <div className="flex items-center space-x-2">
                <div className="w-4 h-4 bg-red-500 rounded"></div>
                <span className="text-sm text-gray-600">Vendido</span>
              </div>
              <div className="flex items-center space-x-2">
                <div className="w-4 h-4 bg-blue-500 rounded border-2 border-blue-700"></div>
                <span className="text-sm text-gray-600">Seleccionado</span>
              </div>
            </div>
          </div>

          {/* Seat Layout per Section */}
          {Object.entries(groupedSeats).map(([sectionName, rows]) => (
            <div key={sectionName} className="bg-white shadow rounded-lg p-6">
              <h3 className="text-lg font-medium text-gray-900 mb-4">
                {sectionName}
              </h3>
              
              <div className="space-y-2">
                {Object.entries(rows)
                  .sort(([a], [b]) => parseInt(a) - parseInt(b))
                  .map(([rowName, rowSeats]) => (
                  <div key={rowName} className="flex items-center space-x-2">
                    <div className="w-8 text-sm text-gray-500 text-right">
                      {rowName}
                    </div>
                    <div className="flex space-x-1">
                      {rowSeats.map(seat => (
                        <button
                          key={seat.id}
                          onClick={() => handleSeatClick(seat.id)}
                          className={`w-6 h-6 text-xs rounded text-white font-medium transition-all
                            ${getSeatStatusColor(seat.status)}
                            ${selectedSeats.has(seat.id) ? "ring-2 ring-blue-700 ring-offset-1" : ""}
                            ${seat.status === "available" ? "cursor-pointer" : "cursor-not-allowed"}
                          `}
                          disabled={seat.status !== "available"}
                          title={`${seat.section}-${seat.row}-${seat.number} - PEN ${seat.price}`}
                        >
                          {seat.number}
                        </button>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}