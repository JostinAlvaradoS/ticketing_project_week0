import { API_CONFIG, RETRY_DELAY_MS, RETRY_ADD_TO_CART_ATTEMPTS } from "./config"
import type {
  AddToCartRequest,
  AddToCartResponse,
  CheckoutRequest,
  Order,
} from "@/lib/types"

function delay(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms))
}

export async function addToCart(
  data: AddToCartRequest
): Promise<AddToCartResponse> {
  const url = `${API_CONFIG.ordering}/cart/add`
  console.log(`[addToCart] Calling ${url}`, data)
  
  try {
    const res = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    })
    
    console.log(`[addToCart] Response status: ${res.status}`)
    
    const contentType = res.headers.get("content-type")
    let jsonBody: any = {}
    
    if (contentType?.includes("application/json")) {
      jsonBody = await res.json()
      console.log(`[addToCart] Response body:`, jsonBody)
    }
    
    if (!res.ok) {
      const errorMsg = jsonBody.errorMessage || jsonBody.error || `Failed to add to cart: ${res.status} ${res.statusText}`
      console.error(`[addToCart] Error: ${errorMsg}`)
      throw new Error(errorMsg)
    }
    
    return jsonBody
  } catch (err) {
    console.error(`[addToCart] Network error:`, err)
    if (err instanceof TypeError && err.message.includes("Failed to fetch")) {
      throw new Error(
        `Failed to connect to Ordering service at ${API_CONFIG.ordering}. Make sure the service is running.`
      )
    }
    throw err
  }
}

/**
 * Retry adding to cart with delay to account for Kafka event propagation.
 * The reservation store in Ordering uses Kafka events, so there's a ~2-3s delay.
 */
export async function addToCartWithRetry(
  data: AddToCartRequest
): Promise<AddToCartResponse> {
  let lastError: Error | null = null

  for (let attempt = 1; attempt <= RETRY_ADD_TO_CART_ATTEMPTS; attempt++) {
    try {
      console.log(`[addToCartWithRetry] Attempt ${attempt}/${RETRY_ADD_TO_CART_ATTEMPTS}`)
      return await addToCart(data)
    } catch (error) {
      lastError = error instanceof Error ? error : new Error(String(error))
      console.log(`[addToCartWithRetry] Attempt ${attempt} failed:`, lastError.message)
      if (attempt < RETRY_ADD_TO_CART_ATTEMPTS) {
        console.log(`[addToCartWithRetry] Waiting ${RETRY_DELAY_MS}ms before retry...`)
        await delay(RETRY_DELAY_MS)
      }
    }
  }

  console.error(`[addToCartWithRetry] All ${RETRY_ADD_TO_CART_ATTEMPTS} attempts failed`)
  throw lastError
}

export async function checkout(data: CheckoutRequest): Promise<Order> {
  const res = await fetch(`${API_CONFIG.ordering}/orders/checkout`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  })
  if (res.status === 404) {
    throw new Error("Order not found")
  }
  if (res.status === 401) {
    throw new Error("Unauthorized")
  }
  if (res.status === 400) {
    throw new Error("Order is not in draft state")
  }
  if (!res.ok) {
    throw new Error(`Checkout failed: ${res.status} ${res.statusText}`)
  }
  return res.json()
}
