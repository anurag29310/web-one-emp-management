import { useCallback, useEffect, useState } from 'react'
import type { LeaveRequest } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { leaveRepository } from '../api'

interface UseLeaveRequestResult {
  leaveRequest: LeaveRequest | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useLeaveRequest(id: string | undefined): UseLeaveRequestResult {
  const [leaveRequest, setLeaveRequest] = useState<LeaveRequest | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    leaveRepository
      .getById(id)
      .then((data) => {
        if (!cancelled) setLeaveRequest(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load leave request.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [id, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { leaveRequest, isLoading, error, refresh }
}
