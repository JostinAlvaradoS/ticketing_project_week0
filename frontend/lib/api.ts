import type {
  Event,
  Ticket,
  CreateEventPayload,
  CreateTicketsPayload,
  CreateTicketsResponse,
  ReserveTicketPayload,
  UpdateTicketPayload,
} from "./types"

const CRUD_URL = process.env.NEXT_PUBLIC_API_CRUD || "http://localhost:8002"
const PRODUCER_URL = process.env.NEXT_PUBLIC_API_PRODUCER || "http://localhost:8001"

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const text = await res.text()
    throw new Error(text || `Error ${res.status}`)
  }
  return res.json()
}

export const api = {
  // ─── Events ───────────────────────────────────────────
  async getEvents(): Promise<Event[]> {
    const res = await fetch(`${CRUD_URL}/api/events`)
    return handleResponse<Event[]>(res)
  },

  async getEvent(id: number): Promise<Event> {
    const res = await fetch(`${CRUD_URL}/api/events/${id}`)
    return handleResponse<Event>(res)
  },

  async createEvent(payload: CreateEventPayload): Promise<Event> {
    const res = await fetch(`${CRUD_URL}/api/events`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    })
    return handleResponse<Event>(res)
  },

  async updateEvent(id: number, payload: Partial<CreateEventPayload>): Promise<Event> {
    const res = await fetch(`${CRUD_URL}/api/events/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    })
    return handleResponse<Event>(res)
  },

  async deleteEvent(id: number): Promise<void> {
    const res = await fetch(`${CRUD_URL}/api/events/${id}`, { method: "DELETE" })
    if (!res.ok) throw new Error(`Failed to delete event ${id}`)
  },

  // ─── Tickets ──────────────────────────────────────────
  async getTicketsByEvent(eventId: number): Promise<Ticket[]> {
    const res = await fetch(`${CRUD_URL}/api/tickets/event/${eventId}`)
    return handleResponse<Ticket[]>(res)
  },

  async getTicket(id: number): Promise<Ticket> {
    const res = await fetch(`${CRUD_URL}/api/tickets/${id}`)
    return handleResponse<Ticket>(res)
  },

  async createTickets(payload: CreateTicketsPayload): Promise<CreateTicketsResponse> {
    const res = await fetch(`${CRUD_URL}/api/tickets/bulk`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    })
    return handleResponse<CreateTicketsResponse>(res)
  },

  async updateTicketStatus(id: number, payload: UpdateTicketPayload): Promise<Ticket> {
    const res = await fetch(`${CRUD_URL}/api/tickets/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    })
    return handleResponse<Ticket>(res)
  },

  // ─── Producer ─────────────────────────────────────────
  async reserveTicket(payload: ReserveTicketPayload): Promise<{ message: string; ticketId: number }> {
    const res = await fetch(`${PRODUCER_URL}/api/tickets/reserve`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    })
    if (res.status === 202) {
      return res.json()
    }
    throw new Error(await res.text())
  },

  // ─── Health ───────────────────────────────────────────
  async healthCrud(): Promise<boolean> {
    try {
      const res = await fetch(`${CRUD_URL}/health`)
      return res.ok
    } catch {
      return false
    }
  },

  async healthProducer(): Promise<boolean> {
    try {
      const res = await fetch(`${PRODUCER_URL}/health`)
      return res.ok
    } catch {
      return false
    }
  },
}
