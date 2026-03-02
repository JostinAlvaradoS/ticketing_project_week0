"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { AdminForm, AdminFormSection } from "@/components/admin/AdminForm"
import { AdminButton } from "@/components/admin/AdminButton"

interface CreateEventData {
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

export default function CreateEventPage() {
  const router = useRouter()
  const [isLoading, setIsLoading] = useState(false)
  const [errors, setErrors] = useState<Record<string, string>>({})
  
  const [formData, setFormData] = useState<CreateEventData>({
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

  const handleInputChange = (field: keyof CreateEventData, value: any) => {
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
    } else {
      const eventDate = new Date(formData.eventDate)
      const now = new Date()
      if (eventDate <= now) {
        newErrors.eventDate = "La fecha del evento debe ser en el futuro"
      }
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
      console.log("Creating event:", formData)
      
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 2000))
      
      // Redirect to events list on success
      router.push("/admin/events")
      
    } catch (error) {
      console.error("Error creating event:", error)
      setErrors({ general: "Error al crear el evento. Por favor, intenta de nuevo." })
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Crear Evento</h1>
          <p className="mt-2 text-gray-600">
            Agrega un nuevo evento al catálogo
          </p>
        </div>
        <Link href="/admin/events">
          <AdminButton variant="ghost">
            ← Volver a Eventos
          </AdminButton>
        </Link>
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
              Activar evento inmediatamente
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
          <Link href="/admin/events">
            <AdminButton variant="ghost" disabled={isLoading}>
              Cancelar
            </AdminButton>
          </Link>
          <AdminButton type="submit" disabled={isLoading}>
            {isLoading ? "Creando..." : "Crear Evento"}
          </AdminButton>
        </div>
      </AdminForm>
    </div>
  )
}