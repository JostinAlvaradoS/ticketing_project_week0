"use client"

import Link from "next/link"
import { usePathname, useRouter } from "next/navigation"
import { useEffect, useState } from "react"
import { cn } from "@/lib/utils"
import { useAdminAuth } from "@/context/admin-auth-context"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Separator } from "@/components/ui/separator"
import { 
  Calendar, 
  BarChart3, 
  Menu, 
  X, 
  LogOut, 
  Plus,
  Loader2
} from "lucide-react"

const adminNavLinks = [
  {
    href: "/admin/dashboard", 
    label: "Dashboard",
    icon: BarChart3
  },
  {
    href: "/admin/events",
    label: "Eventos",
    icon: Calendar
  }
]

export default function AdminLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const pathname = usePathname()
  const router = useRouter()
  const { user, logout, isAuthenticated, isLoading } = useAdminAuth()
  const [sidebarOpen, setSidebarOpen] = useState(false)

  useEffect(() => {
    // Redirect to login if not authenticated and not on login page
    if (!isLoading && !isAuthenticated && pathname !== "/admin/login") {
      router.push("/admin/login")
    }
  }, [isAuthenticated, isLoading, pathname, router])

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="flex flex-col items-center space-y-4">
          <Loader2 className="h-12 w-12 animate-spin text-primary" />
          <p className="text-muted-foreground">Cargando...</p>
        </div>
      </div>
    )
  }

  // Don't render layout for login page
  if (pathname === "/admin/login") {
    return <>{children}</>
  }

  if (!isAuthenticated) {
    return null // Will redirect to login
  }

  const handleLogout = () => {
    logout()
    router.push("/admin/login")
  }

  return (
    <div className="min-h-screen bg-background">
      {/* Mobile sidebar overlay */}
      {sidebarOpen && (
        <div 
          className="fixed inset-0 z-40 lg:hidden bg-background/80 backdrop-blur-sm"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* Sidebar */}
      <Card className={cn(
        "fixed inset-y-0 left-0 z-50 w-64 transform transition-transform lg:translate-x-0 border-r",
        sidebarOpen ? "translate-x-0" : "-translate-x-full"
      )}>
        {/* Sidebar header */}
        <div className="flex items-center justify-between h-16 px-6 border-b">
          <h2 className="text-lg font-semibold">
            Admin Panel
          </h2>
          <Button
            variant="ghost"
            size="sm"
            className="lg:hidden"
            onClick={() => setSidebarOpen(false)}
          >
            <X className="h-4 w-4" />
          </Button>
        </div>

        {/* Navigation */}
        <CardContent className="p-4">
          <nav className="space-y-2">
            {adminNavLinks.map((link) => {
              const isActive = pathname === link.href || pathname.startsWith(link.href + "/")
              const IconComponent = link.icon
              
              return (
                <Button
                  key={link.href}
                  variant={isActive ? "default" : "ghost"}
                  className="w-full justify-start"
                  asChild
                >
                  <Link href={link.href}>
                    <IconComponent className="mr-3 h-4 w-4" />
                    {link.label}
                  </Link>
                </Button>
              )
            })}
          </nav>
        </CardContent>

        {/* User info at bottom */}
        <div className="absolute bottom-0 left-0 right-0 p-4 border-t">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <Avatar className="h-8 w-8">
                <AvatarFallback>
                  {user?.name?.charAt(0) || "A"}
                </AvatarFallback>
              </Avatar>
              <div>
                <p className="text-sm font-medium">
                  {user?.name || "Admin"}
                </p>
                <p className="text-xs text-muted-foreground">Administrador</p>
              </div>
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={handleLogout}
              title="Cerrar sesión"
            >
              <LogOut className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </Card>

      {/* Main content area */}
      <div className="lg:pl-64">
        {/* Top navigation bar */}
        <Card className="sticky top-0 z-30 border-b border-l-0 border-r-0 border-t-0 rounded-none">
          <div className="h-16 flex items-center px-6">
            <Button
              variant="ghost"
              size="sm"
              className="lg:hidden mr-4"
              onClick={() => setSidebarOpen(true)}
            >
              <Menu className="h-4 w-4" />
            </Button>
            
            <div className="flex-1">
              <h1 className="text-xl font-semibold">
                Panel de Administración
              </h1>
            </div>
          </div>
        </Card>

        {/* Page content */}
        <main className="p-6">
          {children}
        </main>
      </div>
    </div>
  )
}