import { NextRequest, NextResponse } from "next/server"

// This should match your Identity Service endpoint
const IDENTITY_SERVICE_URL = process.env.IDENTITY_SERVICE_URL || "http://localhost:50000"

export async function POST(request: NextRequest) {
  try {
    const body = await request.json()
    let { email, password } = body

    if (!email || !password) {
      return NextResponse.json(
        { message: "Email and password are required" },
        { status: 400 }
      )
    }

    // Fix escaped characters in password (e.g., \041 -> !, \044 -> $, etc.)
    password = password
      .replace(/\\041/g, '!')   // Octal escape for !
      .replace(/\\044/g, '$')   // Octal escape for $ 
      .replace(/\\040/g, ' ')   // Octal escape for space
      .replace(/\\035/g, '#')   // Octal escape for #
      .replace(/\\046/g, '&')   // Octal escape for &

    // First check if Identity service is reachable
    try {
      console.log("Testing Identity service connectivity...")
      const controller = new AbortController()
      const timeoutId = setTimeout(() => controller.abort(), 5000)
      
      const healthCheck = await fetch(`${IDENTITY_SERVICE_URL}/health`, { 
        method: "GET",
        signal: controller.signal
      })
      
      clearTimeout(timeoutId)
      console.log("Health check response:", healthCheck.status)
    } catch (healthError) {
      console.error("Identity service not reachable:", healthError)
      return NextResponse.json(
        { 
          message: "Servicio de autenticación no disponible. Verifique que el Identity service esté corriendo.", 
          debug: `Cannot reach Identity service at ${IDENTITY_SERVICE_URL}`,
          suggestion: "Ejecute: cd services/identity/src/Identity.Api && dotnet run"
        },
        { status: 503 }
      )
    }

    // Call Identity Service to authenticate
    console.log("Calling Identity service at:", `${IDENTITY_SERVICE_URL}/token`)
    console.log("Request payload:", { email, password: password.substring(0, 3) + "***" })
    
    const identityResponse = await fetch(`${IDENTITY_SERVICE_URL}/token`, {
      method: "POST", 
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        email,
        password,
      }),
    })

    console.log("Identity service response status:", identityResponse.status)
    console.log("Identity service response headers:", Object.fromEntries(identityResponse.headers.entries()))
    
    if (!identityResponse.ok) {
      const responseText = await identityResponse.text()
      console.log("Identity service error response:", responseText)
      
      let errorData = null
      try {
        errorData = JSON.parse(responseText)
      } catch {
        errorData = { message: responseText || "Error desconocido del servicio de autenticación" }
      }
      
      return NextResponse.json(
        { 
          message: errorData?.message || "Credenciales inválidas",
          debug: `Identity service returned ${identityResponse.status}: ${responseText}`
        },
        { status: 401 }
      )
    }

    const { token, userRole, userEmail, expiresAt } = await identityResponse.json()

    console.log("User:" , {token});
    

    // Validate user has admin role (direct from response and JWT verification)
    if (userRole !== "Admin") {
      return NextResponse.json(
        { message: "Acceso no autorizado. Se requiere rol de administrador." },
        { status: 403 }
      )
    }

    // Additional JWT validation for security
    try {
      const payload = JSON.parse(atob(token.split(".")[1]))
      console.log("JWT Payload:", payload)
      
      let jwtRole = payload.role || payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]
      
      // Handle case where role might be an array (due to duplicate claims)
      if (Array.isArray(jwtRole)) {
        jwtRole = jwtRole[0] // Take the first role if it's an array
      }
      
      if (jwtRole !== "Admin") {
        return NextResponse.json(
          { 
            message: "Token inválido - rol no coincide.",
            debug: `Expected 'Admin', got '${jwtRole}' (type: ${typeof jwtRole})`
          },
          { status: 403 }
        )
      }
    } catch (error) {
      return NextResponse.json(
        { message: "Token inválido" },
        { status: 401 }
      )
    }

    // Set token in HTTP-only cookie for security
    const response = NextResponse.json({ 
      token, 
      userRole, 
      userEmail,
      expiresAt 
    })
    response.cookies.set("admin-token", token, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      maxAge: 60 * 60 * 8, // 8 hours
      path: "/admin"
    })

    return response
    
  } catch (error) {
    console.error("Admin login error:", error)
    return NextResponse.json(
      { message: "Error interno del servidor" },
      { status: 500 }
    )
  }
}