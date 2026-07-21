import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useEmployees } from '../hooks/useEmployees'
import { Avatar } from '@/app/shared/components/Avatar'
import { StatusBadge } from '@/app/shared/components/StatusBadge'

export function EmployeeListPage() {
  const [search, setSearch] = useState('')
  const [searchParams, setSearchParams] = useSearchParams()
  const departmentId = searchParams.get('departmentId') ?? undefined

  const { result, isLoading, error } = useEmployees({ search: search || undefined, departmentId })

  function clearDepartmentFilter() {
    setSearchParams((params) => {
      params.delete('departmentId')
      return params
    })
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Employees</h1>
          <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
        </div>
        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search by name or code…"
          className="w-64 rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        />
      </div>

      {departmentId && (
        <div className="flex items-center gap-2 text-sm text-ink-subtle">
          <span>Filtered by department</span>
          <button
            type="button"
            onClick={clearDepartmentFilter}
            className="rounded-full bg-surface-2 px-2.5 py-0.5 text-xs text-ink-muted hover:text-ink"
          >
            Clear ×
          </button>
        </div>
      )}

      {error && <p className="text-sm text-danger">{error}</p>}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Employee</th>
              <th className="px-4 py-3">Code</th>
              <th className="px-4 py-3">Department</th>
              <th className="px-4 py-3">Designation</th>
              <th className="px-4 py-3">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 4 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={5}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && result?.data.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={5}>
                  No employees match your search.
                </td>
              </tr>
            )}

            {!isLoading &&
              result?.data.map((employee) => (
                <tr key={employee.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link to={`/employees/${employee.id}`} className="flex items-center gap-3">
                      <Avatar name={employee.fullName} size="sm" />
                      <span className="font-medium text-ink hover:text-primary-hover">{employee.fullName}</span>
                    </Link>
                  </td>
                  <td className="px-4 py-3 font-mono text-ink-subtle">{employee.employeeCode}</td>
                  <td className="px-4 py-3 text-ink-muted">{employee.departmentName ?? '—'}</td>
                  <td className="px-4 py-3 text-ink-muted">{employee.designation ?? '—'}</td>
                  <td className="px-4 py-3">
                    <StatusBadge status={employee.employmentStatus ?? 'Inactive'} />
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
