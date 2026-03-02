"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { AdminForm, AdminFormSection } from "@/components/admin/AdminForm"
import { AdminButton } from "@/components/admin/AdminButton"
import { catalogAdminApi, type CreateEventRequest } from "@/lib/api/catalog"
import { useToast } from "@/hooks/use-toast"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Label } from "@/components/ui/label"
import { Checkbox } from "@/components/ui/checkbox"

interface CreateEventData {
  name: string
  description: string
  eventDate: string
  venue: string
  maxCapacity: number
  basePrice: number
  tags: string[]
  imageUrl?: string // Added imageUrl property
  isActive: boolean // Added isActive property
}

export default function CreateEventPage() {
  const router = useRouter()
  const { toast } = useToast()
  const [isLoading, setIsLoading] = useState(false)
  const [errors, setErrors] = useState<Record<string, string>>({})
  
  const [formData, setFormData] = useState<CreateEventData>({
    name: "",
    description: "",
    eventDate: "",
    venue: "",
    maxCapacity: 0,
    basePrice: 0,
    tags: [],
    imageUrl: "", // Initialized imageUrl property
    isActive: true // Initialized isActive property
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
      const createRequest: CreateEventRequest = {
        name: formData.name,
        description: formData.description,
        eventDate: formData.eventDate,
        venue: formData.venue,
        maxCapacity: formData.maxCapacity,
        basePrice: formData.basePrice
      }

      await catalogAdminApi.createEvent(createRequest)
      
      toast({
        title: "Éxito",
        description: "Evento creado correctamente.",
      })
      
      // Redirect to events list on success
      router.push("/admin/events")
      
    } catch (error) {
      console.error("Error creating event:", error)
      toast({
        title: "Error",
        description: "Error al crear el evento. Por favor, intenta de nuevo.",
        variant: "destructive",
      })
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
          <h1 className="text-3xl font-bold text-gray-300">Crear Evento</h1>
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
            <div className="space-y-1">
              <Label htmlFor="name">Nombre del Evento *</Label>
              <Input
                id="name"
                type="text"
                value={formData.name}
                onChange={(e) => handleInputChange("name", e.target.value)}
                aria-invalid={!!errors.name}
                placeholder="Ej: Concierto de Rock 2026"
              />
              {errors.name && (
                <p className="mt-1 text-sm text-destructive">{errors.name}</p>
              )}
            </div>

            <div className="space-y-1">
              <Label htmlFor="venue">Venue *</Label>
              <Input
                id="venue"
                type="text"
                value={formData.venue}
                onChange={(e) => handleInputChange("venue", e.target.value)}
                aria-invalid={!!errors.venue}
                placeholder="Ej: Estadio Nacional"
              />
              {errors.venue && (
                <p className="mt-1 text-sm text-destructive">{errors.venue}</p>
              )}
            </div>
          </div>

          <div className="space-y-1">
            <Label htmlFor="description">Descripción *</Label>
            <Textarea
              id="description"
              rows={4}
              value={formData.description}
              onChange={(e) => handleInputChange("description", e.target.value)}
              aria-invalid={!!errors.description}
              placeholder="Describe el evento..."
            />
            {errors.description && (
              <p className="mt-1 text-sm text-destructive">{errors.description}</p>
            )}
          </div>
        </AdminFormSection>

        {/* Date and Capacity */}
        <AdminFormSection 
          title="Fecha y Capacidad" 
          description="Información sobre cuándo y cuántas personas"
        >
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="space-y-1">
              <Label htmlFor="eventDate">Fecha y Hora *</Label>
              <Input
                id="eventDate"
                type="datetime-local"
                value={formData.eventDate}
                onChange={(e) => handleInputChange("eventDate", e.target.value)}
                aria-invalid={!!errors.eventDate}
              />
              {errors.eventDate && (
                <p className="mt-1 text-sm text-destructive">{errors.eventDate}</p>
              )}
            </div>

            <div className="space-y-1">
              <Label htmlFor="maxCapacity">Capacidad Máxima *</Label>
              <Input
                id="maxCapacity"
                type="number"
                min="1"
                value={formData.maxCapacity || ""}
                onChange={(e) => handleInputChange("maxCapacity", parseInt(e.target.value) || 0)}
                aria-invalid={!!errors.maxCapacity}
                placeholder="Ej: 50000"
              />
              {errors.maxCapacity && (
                <p className="mt-1 text-sm text-destructive">{errors.maxCapacity}</p>
              )}
            </div>

            <div className="space-y-1">
              <Label htmlFor="basePrice">Precio Base (PEN) *</Label>
              <Input
                id="basePrice"
                type="number"
                min="0"
                step="0.01"
                value={formData.basePrice || ""}
                onChange={(e) => handleInputChange("basePrice", parseFloat(e.target.value) || 0)}
                aria-invalid={!!errors.basePrice}
                placeholder="Ej: 75.00"
              />
              {errors.basePrice && (
                <p className="mt-1 text-sm text-destructive">{errors.basePrice}</p>
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
            <div className="space-y-1">
              <Label htmlFor="imageUrl">Imagen URL</Label>
              <Input
                id="imageUrl"
                type="url"
                value={formData.imageUrl}
                onChange={(e) => handleInputChange("imageUrl", e.target.value)}
                placeholder="https://example.com/imagen.jpg"
              />
            </div>

            <div className="space-y-1">
              <Label htmlFor="tags">Tags (separadas por comas)</Label>
              <Input
                id="tags"
                type="text"
                value={formData.tags.join(", ")}
                onChange={(e) => handleTagsChange(e.target.value)}
                placeholder="rock, música, concierto, nacional"
              />
            </div>
          </div>

          <div className="flex items-center gap-2">
            <Checkbox
              id="isActive"
              checked={formData.isActive}
              onCheckedChange={(checked) => handleInputChange("isActive", !!checked)}
            />
            <Label htmlFor="isActive" className="cursor-pointer">
              Activar evento inmediatamente
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