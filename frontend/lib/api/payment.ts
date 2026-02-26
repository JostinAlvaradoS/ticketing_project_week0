import { API_CONFIG } from "./config"

export interface ProcessPaymentRequest {
  orderId: string
  customerId: string
  reservationId?: string
  amount: number
  currency?: string
  paymentMethod?: string
}

export interface ProcessPaymentResponse {
  success: boolean
  errorMessage: string | null
  payment?: {
    id: string
    orderId: string
    customerId: string
    reservationId: string | null
    amount: number
    currency: string
    paymentMethod: string
    status: string
    errorCode: string | null
    errorMessage: string | null
    failureReason: string | null
    createdAt: string
    processedAt: string | null
    isSimulated: boolean
    simulatedResponse: string | null
  }
}

export async function processPayment(
  data: ProcessPaymentRequest
): Promise<ProcessPaymentResponse> {
  const url = `${API_CONFIG.payment}/payments`
  
  console.log(`[processPayment] Calling ${url}`, data)
  
  try {
    const res = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    })
    
    console.log(`[processPayment] Response status: ${res.status}`)
    
    const contentType = res.headers.get("content-type")
    let jsonBody: any = {}
    
    if (contentType?.includes("application/json")) {
      jsonBody = await res.json()
      console.log(`[processPayment] Response body:`, jsonBody)
    }
    
    if (!res.ok) {
      const errorMsg = 
        jsonBody.errorMessage || 
        jsonBody.error || 
        `Payment failed: ${res.status} ${res.statusText}`
      console.error(`[processPayment] Error: ${errorMsg}`)
      throw new Error(errorMsg)
    }
    
    return jsonBody as ProcessPaymentResponse
  } catch (err) {
    console.error(`[processPayment] Network error:`, err)
    if (err instanceof TypeError && err.message.includes("Failed to fetch")) {
      throw new Error(
        `Failed to connect to Payment service at ${API_CONFIG.payment}. Make sure the service is running.`
      )
    }
    throw err
  }
}
