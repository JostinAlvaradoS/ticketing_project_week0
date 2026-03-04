import { ReactNode } from "react"
import { cn } from "@/lib/utils"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Card } from "@/components/ui/card"

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
    <Card className="w-full">
      <Table className={cn(className)}>
        {children}
      </Table>
    </Card>
  )
}

function AdminTableHeader({ children, className }: AdminTableHeaderProps) {
  return (
    <TableHeader className={cn(className)}>
      {children}
    </TableHeader>
  )
}

function AdminTableBody({ children, className }: AdminTableBodyProps) {
  return (
    <TableBody className={cn(className)}>
      {children}
    </TableBody>
  )
}

function AdminTableRow({ children, className, onClick }: AdminTableRowProps) {
  return (
    <TableRow 
      className={cn(
        onClick && "cursor-pointer",
        className
      )}
      onClick={onClick}
    >
      {children}
    </TableRow>
  )
}

function AdminTableCell({ children, className, header }: AdminTableCellProps) {
  if (header) {
    return (
      <TableHead className={cn(className)}>
        {children}
      </TableHead>
    )
  }

  return (
    <TableCell className={cn(className)}>
      {children}
    </TableCell>
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
              <div className="h-4 bg-muted rounded animate-pulse" />
            </AdminTableCell>
          ))}
        </AdminTableRow>
      </AdminTableHeader>
      <AdminTableBody>
        {Array.from({ length: rows }).map((_, rowIndex) => (
          <AdminTableRow key={rowIndex}>
            {Array.from({ length: columns }).map((_, colIndex) => (
              <AdminTableCell key={colIndex}>
                <div className="h-4 bg-muted rounded animate-pulse" />
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
              <p className="text-muted-foreground text-sm">{message}</p>
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