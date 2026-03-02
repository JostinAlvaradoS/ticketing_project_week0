"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { useAdminAuth } from "@/context/admin-auth-context"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Label } from "@/components/ui/label"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Shield, Lock, AlertCircle, Info, Eye, EyeOff } from "lucide-react"

export default function AdminLoginPage() {
  const [formData, setFormData] = useState({
    email: "",
    password: ""
  })
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState("")
  const [showPassword, setShowPassword] = useState(false)
  
  const { login } = useAdminAuth()
  const router = useRouter()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsLoading(true)
    setError("")

    try {
      // Call Identity Service to get JWT token
      const response = await fetch("/api/auth/admin-login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          email: formData.email,
          password: formData.password,
        }),
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.message || "Error de autenticación")
      }

      const { token, userRole, userEmail } = await response.json()

      // Create user object from response
      const user = {
        id: userEmail, // Using email as ID for now
        name: "Admin User", // Default name since Identity service doesn't provide it
        email: userEmail,
        role: userRole
      }
      
      if (!user || user.role !== "Admin") {
        throw new Error("Acceso no autorizado. Se requiere rol de administrador.")
      }

      // Login successful
      login(token, user)
      router.push("/admin/events")
      
    } catch (err) {
      setError(err instanceof Error ? err.message : "Error desconocido")
    } finally {
      setIsLoading(false)
    }
  }

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData(prev => ({
      ...prev,
      [e.target.name]: e.target.value
    }))
  }

  const handleAutoFill = () => {
    setFormData({ 
      email: "admin@ticketing.com", 
      password: "Admin123!" 
    })
  }

  return (
    <main className="min-h-screen bg-background flex items-center justify-center px-4">
      <Card className="w-full max-w-md bg-card border-border">
        <CardHeader className="flex flex-col items-center gap-4 pb-6">
          <div className="size-14 rounded-full bg-accent/15 flex items-center justify-center">
            <Shield className="size-7 text-accent" />
          </div>
          <div className="flex flex-col items-center gap-1">
            <CardTitle className="text-xl text-foreground">
              Panel de Administración
            </CardTitle>
            <p className="text-sm text-muted-foreground text-center text-pretty">
              Ingresa tus credenciales para acceder al dashboard
            </p>
          </div>
        </CardHeader>
        <CardContent className="space-y-6">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="email" className="text-foreground">
                Email
              </Label>
              <Input
                id="email"
                name="email"
                type="email"
                placeholder="admin@ticketing.com"
                value={formData.email}
                onChange={handleInputChange}
                className="bg-secondary border-border text-foreground placeholder:text-muted-foreground"
                required
                autoComplete="email"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="password" className="text-foreground">
                Contraseña
              </Label>
              <div className="relative">
                <Input
                  id="password"
                  name="password"
                  type={showPassword ? "text" : "password"}
                  placeholder="Ingresa tu contraseña"
                  value={formData.password}
                  onChange={handleInputChange}
                  className="bg-secondary border-border text-foreground placeholder:text-muted-foreground pr-10"
                  required
                  autoComplete="current-password"
                />
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                  onClick={() => setShowPassword(!showPassword)}
                  tabIndex={-1}
                >
                  {showPassword ? (
                    <EyeOff className="h-4 w-4 text-muted-foreground" />
                  ) : (
                    <Eye className="h-4 w-4 text-muted-foreground" />
                  )}
                </Button>
              </div>
            </div>

            {error && (
              <Alert variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <Button
              type="submit"
              disabled={isLoading}
              className="w-full bg-primary text-primary-foreground hover:bg-primary/90"
            >
              {isLoading ? (
                <>
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-primary-foreground mr-2" />
                  Iniciando sesión...
                </>
              ) : (
                <>
                  <Lock className="h-4 w-4 mr-2" />
                  Iniciar Sesión
                </>
              )}
            </Button>
          </form>

          <div className="pt-6 border-t border-border">
            <Alert>
              <Info className="h-4 w-4" />
              <AlertDescription className="space-y-2">
                <p className="text-sm font-medium">Credenciales por defecto:</p>
                <div className="grid grid-cols-1 gap-1 text-xs">
                  <div className="flex items-center justify-between bg-secondary rounded px-2 py-1">
                    <span className="text-muted-foreground">Email:</span>
                    <code className="text-foreground">admin@ticketing.com</code>
                  </div>
                  <div className="flex items-center justify-between bg-secondary rounded px-2 py-1">
                    <span className="text-muted-foreground">Password:</span>
                    <code className="text-foreground">Admin123!</code>
                  </div>
                </div>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={handleAutoFill}
                  className="w-full mt-2 text-xs h-8 text-accent hover:text-accent-foreground hover:bg-accent/20"
                >
                  Auto-completar formulario
                </Button>
              </AlertDescription>
            </Alert>
          </div>

          <p className="text-xs text-muted-foreground text-center">
            Solo administradores autorizados pueden acceder a este panel.
          </p>
        </CardContent>
      </Card>
    </main>
  )
}