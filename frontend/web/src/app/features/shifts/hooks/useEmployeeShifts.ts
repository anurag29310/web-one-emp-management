import { useCallback, useEffect, useState } from 'react'
import type { EmployeeShift } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { shiftRepository } from '../api'

interface UseEmployeeShiftsResult {
  assignments: EmployeeShift[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

/** GET /employees/{employeeId}/shifts — Admin/HR, Manager for their team, or the employee themselves. */
export function useEmployeeShifts(employeeId: string | undefined): UseEmployeeShiftsResult {
  const [assignments, setAssignments] = useState<EmployeeShift[]>([])
  const [isLoading, setIsLoading] = useState(Boolean(employeeId))
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!employeeId) return
    let cancelled = false
    shiftRepository
      .getEmployeeShifts(employeeId)
      .then((data) => {
        if (!cancelled) setAssignments(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load shift assignments.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [employeeId, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { assignments, isLoading, error, refresh }
}
