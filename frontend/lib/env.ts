import { z } from "zod"

const envSchema = z.object({
  NEXT_PUBLIC_API_CRUD: z.string().min(1, "CRUD API URL is required"),
  NEXT_PUBLIC_API_PRODUCER: z.string().min(1, "Producer API URL is required"),
})

function validateEnv() {
  const parsed = envSchema.safeParse({
    NEXT_PUBLIC_API_CRUD: process.env.NEXT_PUBLIC_API_CRUD || "http://localhost:8002",
    NEXT_PUBLIC_API_PRODUCER: process.env.NEXT_PUBLIC_API_PRODUCER || "http://localhost:8001",
  })

  if (!parsed.success) {
    console.error("❌ Invalid environment variables:", parsed.error.flatten().fieldErrors)
    throw new Error("Invalid environment variables")
  }

  return parsed.data
}

export const env = validateEnv()
