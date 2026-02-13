import React from "react"
import type { Metadata, Viewport } from "next"
import { Toaster } from "sonner"

import { Header } from "@/components/header"
import "./globals.css"

export const metadata: Metadata = {
  title: "Ticketing Dashboard",
  description: "Sistema de gestion de eventos y tickets",
}

export const viewport: Viewport = {
  themeColor: "#0a0a0a",
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="es" className="dark">
      <body className="font-sans antialiased">
        <Header />
        {children}
        <Toaster richColors position="bottom-right" />
      </body>
    </html>
  )
}
