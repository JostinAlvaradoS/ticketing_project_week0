import { ButtonHTMLAttributes, forwardRef, ReactNode } from "react"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"
import type { VariantProps } from "class-variance-authority"

interface AdminButtonProps extends 
  Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'size'>,
  VariantProps<typeof Button> {
  isLoading?: boolean
  asChild?: boolean
}

const AdminButton = forwardRef<HTMLButtonElement, AdminButtonProps>(
  ({ className, variant = "default", size = "default", isLoading, children, disabled, asChild, ...props }, ref) => {
    // Create a single child element that contains both spinner and children
    const content: ReactNode = isLoading ? (
      <span className="inline-flex items-center">
        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2" />
        {children}
      </span>
    ) : children

    return (
      <Button
        className={cn(className)}
        variant={variant}
        size={size}
        disabled={disabled || isLoading}
        asChild={asChild}
        ref={ref}
        {...props}
      >
        {content}
      </Button>
    )
  }
)

AdminButton.displayName = "AdminButton"

export { AdminButton }