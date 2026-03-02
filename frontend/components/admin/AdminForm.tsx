import { ReactNode, FormHTMLAttributes } from "react"
import { cn } from "@/lib/utils"
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card"
import { Label } from "@/components/ui/label"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { CheckCircle, AlertCircle } from "lucide-react"

interface AdminFormProps extends FormHTMLAttributes<HTMLFormElement> {
  children: ReactNode
  title?: string
  description?: string
}

interface AdminFormSectionProps {
  children: ReactNode
  title?: string
  description?: string
}

interface AdminFormFieldProps {
  children: ReactNode
  className?: string
}

interface AdminFormLabelProps {
  htmlFor?: string
  children: ReactNode
  required?: boolean
  className?: string
}

interface AdminFormInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  error?: string
}

interface AdminFormTextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  error?: string
}

interface AdminFormSelectWrapperProps {
  error?: string
  children: ReactNode
  value?: string
  onValueChange?: (value: string) => void
  placeholder?: string
  className?: string
}

function AdminForm({ children, title, description, className, ...props }: AdminFormProps) {
  return (
    <Card>
      {(title || description) && (
        <CardHeader>
          {title && <CardTitle>{title}</CardTitle>}
          {description && <CardDescription>{description}</CardDescription>}
        </CardHeader>
      )}
      
      <CardContent>
        <form className={cn("space-y-6", className)} {...props}>
          {children}
        </form>
      </CardContent>
    </Card>
  )
}

function AdminFormSection({ children, title, description }: AdminFormSectionProps) {
  return (
    <div className="space-y-6">
      {(title || description) && (
        <div className="pb-4 border-b">
          {title && (
            <h4 className="text-md font-medium">{title}</h4>
          )}
          {description && (
            <p className="mt-1 text-sm text-muted-foreground">{description}</p>
          )}
        </div>
      )}
      <div className="space-y-4">
        {children}
      </div>
    </div>
  )
}

function AdminFormField({ children, className }: AdminFormFieldProps) {
  return (
    <div className={cn("space-y-2", className)}>
      {children}
    </div>
  )
}

function AdminFormLabel({ htmlFor, children, required, className }: AdminFormLabelProps) {
  return (
    <Label 
      htmlFor={htmlFor}
      className={cn(className)}
    >
      {children}
      {required && <span className="text-destructive ml-1">*</span>}
    </Label>
  )
}

function AdminFormInput({ error, className, ...props }: AdminFormInputProps) {
  return (
    <div>
      <Input
        className={cn(
          error && "border-destructive focus-visible:ring-destructive",
          className
        )}
        {...props}
      />
      {error && (
        <p className="mt-1 text-sm text-destructive">{error}</p>
      )}
    </div>
  )
}

function AdminFormTextarea({ error, className, ...props }: AdminFormTextareaProps) {
  return (
    <div>
      <Textarea
        className={cn(
          error && "border-destructive focus-visible:ring-destructive",
          className
        )}
        {...props}
      />
      {error && (
        <p className="mt-1 text-sm text-destructive">{error}</p>
      )}
    </div>
  )
}

function AdminFormSelect({ error, className, children, value, onValueChange, placeholder, ...props }: AdminFormSelectWrapperProps) {
  return (
    <div>
      <Select value={value} onValueChange={onValueChange}>
        <SelectTrigger className={cn(
          error && "border-destructive focus:ring-destructive",
          className
        )}>
          <SelectValue placeholder={placeholder} />
        </SelectTrigger>
        <SelectContent>
          {children}
        </SelectContent>
      </Select>
      {error && (
        <p className="mt-1 text-sm text-destructive">{error}</p>
      )}
    </div>
  )
}

function AdminFormError({ children }: { children: ReactNode }) {
  return (
    <Alert variant="destructive">
      <AlertCircle className="h-4 w-4" />
      <AlertDescription>{children}</AlertDescription>
    </Alert>
  )
}

function AdminFormSuccess({ children }: { children: ReactNode }) {
  return (
    <Alert className="border-green-200 bg-green-50 text-green-800">
      <CheckCircle className="h-4 w-4 text-green-600" />
      <AlertDescription className="text-green-800">{children}</AlertDescription>
    </Alert>
  )
}

// Export SelectItem for use with AdminFormSelect
export { SelectItem }

export {
  AdminForm,
  AdminFormSection,
  AdminFormField,
  AdminFormLabel,
  AdminFormInput,
  AdminFormTextarea,
  AdminFormSelect,
  AdminFormError,
  AdminFormSuccess
}