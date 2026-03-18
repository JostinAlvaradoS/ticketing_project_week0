"use client"

import { SWRConfig } from "swr"
import { usePathname } from "next/navigation" 
import { AuthProvider, useAuth } from "@/context/auth-context"
import { AdminAuthProvider } from "@/context/admin-auth-context"
import { CartProvider } from "@/context/cart-context"
import { Navbar } from "@/components/navbar"
import { LoginScreen } from "@/components/login-screen"
import type { ReactNode } from "react"

function AuthenticatedApp({ children }: { children: ReactNode }) {
  // Hardcoded authentication for automation development
  const userId = "00000000-0000-0000-0000-000000000001"
  const isAuthenticated = true
  const pathname = usePathname()

  // Don't apply regular auth to admin routes
  if (pathname?.startsWith('/admin')) {
    return <>{children}</>
  }

  return (
    <CartProvider>
      <Navbar />
      {children}
    </CartProvider>
  )
}

function ConditionalAuthProvider({ children }: { children: ReactNode }) {
  const pathname = usePathname()

  // Use AdminAuthProvider for admin routes
  if (pathname?.startsWith('/admin')) {
    return (
      <AdminAuthProvider>
        {children}
      </AdminAuthProvider>
    )
  }

  // Use regular AuthProvider for user routes
  return (
    <AuthProvider>
      <AuthenticatedApp>{children}</AuthenticatedApp>
    </AuthProvider>
  )
}

export function Providers({ children }: { children: ReactNode }) {
  return (
    <SWRConfig
      value={{
        revalidateOnFocus: false,
        errorRetryCount: 2,
      }}
    >
      <ConditionalAuthProvider>
        {children}
      </ConditionalAuthProvider>
    </SWRConfig>
  )
}
