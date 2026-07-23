import { useCallback, useEffect, useState } from 'react'
import type { LeaveType } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { leaveTypeRepository } from '../api'

interface UseLeaveTypesResult {
  leaveTypes: LeaveType[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useLeaveTypes(): UseLeaveTypesResult {
  const [leaveTypes, setLeaveTypes] = useState<LeaveType[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    let cancelled = false
    leaveTypeRepository
      .list()
      .then((data) => {
        if (!cancelled) setLeaveTypes(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load leave types.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { leaveTypes, isLoading, error, refresh }
}
