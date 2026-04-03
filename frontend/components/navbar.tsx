"use client"

import Link from "next/link"
import { Ticket, ShoppingCart } from "lucide-react"
import { useCart } from "@/context/cart-context"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

export function Navbar() {
  const { order } = useCart()
  const itemCount = order?.items.length ?? 0

  return (
    <header className="sticky top-0 z-50 border-b border-border/60 bg-background/80 backdrop-blur-md">
      <nav className="mx-auto flex max-w-7xl items-center justify-between px-4 h-14">
        <Link
          href="/"
          className="flex items-center gap-2.5 text-foreground hover:text-accent transition-colors group"
        >
          <div className="flex size-7 items-center justify-center rounded-md bg-accent/10 border border-accent/20 group-hover:bg-accent/20 transition-colors">
            <Ticket className="size-4 text-accent" />
          </div>
          <span className="font-bold tracking-tight">SpecKit</span>
          <span className="hidden sm:inline text-muted-foreground font-normal">Tickets</span>
        </Link>

        <Button
          variant="ghost"
          size="sm"
          asChild
          className="relative text-muted-foreground hover:text-foreground hover:bg-secondary"
        >
          <Link href="/checkout">
            <ShoppingCart className="size-5" />
            {itemCount > 0 && (
              <span className={cn(
                "absolute -top-1 -right-1 size-5 rounded-full text-xs font-bold",
                "flex items-center justify-center",
                "bg-accent text-accent-foreground",
                "ring-2 ring-background"
              )}>
                {itemCount}
              </span>
            )}
            <span className="sr-only">Cart ({itemCount} items)</span>
          </Link>
        </Button>
      </nav>
    </header>
  )
}
