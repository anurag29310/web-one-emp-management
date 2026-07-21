import { useEffect, useState } from 'react'
import type { Employee } from '@/app/shared/models/employee'
import { AppError } from '@/app/shared/models/appError'
import { employeeRepository } from '../api'

interface UseEmployeeResult {
  employee: Employee | null
  isLoading: boolean
  error: string | null
}

export function useEmployee(id: string | undefined): UseEmployeeResult {
  const [employee, setEmployee] = useState<Employee | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    employeeRepository
      .getById(id)
      .then((data) => {
        if (!cancelled) setEmployee(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load employee profile.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [id])

  return { employee, isLoading, error }
}
