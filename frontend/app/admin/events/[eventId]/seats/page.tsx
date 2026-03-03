"use client"

import { useState, useEffect, use } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { AdminButton } from "@/components/admin/AdminButton"
import { getEvent, catalogAdminApi, type Event, type SeatSectionConfiguration } from "@/lib/api/catalog"
import { useToast } from "@/hooks/use-toast"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Info, Settings, Plus, Trash2 } from "lucide-react"

interface SeatSection {
  section: string
  rows: number
  seatsPerRow: number
  priceMultiplier: number
}

export default function EventSeatsPage({ 
  params 
}: { 
  params: Promise<{ eventId: string }> 
}) {
  const { eventId } = use(params)
  const router = useRouter()
  const { toast } = useToast()
  const [event, setEvent] = useState<Event | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isGenerating, setIsGenerating] = useState(false)
  const [sections, setSections] = useState<SeatSection[]>([
    { section: "General", rows: 10, seatsPerRow: 20, priceMultiplier: 1.0 }
  ])

  useEffect(() => {
    fetchEvent()
  }, [eventId])

  const fetchEvent = async () => {
    setIsLoading(true)
    try {
      const fetchedEvent = await getEvent(eventId)
      
      if (!fetchedEvent) {
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
      toast({
        title: "Error",
        description: "Error al cargar el evento.",
        variant: "destructive",
      })
    } finally {
      setIsLoading(false)
    }
  }

  const addSection = () => {
    setSections([...sections, { 
      section: `Sección ${sections.length + 1}`, 
      rows: 5, 
      seatsPerRow: 10, 
      priceMultiplier: 1.0 
    }])
  }

  const removeSection = (index: number) => {
    if (sections.length > 1) {
      setSections(sections.filter((_, i) => i !== index))
    }
  }

  const updateSection = (index: number, field: keyof SeatSection, value: string | number) => {
    const newSections = [...sections]
    newSections[index] = { ...newSections[index], [field]: value }
    setSections(newSections)
  }

  const getTotalSeats = () => {
    return sections.reduce((total, section) => total + (section.rows * section.seatsPerRow), 0)
  }

  const handleGenerateSeats = async () => {
    if (!event) return

    // Validate sections
    const hasErrors = sections.some(section => 
      section.rows <= 0 || 
      section.seatsPerRow <= 0 || 
      section.priceMultiplier <= 0 ||
      !section.section.trim()
    )

    if (hasErrors) {
      toast({
        title: "Error de validación",
        description: "Todos los campos deben tener valores válidos mayores a 0.",
        variant: "destructive",
      })
      return
    }

    const totalSeats = getTotalSeats()
    if (totalSeats > event.maxCapacity) {
      toast({
        title: "Capacidad excedida",
        description: `El total de asientos (${totalSeats}) excede la capacidad máxima del evento (${event.maxCapacity}).`,
        variant: "destructive",
      })
      return
    }

    setIsGenerating(true)
    try {
      const sectionConfigurations: SeatSectionConfiguration[] = sections.map(section => ({
        sectionCode: section.section,
        rows: section.rows,
        seatsPerRow: section.seatsPerRow,
        priceMultiplier: section.priceMultiplier
      }))

      await catalogAdminApi.generateSeats(eventId, {
        sectionConfigurations
      })

      toast({
        title: "Éxito",
        description: `Se generaron ${totalSeats} asientos correctamente.`,
      })

      // Redirect to event detail
      router.push(`/admin/events/${eventId}`)
      
    } catch (error) {
      console.error("Error generating seats:", error)
      toast({
        title: "Error",
        description: "Error al generar los asientos. Por favor, intenta nuevamente.",
        variant: "destructive",
      })
    } finally {
      setIsGenerating(false)
    }
  }

  const handlePreviewSeats = () => {
    toast({
      title: "Funcionalidad en desarrollo",
      description: "La vista previa de asientos está en desarrollo y estará disponible pronto.",
    })
  }

  if (isLoading) {
    return (
      <div className="max-w-4xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <Skeleton className="h-8 w-64 mb-2" />
            <Skeleton className="h-4 w-96" />
          </div>
          <Skeleton className="h-10 w-32" />
        </div>
        
        <Card>
          <CardContent className="p-6">
            <Skeleton className="h-4 w-full mb-4" />
            <Skeleton className="h-4 w-3/4 mb-4" />
            <Skeleton className="h-4 w-1/2" />
          </CardContent>
        </Card>
      </div>
    )
  }

  if (!event) {
    return (
      <div className="max-w-4xl mx-auto space-y-6">
        <div className="text-center">
          <h1 className="text-2xl font-bold mb-4">Error</h1>
          <p className="text-muted-foreground">Evento no encontrado</p>
          <Link href="/admin/events" className="mt-4 inline-block">
            <AdminButton>Volver a Eventos</AdminButton>
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Gestión de Asientos</h1>
          <p className="mt-2 text-muted-foreground">
            Configura y genera los asientos para <span className="font-medium">{event.name}</span>
          </p>
        </div>
        <AdminButton variant="outline" asChild>
          <Link href={`/admin/events/${event.id}`}>
            ← Volver al Evento
          </Link>
        </AdminButton>
      </div>

      {/* Event Summary */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center">
            <Settings className="h-5 w-5 mr-2" />
            Resumen del Evento
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <p className="font-medium">Capacidad Máxima</p>
              <p className="text-2xl font-bold text-primary">{event.maxCapacity?.toLocaleString() || 'N/A'}</p>
            </div>
            <div>
              <p className="font-medium">Precio Base</p>
              <p className="text-2xl font-bold text-green-600">
                {new Intl.NumberFormat("es-PE", { style: "currency", currency: "PEN" }).format(event.basePrice)}
              </p>
            </div>
            <div>
              <p className="font-medium">Asientos Configurados</p>
              <p className="text-2xl font-bold text-blue-600">{getTotalSeats().toLocaleString()}</p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Configuration Alert */}
      <Alert>
        <Info className="h-4 w-4" />
        <AlertDescription>
          Configura las secciones de asientos para tu evento. El precio final se calculará multiplicando el precio base por el multiplicador de cada sección.
        </AlertDescription>
      </Alert>

      {/* Sections Configuration */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            Configuración de Secciones
            <Button onClick={addSection} size="sm">
              <Plus className="h-4 w-4 mr-2" />
              Agregar Sección
            </Button>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          {sections.map((section, index) => (
            <div key={index} className="p-4 border rounded-lg space-y-4">
              <div className="flex items-center justify-between">
                <h3 className="font-medium">Sección {index + 1}</h3>
                {sections.length > 1 && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => removeSection(index)}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                )}
              </div>
              
              <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                <div>
                  <Label htmlFor={`section-${index}`}>Nombre de la Sección</Label>
                  <Input
                    id={`section-${index}`}
                    value={section.section}
                    onChange={(e) => updateSection(index, 'section', e.target.value)}
                    placeholder="Ej: VIP, General"
                  />
                </div>
                
                <div>
                  <Label htmlFor={`rows-${index}`}>Filas</Label>
                  <Input
                    id={`rows-${index}`}
                    type="number"
                    min="1"
                    value={section.rows}
                    onChange={(e) => updateSection(index, 'rows', parseInt(e.target.value) || 0)}
                  />
                </div>
                
                <div>
                  <Label htmlFor={`seats-${index}`}>Asientos por Fila</Label>
                  <Input
                    id={`seats-${index}`}
                    type="number"
                    min="1"
                    value={section.seatsPerRow}
                    onChange={(e) => updateSection(index, 'seatsPerRow', parseInt(e.target.value) || 0)}
                  />
                </div>
                
                <div>
                  <Label htmlFor={`multiplier-${index}`}>Multiplicador de Precio</Label>
                  <Input
                    id={`multiplier-${index}`}
                    type="number"
                    step="0.1"
                    min="0.1"
                    value={section.priceMultiplier}
                    onChange={(e) => updateSection(index, 'priceMultiplier', parseFloat(e.target.value) || 1.0)}
                  />
                </div>
              </div>
              
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm text-muted-foreground">
                <div>
                  <span className="font-medium">Total Asientos:</span> {section.rows * section.seatsPerRow}
                </div>
                <div>
                  <span className="font-medium">Precio por Asiento:</span> {new Intl.NumberFormat("es-PE", { style: "currency", currency: "PEN" }).format(event.basePrice * section.priceMultiplier)}
                </div>
                <div>
                  <span className="font-medium">Ingresos Potenciales:</span> {new Intl.NumberFormat("es-PE", { style: "currency", currency: "PEN" }).format(event.basePrice * section.priceMultiplier * section.rows * section.seatsPerRow)}
                </div>
              </div>
            </div>
          ))}
        </CardContent>
      </Card>

      {/* Summary and Actions */}
      <Card>
        <CardHeader>
          <CardTitle>Resumen y Acciones</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 p-4 bg-muted rounded-lg">
            <div className="text-center">
              <p className="text-2xl font-bold text-primary">{getTotalSeats().toLocaleString()}</p>
              <p className="text-sm text-muted-foreground">Total de Asientos</p>
            </div>
            <div className="text-center">
              <p className="text-2xl font-bold text-green-600">
                {new Intl.NumberFormat("es-PE", { style: "currency", currency: "PEN" }).format(
                  sections.reduce((total, section) => 
                    total + (event.basePrice * section.priceMultiplier * section.rows * section.seatsPerRow), 0
                  )
                )}
              </p>
              <p className="text-sm text-muted-foreground">Ingresos Potenciales</p>
            </div>
            <div className="text-center">
              <Badge variant={getTotalSeats() <= event.maxCapacity ? "default" : "destructive"}>
                {getTotalSeats() <= event.maxCapacity ? "En Capacidad" : "Excede Capacidad"}
              </Badge>
              <p className="text-sm text-muted-foreground mt-1">
                {event.maxCapacity - getTotalSeats()} asientos restantes
              </p>
            </div>
          </div>
          
          <div className="flex flex-wrap gap-3">
            <AdminButton 
              onClick={handleGenerateSeats}
              isLoading={isGenerating}
              disabled={getTotalSeats() > event.maxCapacity || getTotalSeats() === 0}
            >
              Generar Asientos
            </AdminButton>
            
            <AdminButton 
              variant="outline"
              onClick={handlePreviewSeats}
            >
              Vista Previa
            </AdminButton>
            
            <AdminButton variant="outline" asChild>
              <Link href={`/admin/events/${event.id}`}>
                Cancelar
              </Link>
            </AdminButton>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}