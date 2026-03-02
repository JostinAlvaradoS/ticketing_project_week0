// API service for admin-related operations
// This service handles communication with the backend microservices

const API_BASE = process.env.NEXT_PUBLIC_API_BASE || "http://localhost:50002"
const IDENTITY_API = process.env.NEXT_PUBLIC_IDENTITY_API || "http://localhost:50000"

// Helper function to get auth token from client-side storage
const getAuthToken = (): string | null => {
  if (typeof window === "undefined") return null
  return localStorage.getItem("adminToken")
}

// Generic API client
class ApiClient {
  private baseURL: string

  constructor(baseURL: string) {
    this.baseURL = baseURL
  }

  private async request<T>(
    endpoint: string, 
    options: RequestInit = {}
  ): Promise<T> {
    const token = getAuthToken()
    
    const headers: HeadersInit = {
      "Content-Type": "application/json",
      ...options.headers,
    }

    if (token) {
      headers.Authorization = `Bearer ${token}`
    }

    const url = `${this.baseURL}${endpoint}`
    
    const response = await fetch(url, {
      ...options,
      headers,
    })

    if (!response.ok) {
      if (response.status === 401) {
        // Token expired or invalid - redirect to login
        if (typeof window !== "undefined") {
          localStorage.removeItem("adminToken")
          window.location.href = "/admin/login"
        }
      }
      
      const errorText = await response.text()
      throw new Error(`HTTP ${response.status}: ${errorText}`)
    }

    const contentType = response.headers.get("content-type")
    if (contentType && contentType.includes("application/json")) {
      return response.json()
    }
    
    return response.text() as T
  }

  async get<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: "GET" })
  }

  async post<T>(endpoint: string, data: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: "POST",
      body: JSON.stringify(data),
    })
  }

  async put<T>(endpoint: string, data: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: "PUT", 
      body: JSON.stringify(data),
    })
  }

  async delete<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: "DELETE" })
  }
}

// Service instances for different microservices
export const catalogApi = new ApiClient(`${API_BASE}/api/catalog`)
export const identityApi = new ApiClient(`${IDENTITY_API}`)
export const inventoryApi = new ApiClient(`${API_BASE}/api/inventory`)
export const orderingApi = new ApiClient(`${API_BASE}/api/ordering`)

// Event-related API operations
export interface CreateEventRequest {
  name: string
  description: string
  eventDate: string
  venue: string
  maxCapacity: number
  basePrice: number
  categoryId?: string
  imageUrl?: string
  tags?: string[]
}

export interface UpdateEventRequest extends CreateEventRequest {
  id: string
  isActive?: boolean
}

export interface Event {
  id: string
  name: string
  description: string
  eventDate: string
  venue: string
  maxCapacity: number
  basePrice: number
  status: "active" | "inactive"
  createdAt: string
  updatedAt: string
  categoryId?: string
  imageUrl?: string
  tags: string[]
}

export interface EventListResponse {
  events: Event[]
  totalCount: number
  pageSize: number
  currentPage: number
}

export interface EventFilters {
  search?: string
  status?: "all" | "active" | "inactive"
  dateFrom?: string
  dateTo?: string
  page?: number
  pageSize?: number
}

export const eventService = {
  // Get all events with optional filters
  async getEvents(filters?: EventFilters): Promise<EventListResponse> {
    const params = new URLSearchParams()
    
    if (filters?.search) params.append("search", filters.search)
    if (filters?.status && filters.status !== "all") params.append("status", filters.status)
    if (filters?.dateFrom) params.append("dateFrom", filters.dateFrom)
    if (filters?.dateTo) params.append("dateTo", filters.dateTo)
    if (filters?.page) params.append("page", filters.page.toString())
    if (filters?.pageSize) params.append("pageSize", filters.pageSize.toString())

    const queryString = params.toString()
    const endpoint = queryString ? `/events?${queryString}` : "/events"
    
    return catalogApi.get<EventListResponse>(endpoint)
  },

  // Get single event by ID
  async getEvent(id: string): Promise<Event> {
    return catalogApi.get<Event>(`/events/${id}`)
  },

  // Create new event
  async createEvent(data: CreateEventRequest): Promise<Event> {
    return catalogApi.post<Event>("/events", data)
  },

  // Update existing event
  async updateEvent(id: string, data: Partial<UpdateEventRequest>): Promise<Event> {
    return catalogApi.put<Event>(`/events/${id}`, data)
  },

  // Delete event (soft delete)
  async deleteEvent(id: string): Promise<void> {
    return catalogApi.delete<void>(`/events/${id}`)
  },

  // Activate/deactivate event
  async toggleEventStatus(id: string, isActive: boolean): Promise<Event> {
    return catalogApi.put<Event>(`/events/${id}/status`, { isActive })
  }
}

// Admin authentication API operations
export interface AdminLoginRequest {
  email: string
  password: string
}

export interface AdminLoginResponse {
  token: string
  user: {
    id: string
    email: string
    role: string
    name: string
  }
}

export interface AdminUser {
  id: string
  email: string
  role: string
  name: string
}

export const authService = {
  // Admin login
  async login(credentials: AdminLoginRequest): Promise<AdminLoginResponse> {
    return identityApi.post<AdminLoginResponse>("/token", credentials)
  },

  // Verify current token and get user info
  async verifyToken(): Promise<AdminUser> {
    return identityApi.get<AdminUser>("/auth/admin/me")
  },

  // Logout (client-side token removal)
  logout(): void {
    if (typeof window !== "undefined") {
      localStorage.removeItem("adminToken")
    }
  }
}

// Seat management API operations (for inventory service)
export interface Seat {
  id: string
  eventId: string
  section: string
  row: string
  number: string
  status: "available" | "reserved" | "sold"
  price: number
}

export interface CreateSeatRequest {
  eventId: string
  section: string
  row: string
  number: string
  price: number
}

export const seatService = {
  // Get seats for an event
  async getEventSeats(eventId: string): Promise<Seat[]> {
    return inventoryApi.get<Seat[]>(`/events/${eventId}/seats`)
  },

  // Create seats for an event
  async createSeats(seats: CreateSeatRequest[]): Promise<Seat[]> {
    return inventoryApi.post<Seat[]>("/seats/batch", { seats })
  },

  // Update seat price
  async updateSeatPrice(seatId: string, price: number): Promise<Seat> {
    return inventoryApi.put<Seat>(`/seats/${seatId}`, { price })
  },

  // Get seat statistics for an event
  async getSeatStats(eventId: string): Promise<{
    total: number
    available: number
    reserved: number
    sold: number
  }> {
    return inventoryApi.get(`/events/${eventId}/seats/stats`)
  }
}

// Error handling utility
export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public code?: string
  ) {
    super(message)
    this.name = "ApiError"
  }
}

// Upload utility for event images
export const uploadService = {
  async uploadEventImage(file: File): Promise<{ url: string }> {
    const formData = new FormData()
    formData.append("image", file)
    formData.append("type", "event")

    const token = getAuthToken()
    const response = await fetch(`${API_BASE}/api/upload/image`, {
      method: "POST",
      headers: {
        ...(token && { Authorization: `Bearer ${token}` })
      },
      body: formData
    })

    if (!response.ok) {
      throw new ApiError("Failed to upload image", response.status)
    }

    return response.json()
  }
}