import { NextResponse } from 'next/server'
import type { NextRequest } from 'next/server'

export function middleware(request: NextRequest) {
  // Only apply middleware to admin routes
  if (!request.nextUrl.pathname.startsWith('/admin')) {
    return NextResponse.next()
  }

  // Allow access to login page
  if (request.nextUrl.pathname === '/admin/login') {
    return NextResponse.next()
  }

  // Check for admin token in cookies or headers
  const token = request.cookies.get('admin-token')?.value || 
                request.headers.get('authorization')?.replace('Bearer ', '')

  if (!token) {
    // No token, redirect to login
    return NextResponse.redirect(new URL('/admin/login', request.url))
  }

  try {
    // Decode and validate JWT token
    const payload = JSON.parse(atob(token.split('.')[1]))
    const currentTime = Math.floor(Date.now() / 1000)
    
    // Check if token is expired
    if (payload.exp <= currentTime) {
      return NextResponse.redirect(new URL('/admin/login', request.url))
    }

    // Check if user has admin role
    let role = payload.role || payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]
    
    // Handle case where role might be an array (due to duplicate claims)
    if (Array.isArray(role)) {
      role = role[0] // Take the first role if it's an array
    }
    
    if (role !== 'Admin') {
      return NextResponse.redirect(new URL('/admin/login', request.url))
    }

    // Set token in response headers for API calls
    const response = NextResponse.next()
    response.headers.set('x-admin-token', token)
    
    return response
  } catch (error) {
    // Invalid token, redirect to login
    return NextResponse.redirect(new URL('/admin/login', request.url))
  }
}

export const config = {
  matcher: [
    '/admin/:path*'
  ]
}