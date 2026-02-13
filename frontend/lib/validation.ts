/**
 * Validación de tarjetas de crédito usando algoritmo de Luhn
 */

export function validateLuhn(cardNumber: string): boolean {
  const digits = cardNumber.replace(/\s/g, "")
  
  if (!/^\d+$/.test(digits)) return false
  if (digits.length < 13 || digits.length > 19) return false

  let sum = 0
  let isEven = false

  for (let i = digits.length - 1; i >= 0; i--) {
    let digit = parseInt(digits[i], 10)

    if (isEven) {
      digit *= 2
      if (digit > 9) {
        digit -= 9
      }
    }

    sum += digit
    isEven = !isEven
  }

  return sum % 10 === 0
}

export function validateEmail(email: string): boolean {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}

export function validateExpiryDate(expiryDate: string): { valid: boolean; message?: string } {
  if (!expiryDate || expiryDate.length !== 5) {
    return { valid: false, message: "Formato inválido (MM/YY)" }
  }

  const [month, year] = expiryDate.split("/")
  const expMonth = parseInt(month, 10)
  const expYear = parseInt(`20${year}`, 10)

  if (expMonth < 1 || expMonth > 12) {
    return { valid: false, message: "Mes inválido" }
  }

  const currentDate = new Date()
  const currentYear = currentDate.getFullYear()
  const currentMonth = currentDate.getMonth() + 1

  if (expYear < currentYear || (expYear === currentYear && expMonth < currentMonth)) {
    return { valid: false, message: "Tarjeta expirada" }
  }

  return { valid: true }
}
