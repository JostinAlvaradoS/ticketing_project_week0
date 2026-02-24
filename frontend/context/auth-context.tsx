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
  const [userId, setUserId] = useState<string | null>(null)
  const [hydrated, setHydrated] = useState(false)

  useEffect(() => {
    const stored = sessionStorage.getItem(STORAGE_KEY)
    if (stored) setUserId(stored)
    setHydrated(true)
  }, [])

  const login = useCallback((id: string) => {
    const trimmed = id.trim()
    if (!trimmed) return
    sessionStorage.setItem(STORAGE_KEY, trimmed)
    setUserId(trimmed)
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
