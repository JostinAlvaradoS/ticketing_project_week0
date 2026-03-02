import { ReactNode } from "react"
import { cn } from "@/lib/utils"

interface AdminTableProps {
  children: ReactNode
  className?: string
}

interface AdminTableHeaderProps {
  children: ReactNode
  className?: string
}

interface AdminTableBodyProps {
  children: ReactNode
  className?: string
}

interface AdminTableRowProps {
  children: ReactNode
  className?: string
  onClick?: () => void
}

interface AdminTableCellProps {
  children: ReactNode
  className?: string
  header?: boolean
}

function AdminTable({ children, className }: AdminTableProps) {
  return (
    <div className="overflow-x-auto bg-white rounded-lg shadow">
      <table className={cn("min-w-full divide-y divide-gray-200", className)}>
        {children}
      </table>
    </div>
  )
}

function AdminTableHeader({ children, className }: AdminTableHeaderProps) {
  return (
    <thead className={cn("bg-gray-50", className)}>
      {children}
    </thead>
  )
}

function AdminTableBody({ children, className }: AdminTableBodyProps) {
  return (
    <tbody className={cn("bg-white divide-y divide-gray-200", className)}>
      {children}
    </tbody>
  )
}

function AdminTableRow({ children, className, onClick }: AdminTableRowProps) {
  return (
    <tr 
      className={cn(
        "hover:bg-gray-50 transition-colors",
        onClick && "cursor-pointer",
        className
      )}
      onClick={onClick}
    >
      {children}
    </tr>
  )
}

function AdminTableCell({ children, className, header }: AdminTableCellProps) {
  if (header) {
    return (
      <th className={cn(
        "px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider",
        className
      )}>
        {children}
      </th>
    )
  }

  return (
    <td className={cn("px-6 py-4 whitespace-nowrap text-sm text-gray-900", className)}>
      {children}
    </td>
  )
}

// Loading state component
interface AdminTableLoadingProps {
  columns: number
  rows?: number
}

function AdminTableLoading({ columns, rows = 5 }: AdminTableLoadingProps) {
  return (
    <AdminTable>
      <AdminTableHeader>
        <AdminTableRow>
          {Array.from({ length: columns }).map((_, i) => (
            <AdminTableCell key={i} header>
              <div className="h-4 bg-gray-200 rounded animate-pulse" />
            </AdminTableCell>
          ))}
        </AdminTableRow>
      </AdminTableHeader>
      <AdminTableBody>
        {Array.from({ length: rows }).map((_, rowIndex) => (
          <AdminTableRow key={rowIndex}>
            {Array.from({ length: columns }).map((_, colIndex) => (
              <AdminTableCell key={colIndex}>
                <div className="h-4 bg-gray-200 rounded animate-pulse" />
              </AdminTableCell>
            ))}
          </AdminTableRow>
        ))}
      </AdminTableBody>
    </AdminTable>
  )
}

// Empty state component
interface AdminTableEmptyProps {
  columns: number
  message?: string
  icon?: string
}

function AdminTableEmpty({ columns, message = "No hay datos disponibles", icon = "📭" }: AdminTableEmptyProps) {
  return (
    <AdminTable>
      <AdminTableBody>
        <AdminTableRow>
          <AdminTableCell className="text-center py-12">
            <div className="flex flex-col items-center">
              <span className="text-4xl mb-2">{icon}</span>
              <p className="text-gray-500 text-sm">{message}</p>
            </div>
          </AdminTableCell>
        </AdminTableRow>
      </AdminTableBody>
    </AdminTable>
  )
}

export {
  AdminTable,
  AdminTableHeader, 
  AdminTableBody,
  AdminTableRow,
  AdminTableCell,
  AdminTableLoading,
  AdminTableEmpty
}