"use client"

import { useState, useEffect, use } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { z } from "zod"
import { AdminForm, AdminFormSection } from "@/components/admin/AdminForm"
import { AdminButton } from "@/components/admin/AdminButton"
import { getEvent, catalogAdminApi, type Event, type UpdateEventRequest } from "@/lib/api/catalog"
import { useToast } from "@/hooks/use-toast"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Checkbox } from "@/components/ui/checkbox"

// Zod validation schema
const editEventSchema = z.object({
  name: z.string().min(1, "El nombre es obligatorio").max(100, "El nombre no puede exceder 100 caracteres"),
  description: z.string().min(1, "La descripción es obligatoria").max(500, "La descripción no puede exceder 500 caracteres"),
  eventDate: z.string().min(1, "La fecha es obligatoria"),
  venue: z.string().min(1, "El venue es obligatorio").max(100, "El venue no puede exceder 100 caracteres"),
  maxCapacity: z.number().min(1, "La capacidad debe ser mayor a 0").max(1000000, "La capacidad es demasiado grande"),
  basePrice: z.number().min(0, "El precio no puede ser negativo").max(10000, "El precio es demasiado alto"),
  isActive: z.boolean()
})

type EditEventForm = z.infer<typeof editEventSchema>

export default function EditEventPage({ 
  params 
}: { 
  params: Promise<{ eventId: string }> 
}) {
  const { eventId } = use(params)
  const router = useRouter()
  const { toast } = useToast()
  const [isLoading, setIsLoading] = useState(false)
  const [isLoadingEvent, setIsLoadingEvent] = useState(true)
  const [errors, setErrors] = useState<Record<string, string>>({})
  
  const [formData, setFormData] = useState<Event>({
    id: "",
    name: "",
    description: "",
    eventDate: "",
    venue: "",
    maxCapacity: 0,
    basePrice: 0,
    isActive: true
  })

  // String versions for form inputs to prevent controlled/uncontrolled issues
  const [formInputs, setFormInputs] = useState({
    maxCapacity: "0",
    basePrice: "0.00"
  })

  useEffect(() => {
    fetchEvent()
  }, [eventId])

  const fetchEvent = async () => {
    setIsLoadingEvent(true)
    try {
      const event = await getEvent(eventId)
      
      if (!event) {
        toast({
          title: "Error",
          description: "Evento no encontrado.",
          variant: "destructive",
        })
        router.push("/admin/events")
        return
      }

      setFormData({
        ...event,
        // Convert ISO date to datetime-local format (YYYY-MM-DDTHH:mm)
        eventDate: event.eventDate ? event.eventDate.slice(0, 16) : "",
      })
      setFormInputs({
        maxCapacity: event.maxCapacity?.toString() || "0",
        basePrice: event.basePrice?.toFixed(2) || "0.00"
      })
    } catch (error) {
      console.error("Error fetching event:", error)
      toast({
        title: "Error",
        description: "Error al cargar el evento.",
        variant: "destructive",
      })
      setErrors({ general: "Error al cargar el evento" })
    } finally {
      setIsLoadingEvent(false)
    }
  }

  const handleInputChange = (field: keyof Event, value: any) => {
    if (field === "maxCapacity") {
      setFormInputs(prev => ({ ...prev, maxCapacity: value }))
      const numValue = parseInt(value) || 0
      setFormData(prev => ({ ...prev, [field]: numValue }))
    } else if (field === "basePrice") {
      setFormInputs(prev => ({ ...prev, basePrice: value }))
      const numValue = parseFloat(value) || 0
      setFormData(prev => ({ ...prev, [field]: numValue }))
    } else {
      setFormData(prev => ({ ...prev, [field]: value }))
    }
    
    // Clear error when user starts typing
    if (errors[field]) {
      setErrors(prev => {
        const newErrors = { ...prev }
        delete newErrors[field]
        return newErrors
      })
    }
  }

  const validateForm = (): boolean => {
    try {
      const formDataToValidate = {
        ...formData,
        maxCapacity: Number(formData.maxCapacity),
        basePrice: Number(formData.basePrice)
      }
      
      editEventSchema.parse(formDataToValidate)
      setErrors({})
      return true
    } catch (error) {
      if (error instanceof z.ZodError) {
        const newErrors: Record<string, string> = {}
        
        error.errors.forEach((err) => {
          const field = err.path[0] as string
          newErrors[field] = err.message
          
          // Show toast for each validation error
          toast({
            title: "Error de validación",
            description: `${field === 'name' ? 'Nombre' :
                         field === 'description' ? 'Descripción' :
                         field === 'eventDate' ? 'Fecha' :
                         field === 'venue' ? 'Venue' :
                         field === 'maxCapacity' ? 'Capacidad' :
                         field === 'basePrice' ? 'Precio' : field}: ${err.message}`,
            variant: "destructive",
          })
        })
        
        setErrors(newErrors)
        return false
      }
      return false
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!validateForm()) {
      return
    }

    setIsLoading(true)
    
    try {
      const updateRequest: UpdateEventRequest = {
        name: formData.name,
        description: formData.description,
        maxCapacity: formData.maxCapacity
      }

      await catalogAdminApi.updateEvent(eventId, updateRequest)
      
      toast({
        title: "Éxito",
        description: "Evento actualizado correctamente.",
      })
      
      // Redirect to event detail on success
      router.push(`/admin/events/${eventId}`)
      
    } catch (error) {
      console.error("Error updating event:", error)
      toast({
        title: "Error",
        description: "Error al actualizar el evento. Por favor, intenta de nuevo.",
        variant: "destructive",
      })
      setErrors({ general: "Error al actualizar el evento. Por favor, intenta de nuevo." })
    } finally {
      setIsLoading(false)
    }
  }

  const handleDelete = async () => {
    toast({
      title: "Funcionalidad en desarrollo",
      description: "La funcionalidad de eliminar eventos está en desarrollo y estará disponible pronto.",
      variant: "default",
    })
  }

  if (isLoadingEvent) {
    return (
      <div className="max-w-4xl mx-auto space-y-6">
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

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-300">Editar Evento</h1>
          <p className="mt-2 text-gray-600">
            Modifica la información del evento
          </p>
        </div>
        <div className="flex space-x-3">
          <Link href={`/admin/events/${eventId}`}>
            <AdminButton variant="ghost">
              ← Volver al Evento
            </AdminButton>
          </Link>
          <AdminButton 
            variant="destructive"
            onClick={handleDelete}
            disabled={isLoading}
          >
            🗑️ Eliminar
          </AdminButton>
        </div>
      </div>

      {/* Form */}
      <AdminForm onSubmit={handleSubmit}>
        {/* General Information */}
        <AdminFormSection 
          title="Información General" 
          description="Datos básicos del evento"
        >
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-2">
              <Label htmlFor="name" className="text-sm font-medium text-slate-700">
                Nombre del Evento *
              </Label>
              <Input
                id="name"
                type="text"
                value={formData.name}
                onChange={(e) => handleInputChange("name", e.target.value)}
                className={`bg-white border-2 transition-all duration-200 ${
                  errors.name 
                    ? "border-red-500 focus:border-red-600 focus:ring-red-200" 
                    : "border-slate-300 hover:border-slate-400 focus:border-blue-500 focus:ring-blue-200"
                }`}
                placeholder="Ej: Concierto de Rock 2026"
              />
              {errors.name && (
                <p className="text-sm text-red-600 font-medium">{errors.name}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="venue" className="text-sm font-medium text-slate-700">
                Venue *
              </Label>
              <Input
                id="venue"
                type="text"
                value={formData.venue}
                onChange={(e) => handleInputChange("venue", e.target.value)}
                className={`bg-white border-2 transition-all duration-200 ${
                  errors.venue 
                    ? "border-red-500 focus:border-red-600 focus:ring-red-200" 
                    : "border-slate-300 hover:border-slate-400 focus:border-blue-500 focus:ring-blue-200"
                }`}
                placeholder="Ej: Estadio Nacional"
              />
              {errors.venue && (
                <p className="text-sm text-red-600 font-medium">{errors.venue}</p>
              )}
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="description" className="text-sm font-medium text-slate-700">
              Descripción *
            </Label>
            <Textarea
              id="description"
              rows={4}
              value={formData.description}
              onChange={(e) => handleInputChange("description", e.target.value)}
              className={`transition-all duration-200 resize-none ${
                errors.description 
                  ? "border-red-500 focus:border-red-600 focus:ring-red-200" 
                  : "border-slate-300 hover:border-slate-400 focus:border-blue-500 focus:ring-blue-200"
              }`}
              placeholder="Describe el evento..."
            />
            {errors.description && (
              <p className="text-sm text-red-600 font-medium">{errors.description}</p>
            )}
          </div>
        </AdminFormSection>

        {/* Date and Capacity */}
        <AdminFormSection 
          title="Fecha y Capacidad" 
          description="Información sobre cuándo y cuántas personas"
        >
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="space-y-2">
              <Label htmlFor="eventDate" className="text-sm font-medium text-slate-700">
                Fecha y Hora *
              </Label>
              <Input
                id="eventDate"
                type="datetime-local"
                value={formData.eventDate}
                onChange={(e) => handleInputChange("eventDate", e.target.value)}
                className={`bg-white border-2 transition-all duration-200 ${
                  errors.eventDate 
                    ? "border-red-500 focus:border-red-600 focus:ring-red-200" 
                    : "border-slate-300 hover:border-slate-400 focus:border-blue-500 focus:ring-blue-200"
                }`}
              />
              {errors.eventDate && (
                <p className="text-sm text-red-600 font-medium">{errors.eventDate}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="maxCapacity" className="text-sm font-medium text-slate-700">
                Capacidad Máxima *
              </Label>
              <Input
                id="maxCapacity"
                type="number"
                min="1"
                value={formInputs.maxCapacity}
                onChange={(e) => handleInputChange("maxCapacity", e.target.value)}
                className={`bg-white border-2 transition-all duration-200 ${
                  errors.maxCapacity 
                    ? "border-red-500 focus:border-red-600 focus:ring-red-200" 
                    : "border-slate-300 hover:border-slate-400 focus:border-blue-500 focus:ring-blue-200"
                }`}
                placeholder="Ej: 50000"
              />
              {errors.maxCapacity && (
                <p className="text-sm text-red-600 font-medium">{errors.maxCapacity}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="basePrice" className="text-sm font-medium text-slate-700">
                Precio Base (PEN) *
              </Label>
              <Input
                id="basePrice"
                type="number"
                min="0"
                step="0.01"
                value={formInputs.basePrice}
                onChange={(e) => handleInputChange("basePrice", e.target.value)}
                className={`bg-white border-2 transition-all duration-200 ${
                  errors.basePrice 
                    ? "border-red-500 focus:border-red-600 focus:ring-red-200" 
                    : "border-slate-300 hover:border-slate-400 focus:border-blue-500 focus:ring-blue-200"
                }`}
                placeholder="Ej: 150.00"
              />
              {errors.basePrice && (
                <p className="text-sm text-red-600 font-medium">{errors.basePrice}</p>
              )}
            </div>
          </div>
        </AdminFormSection>

        {/* Event Status */}
        <AdminFormSection 
          title="Estado del Evento" 
          description="Configuración de activación"
        >
          <div className="flex items-center space-x-3 p-4 bg-slate-50 border-2 border-slate-200 rounded-lg hover:bg-slate-100 transition-colors">
            <Checkbox
              id="isActive"
              checked={formData.isActive}
              onCheckedChange={(checked) => handleInputChange("isActive", checked === true)}
              className="w-5 h-5 border-2 border-slate-400 data-[state=checked]:border-blue-500 data-[state=checked]:bg-blue-500"
            />
            <Label htmlFor="isActive" className="text-sm font-medium text-slate-700 cursor-pointer">
              Evento activo
            </Label>
          </div>
        </AdminFormSection>

        {/* Error Message */}
        {errors.general && (
          <div className="rounded-md bg-red-50 p-4">
            <p className="text-sm text-red-800">{errors.general}</p>
          </div>
        )}

        {/* Actions */}
        <div className="flex justify-end space-x-4 pt-6 border-t">
          <Link href={`/admin/events/${eventId}`}>
            <AdminButton variant="ghost" disabled={isLoading}>
              Cancelar
            </AdminButton>
          </Link>
          <AdminButton type="submit" disabled={isLoading}>
            {isLoading ? "Actualizando..." : "Guardar Cambios"}
          </AdminButton>
        </div>
      </AdminForm>
    </div>
  )
}