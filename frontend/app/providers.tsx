"use client"

import { SWRConfig } from "swr"
import { AuthProvider, useAuth } from "@/context/auth-context"
import { CartProvider } from "@/context/cart-context"
import { Navbar } from "@/components/navbar"
import { LoginScreen } from "@/components/login-screen"
import type { ReactNode } from "react"

function AuthenticatedApp({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth()

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

export function Providers({ children }: { children: ReactNode }) {
  return (
    <SWRConfig
      value={{
        revalidateOnFocus: false,
        errorRetryCount: 2,
      }}
    >
      <AuthProvider>
        <AuthenticatedApp>{children}</AuthenticatedApp>
      </AuthProvider>
    </SWRConfig>
  )
}
