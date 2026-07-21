import { useEffect, useState } from 'react'
import type { Employee, EmployeeListFilters } from '@/app/shared/models/employee'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { AppError } from '@/app/shared/models/appError'
import { employeeRepository } from '../api'

interface UseEmployeesResult {
  result: PagedResult<Employee> | null
  isLoading: boolean
  error: string | null
}

export function useEmployees(filters: EmployeeListFilters = {}): UseEmployeesResult {
  const [result, setResult] = useState<PagedResult<Employee> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const { page, pageSize, search, departmentId, status } = filters

  useEffect(() => {
    let cancelled = false
    employeeRepository
      .list({ page, pageSize, search, departmentId, status })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load employees.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, pageSize, search, departmentId, status])

  return { result, isLoading, error }
}
