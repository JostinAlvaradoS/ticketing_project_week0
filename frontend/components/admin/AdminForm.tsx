import { ReactNode, FormHTMLAttributes } from "react"
import { cn } from "@/lib/utils"

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

interface AdminFormSelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  error?: string
  children: ReactNode
}

function AdminForm({ children, title, description, className, ...props }: AdminFormProps) {
  return (
    <div className="bg-white shadow rounded-lg">
      {(title || description) && (
        <div className="px-6 py-4 border-b border-gray-200">
          {title && (
            <h3 className="text-lg font-medium text-gray-900">{title}</h3>
          )}
          {description && (
            <p className="mt-1 text-sm text-gray-600">{description}</p>
          )}
        </div>
      )}
      
      <form className={cn("p-6 space-y-6", className)} {...props}>
        {children}
      </form>
    </div>
  )
}

function AdminFormSection({ children, title, description }: AdminFormSectionProps) {
  return (
    <div className="space-y-6">
      {(title || description) && (
        <div className="pb-4 border-b border-gray-200">
          {title && (
            <h4 className="text-md font-medium text-gray-900">{title}</h4>
          )}
          {description && (
            <p className="mt-1 text-sm text-gray-600">{description}</p>
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
    <label 
      htmlFor={htmlFor}
      className={cn("block text-sm font-medium text-gray-700", className)}
    >
      {children}
      {required && <span className="text-red-500 ml-1">*</span>}
    </label>
  )
}

function AdminFormInput({ error, className, ...props }: AdminFormInputProps) {
  return (
    <div>
      <input
        className={cn(
          "block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm",
          error && "border-red-300 focus:border-red-500 focus:ring-red-500",
          className
        )}
        {...props}
      />
      {error && (
        <p className="mt-1 text-sm text-red-600">{error}</p>
      )}
    </div>
  )
}

function AdminFormTextarea({ error, className, ...props }: AdminFormTextareaProps) {
  return (
    <div>
      <textarea
        className={cn(
          "block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm",
          error && "border-red-300 focus:border-red-500 focus:ring-red-500",
          className
        )}
        {...props}
      />
      {error && (
        <p className="mt-1 text-sm text-red-600">{error}</p>
      )}
    </div>
  )
}

function AdminFormSelect({ error, className, children, ...props }: AdminFormSelectProps) {
  return (
    <div>
      <select
        className={cn(
          "block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm",
          error && "border-red-300 focus:border-red-500 focus:ring-red-500",
          className
        )}
        {...props}
      >
        {children}
      </select>
      {error && (
        <p className="mt-1 text-sm text-red-600">{error}</p>
      )}
    </div>
  )
}

function AdminFormError({ children }: { children: ReactNode }) {
  return (
    <div className="bg-red-50 border border-red-200 rounded-md p-4">
      <div className="flex">
        <div className="flex-shrink-0">
          <span className="text-red-400">⚠️</span>
        </div>
        <div className="ml-3">
          <p className="text-sm text-red-800">{children}</p>
        </div>
      </div>
    </div>
  )
}

function AdminFormSuccess({ children }: { children: ReactNode }) {
  return (
    <div className="bg-green-50 border border-green-200 rounded-md p-4">
      <div className="flex">
        <div className="flex-shrink-0">
          <span className="text-green-400">✅</span>
        </div>
        <div className="ml-3">
          <p className="text-sm text-green-800">{children}</p>
        </div>
      </div>
    </div>
  )
}

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