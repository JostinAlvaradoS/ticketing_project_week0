"use client"

import Link from "next/link"
import { Ticket, ShoppingCart, LogOut, User } from "lucide-react"
import { useCart } from "@/context/cart-context"
import { useAuth } from "@/context/auth-context"
import { Button } from "@/components/ui/button"
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip"

export function Navbar() {
  const { order } = useCart()
  const { userId, logout } = useAuth()
  const itemCount = order?.items.length ?? 0

  return (
    <header className="sticky top-0 z-50 border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <nav className="mx-auto flex max-w-7xl items-center justify-between px-4 h-14">
        <Link
          href="/"
          className="flex items-center gap-2 text-foreground hover:text-accent transition-colors"
        >
          <Ticket className="size-5" />
          <span className="font-bold tracking-tight">SpecKit Tickets</span>
        </Link>

        <div className="flex items-center gap-2">
          {/* User indicator */}
          <div className="hidden sm:flex items-center gap-1.5 rounded-md bg-secondary px-2.5 py-1.5 text-xs text-muted-foreground">
            <User className="size-3.5" />
            <span className="font-mono">{userId}</span>
          </div>

          {/* Cart */}
          <Button
            variant="ghost"
            size="sm"
            asChild
            className="relative text-muted-foreground hover:text-foreground"
          >
            <Link href="/checkout">
              <ShoppingCart className="size-5" />
              {itemCount > 0 && (
                <span className="absolute -top-1 -right-1 size-5 rounded-full bg-accent text-accent-foreground text-xs font-bold flex items-center justify-center">
                  {itemCount}
                </span>
              )}
              <span className="sr-only">Cart ({itemCount} items)</span>
            </Link>
          </Button>

          {/* Logout */}
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={logout}
                  className="text-muted-foreground hover:text-foreground"
                >
                  <LogOut className="size-4" />
                  <span className="sr-only">Sign out</span>
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                <p>Sign out</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        </div>
      </nav>
    </header>
  )
}
