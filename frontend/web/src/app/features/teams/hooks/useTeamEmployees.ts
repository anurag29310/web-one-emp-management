import { useEffect, useState } from 'react'
import type { Employee } from '@/app/shared/models/employee'
import { AppError } from '@/app/shared/models/appError'
import { teamRepository } from '../api'

interface UseTeamEmployeesResult {
  employees: Employee[]
  isLoading: boolean
  error: string | null
}

export function useTeamEmployees(teamId: string | undefined): UseTeamEmployeesResult {
  const [employees, setEmployees] = useState<Employee[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!teamId) return
    let cancelled = false
    teamRepository
      .listEmployees(teamId)
      .then((data) => {
        if (!cancelled) setEmployees(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load team members.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [teamId])

  return { employees, isLoading, error }
}
