"use client"

import {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
  type ReactNode,
} from "react"

interface AuthContextType {
  userId: string | null
  isAuthenticated: boolean
  login: (userId: string) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextType | null>(null)

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider")
  return ctx
}

const STORAGE_KEY = "speckit-user-id"

export function AuthProvider({ children }: { children: ReactNode }) {
  // Use a random UUID for the session to ensure test idempotency/isolation
  const [userId, setUserId] = useState<string | null>(null)
  const [hydrated, setHydrated] = useState(false)

  useEffect(() => {
    // Generate a secure random UUID for this browser session if none exists
    let currentId = sessionStorage.getItem(STORAGE_KEY)
    if (!currentId) {
      currentId = crypto.randomUUID()
      sessionStorage.setItem(STORAGE_KEY, currentId)
    }
    setUserId(currentId)
    setHydrated(true)
  }, [])

  const login = useCallback((id: string) => {
    setUserId(id)
    sessionStorage.setItem(STORAGE_KEY, id)
  }, [])

  const logout = useCallback(() => {
    sessionStorage.removeItem(STORAGE_KEY)
    setUserId(null)
  }, [])

  if (!hydrated) return null

  return (
    <AuthContext.Provider
      value={{
        userId,
        isAuthenticated: userId !== null,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}
