import { API_CONFIG } from "./config"
import type { SeatmapResponse, EventSummary } from "@/lib/types"

// Extended Event interface for admin operations
export interface Event {
  id: string
  name: string
  description: string
  eventDate: string
  venue: string
  maxCapacity: number
  basePrice: number
  isActive: boolean
  createdAt?: string
  updatedAt?: string
}

export interface EventWithSeatmap extends Event {
  seats: Seat[]
}

export interface Seat {
  id: string
  eventId: string
  section: string
  row: string
  number: string
  price: number
  status: 'Available' | 'Reserved' | 'Sold'
}

export interface SeatSectionConfiguration {
  sectionCode: string
  rows: number
  seatsPerRow: number
  priceMultiplier: number
}

export interface CreateEventRequest {
  name: string
  description: string
  eventDate: string
  venue: string
  maxCapacity: number
  basePrice: number
}

export interface UpdateEventRequest {
  name: string
  description: string
  maxCapacity: number
}

export interface GenerateSeatsRequest {
  sectionConfigurations: SeatSectionConfiguration[]
}

// Get admin token from localStorage
const getAdminToken = () => {
  if (typeof window === 'undefined') return null
  return localStorage.getItem('adminToken')
}

const getAuthHeaders = () => {
  const token = getAdminToken()
  return token ? { Authorization: `Bearer ${token}` } : {}
}

// Public API (existing functions)
export async function getEvents(): Promise<EventSummary[]> {
  const res = await fetch(`${API_CONFIG.catalog}/events`)
  if (!res.ok) {
    throw new Error(`Failed to fetch events: ${res.status} ${res.statusText}`)
  }
  return res.json()
}

export async function getSeatmap(eventId: string): Promise<SeatmapResponse> {
  const res = await fetch(`${API_CONFIG.catalog}/events/${eventId}/seatmap`)
  if (!res.ok) {
    throw new Error(`Failed to fetch seatmap: ${res.status} ${res.statusText}`)
  }
  return res.json()
}

// New public API functions
export async function getEvent(id: string): Promise<Event | null> {
  const response = await fetch(`${API_CONFIG.catalog}/events/${id}`, {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
    },
  })

  if (response.status === 404) {
    return null
  }

  if (!response.ok) {
    throw new Error(`Failed to fetch event: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

// Admin API functions
export const catalogAdminApi = {
  // Create new event
  async createEvent(data: CreateEventRequest): Promise<Event> {
    const response = await fetch(`${API_CONFIG.catalog}/admin/events`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...getAuthHeaders(),
      },
      body: JSON.stringify(data),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to create event')
    }

    return response.json()
  },

  // Update event
  async updateEvent(id: string, data: UpdateEventRequest): Promise<Event> {
    const response = await fetch(`${API_CONFIG.catalog}/admin/events/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        ...getAuthHeaders(),
      },
      body: JSON.stringify(data),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to update event')
    }

    return response.json()
  },

  // Generate seats for event
  async generateSeats(eventId: string, data: GenerateSeatsRequest): Promise<{ message: string }> {
    const response = await fetch(`${API_CONFIG.catalog}/admin/events/${eventId}/seats`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...getAuthHeaders(),
      },
      body: JSON.stringify(data),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to generate seats')
    }

    return response.json()
  },

  // Deactivate event
  async deactivateEvent(eventId: string): Promise<Event> {
    const response = await fetch(`${API_CONFIG.catalog}/admin/events/${eventId}/deactivate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...getAuthHeaders(),
      },
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to deactivate event')
    }

    return response.json()
  },

  // Reactivate event
  async reactivateEvent(eventId: string): Promise<Event> {
    const response = await fetch(`${API_CONFIG.catalog}/admin/events/${eventId}/reactivate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...getAuthHeaders(),
      },
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to reactivate event')
    }

    return response.json()
  },
}
