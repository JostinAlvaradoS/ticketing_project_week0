import type { EventSummary } from "@/lib/types"

export const MOCK_EVENTS: EventSummary[] = [
  {
    id: "550e8400-e29b-41d4-a716-446655440000",
    name: "Test Concert",
    description: "A test concert for smoke testing",
    date: "2026-03-15T19:00:00Z",
    basePrice: 50.0,
    venue: "Main Arena",
  },
  {
    id: "660e9500-f39c-52e5-b827-557766551111",
    name: "Jazz Night",
    description: "An evening of smooth jazz with top artists",
    date: "2026-04-20T20:00:00Z",
    basePrice: 75.0,
    venue: "Blue Note Lounge",
  },
  {
    id: "770e0600-a40d-63f6-c938-668877662222",
    name: "Electronic Festival",
    description: "A full day of electronic music and visual arts",
    date: "2026-05-10T14:00:00Z",
    basePrice: 120.0,
    venue: "Outdoor Stage",
  },
]
