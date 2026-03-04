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
  const { isAuthenticated } = useAuth()
  const pathname = usePathname()

  // Don't apply regular auth to admin routes
  if (pathname?.startsWith('/admin')) {
    return <>{children}</>
  }

  if (!isAuthenticated) {
    return <LoginScreen />
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
