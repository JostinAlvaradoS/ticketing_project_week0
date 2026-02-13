/**
 * Configuración centralizada de polling
 */

export const POLLING_CONFIG = {
  reservation: {
    maxAttempts: 20,
    initialDelay: 100,
    maxDelay: 1000,
    timeoutMs: 10000,
  },
  payment: {
    maxAttempts: 30,
    initialDelay: 500,
    maxDelay: 2000,
    timeoutMs: 15000,
  },
} as const
