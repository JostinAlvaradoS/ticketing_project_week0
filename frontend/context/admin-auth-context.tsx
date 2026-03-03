"use client"

import {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
  type ReactNode,
} from "react"

interface AdminUser {
  id: string
  name: string
  email: string
  role: string
}

interface AdminAuthContextType {
  user: AdminUser | null
  token: string | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (token: string, user: AdminUser) => void
  logout: () => void
}

const AdminAuthContext = createContext<AdminAuthContextType | null>(null)

export function useAdminAuth() {
  const ctx = useContext(AdminAuthContext)
  if (!ctx) throw new Error("useAdminAuth must be used within an AdminAuthProvider")
  return ctx
}

const TOKEN_STORAGE_KEY = "admin-token"
const USER_STORAGE_KEY = "admin-user"

export function AdminAuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AdminUser | null>(null)
  const [token, setToken] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  // Initialize auth state from storage
  useEffect(() => {
    const storedToken = localStorage.getItem(TOKEN_STORAGE_KEY)
    const storedUser = localStorage.getItem(USER_STORAGE_KEY)
    
    if (storedToken && storedUser) {
      try {
        const parsedUser = JSON.parse(storedUser) as AdminUser
        // Validate token is not expired
        if (isTokenValid(storedToken)) {
          setToken(storedToken)
          setUser(parsedUser)
        } else {
          // Token expired, clear storage
          localStorage.removeItem(TOKEN_STORAGE_KEY)
          localStorage.removeItem(USER_STORAGE_KEY)
        }
      } catch (error) {
        // Invalid stored data, clear it
        localStorage.removeItem(TOKEN_STORAGE_KEY)
        localStorage.removeItem(USER_STORAGE_KEY)
      }
    }
    
    setIsLoading(false)
  }, [])

  const login = useCallback((newToken: string, newUser: AdminUser) => {
    // Validate user has admin role
    if (newUser.role !== "Admin") {
      throw new Error("Usuario no autorizado. Se requiere rol de administrador.")
    }

    setToken(newToken)
    setUser(newUser)
    
    localStorage.setItem(TOKEN_STORAGE_KEY, newToken)
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(newUser))
  }, [])

  const logout = useCallback(() => {
    setToken(null)
    setUser(null)
    
    localStorage.removeItem(TOKEN_STORAGE_KEY)
    localStorage.removeItem(USER_STORAGE_KEY)
  }, [])

  // Auto logout when token expires
  useEffect(() => {
    if (token && !isTokenValid(token)) {
      logout()
    }
  }, [token, logout])

  const contextValue: AdminAuthContextType = {
    user,
    token,
    isAuthenticated: !!(user && token && user.role === "Admin"),
    isLoading,
    login,
    logout,
  }

  return (
    <AdminAuthContext.Provider value={contextValue}>
      {children}
    </AdminAuthContext.Provider>
  )
}

// Helper function to validate JWT token expiration
function isTokenValid(token: string): boolean {
  try {
    const payload = JSON.parse(atob(token.split(".")[1]))
    const currentTime = Math.floor(Date.now() / 1000)
    return payload.exp > currentTime
  } catch {
    return false
  }
}

// Helper function to decode JWT token and extract user info
export function decodeAdminToken(token: string): AdminUser | null {
  try {
    const payload = JSON.parse(atob(token.split(".")[1]))
    
    let role = payload.role || payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]
    
    // Handle case where role might be an array (due to duplicate claims)
    if (Array.isArray(role)) {
      role = role[0] // Take the first role if it's an array
    }
    
    return {
      id: payload.sub || payload.userId,
      name: payload.name || payload.given_name || "Admin",
      email: payload.email,
      role: role
    }
  } catch (error) {
    console.error("Error decoding token:", error)
    return null
  }
}