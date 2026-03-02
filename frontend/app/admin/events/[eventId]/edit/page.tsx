"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { AdminForm, AdminFormSection } from "@/components/admin/AdminForm"
import { AdminButton } from "@/components/admin/AdminButton"

interface Event {
  id: string
  name: string
  description: string
  eventDate: string
  venue: string
  maxCapacity: number
  basePrice: number
  categoryId?: string
  imageUrl?: string
  tags: string[]
  isActive: boolean
}

export default function EditEventPage({ 
  params 
}: { 
  params: { eventId: string } 
}) {
  const router = useRouter()
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
    categoryId: "",
    imageUrl: "",
    tags: [],
    isActive: true
  })

  useEffect(() => {
    fetchEvent()
  }, [params.eventId])

  const fetchEvent = async () => {
    setIsLoadingEvent(true)
    try {
      // In a real implementation, this would call your backend API
      await new Promise(resolve => setTimeout(resolve, 1000))
      
      // Mock event data
      const mockEvent: Event = {
        id: params.eventId,
        name: "Concierto Rock 2026",
        description: "Gran concierto de rock en el estadio nacional",
        eventDate: "2026-06-15T20:00",
        venue: "Estadio Nacional",
        maxCapacity: 50000,
        basePrice: 75.00,
        categoryId: "1",
        imageUrl: "https://example.com/concert-image.jpg",
        tags: ["rock", "música", "concierto", "nacional"],
        isActive: true
      }

      setFormData(mockEvent)
    } catch (error) {
      console.error("Error fetching event:", error)
      setErrors({ general: "Error al cargar el evento" })
    } finally {
      setIsLoadingEvent(false)
    }
  }

  const handleInputChange = (field: keyof Event, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }))
    
    // Clear error when user starts typing
    if (errors[field]) {
      setErrors(prev => {
        const newErrors = { ...prev }
        delete newErrors[field]
        return newErrors
      })
    }
  }

  const handleTagsChange = (tagsString: string) => {
    const tags = tagsString.split(",").map(tag => tag.trim()).filter(tag => tag.length > 0)
    handleInputChange("tags", tags)
  }

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.name.trim()) {
      newErrors.name = "El nombre es requerido"
    }

    if (!formData.description.trim()) {
      newErrors.description = "La descripción es requerida"
    }

    if (!formData.eventDate) {
      newErrors.eventDate = "La fecha del evento es requerida"
    }

    if (!formData.venue.trim()) {
      newErrors.venue = "El venue es requerido"
    }

    if (formData.maxCapacity <= 0) {
      newErrors.maxCapacity = "La capacidad máxima debe ser mayor a 0"
    }

    if (formData.basePrice < 0) {
      newErrors.basePrice = "El precio base no puede ser negativo"
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!validateForm()) {
      return
    }

    setIsLoading(true)
    
    try {
      // In a real implementation, this would call your backend API
      console.log("Updating event:", formData)
      
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 2000))
      
      // Redirect to event detail on success
      router.push(`/admin/events/${params.eventId}`)
      
    } catch (error) {
      console.error("Error updating event:", error)
      setErrors({ general: "Error al actualizar el evento. Por favor, intenta de nuevo." })
    } finally {
      setIsLoading(false)
    }
  }

  const handleDelete = async () => {
    if (!confirm("¿Estás seguro de que deseas eliminar este evento? Esta acción no se puede deshacer.")) {
      return
    }

    setIsLoading(true)
    
    try {
      // In a real implementation, this would call your backend API
      console.log("Deleting event:", params.eventId)
      
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1500))
      
      // Redirect to events list on success
      router.push("/admin/events")
      
    } catch (error) {
      console.error("Error deleting event:", error)
      setErrors({ general: "Error al eliminar el evento. Por favor, intenta de nuevo." })
    } finally {
      setIsLoading(false)
    }
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
          <h1 className="text-3xl font-bold text-gray-900">Editar Evento</h1>
          <p className="mt-2 text-gray-600">
            Modifica la información del evento
          </p>
        </div>
        <div className="flex space-x-3">
          <Link href={`/admin/events/${params.eventId}`}>
            <AdminButton variant="ghost">
              ← Volver al Evento
            </AdminButton>
          </Link>
          <AdminButton 
            variant="danger"
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
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Nombre del Evento *
              </label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => handleInputChange("name", e.target.value)}
                className={`block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm ${
                  errors.name ? "border-red-500 focus:border-red-500 focus:ring-red-500" : ""
                }`}
                placeholder="Ej: Concierto de Rock 2026"
              />
              {errors.name && (
                <p className="mt-1 text-sm text-red-600">{errors.name}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Venue *
              </label>
              <input
                type="text"
                value={formData.venue}
                onChange={(e) => handleInputChange("venue", e.target.value)}
                className={`block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm ${
                  errors.venue ? "border-red-500 focus:border-red-500 focus:ring-red-500" : ""
                }`}
                placeholder="Ej: Estadio Nacional"
              />
              {errors.venue && (
                <p className="mt-1 text-sm text-red-600">{errors.venue}</p>
              )}
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Descripción *
            </label>
            <textarea
              rows={4}
              value={formData.description}
              onChange={(e) => handleInputChange("description", e.target.value)}
              className={`block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm ${
                errors.description ? "border-red-500 focus:border-red-500 focus:ring-red-500" : ""
              }`}
              placeholder="Describe el evento..."
            />
            {errors.description && (
              <p className="mt-1 text-sm text-red-600">{errors.description}</p>
            )}
          </div>
        </AdminFormSection>

        {/* Date and Capacity */}
        <AdminFormSection 
          title="Fecha y Capacidad" 
          description="Información sobre cuándo y cuántas personas"
        >
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Fecha y Hora *
              </label>
              <input
                type="datetime-local"
                value={formData.eventDate}
                onChange={(e) => handleInputChange("eventDate", e.target.value)}
                className={`block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm ${
                  errors.eventDate ? "border-red-500 focus:border-red-500 focus:ring-red-500" : ""
                }`}
              />
              {errors.eventDate && (
                <p className="mt-1 text-sm text-red-600">{errors.eventDate}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Capacidad Máxima *
              </label>
              <input
                type="number"
                min="1"
                value={formData.maxCapacity || ""}
                onChange={(e) => handleInputChange("maxCapacity", parseInt(e.target.value) || 0)}
                className={`block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm ${
                  errors.maxCapacity ? "border-red-500 focus:border-red-500 focus:ring-red-500" : ""
                }`}
                placeholder="Ej: 50000"
              />
              {errors.maxCapacity && (
                <p className="mt-1 text-sm text-red-600">{errors.maxCapacity}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Precio Base (PEN) *
              </label>
              <input
                type="number"
                min="0"
                step="0.01"
                value={formData.basePrice || ""}
                onChange={(e) => handleInputChange("basePrice", parseFloat(e.target.value) || 0)}
                className={`block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm ${
                  errors.basePrice ? "border-red-500 focus:border-red-500 focus:ring-red-500" : ""
                }`}
                placeholder="Ej: 75.00"
              />
              {errors.basePrice && (
                <p className="mt-1 text-sm text-red-600">{errors.basePrice}</p>
              )}
            </div>
          </div>
        </AdminFormSection>

        {/* Additional Information */}
        <AdminFormSection 
          title="Información Adicional" 
          description="Categorías, imágenes y configuración"
        >
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Imagen URL
              </label>
              <input
                type="url"
                value={formData.imageUrl}
                onChange={(e) => handleInputChange("imageUrl", e.target.value)}
                className="block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
                placeholder="https://example.com/imagen.jpg"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Tags (separadas por comas)
              </label>
              <input
                type="text"
                value={formData.tags.join(", ")}
                onChange={(e) => handleTagsChange(e.target.value)}
                className="block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
                placeholder="rock, música, concierto, nacional"
              />
            </div>
          </div>

          <div className="flex items-center">
            <input
              id="isActive"
              type="checkbox"
              checked={formData.isActive}
              onChange={(e) => handleInputChange("isActive", e.target.checked)}
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
            <label htmlFor="isActive" className="ml-2 block text-sm text-gray-900">
              Evento activo
            </label>
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
          <Link href={`/admin/events/${params.eventId}`}>
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