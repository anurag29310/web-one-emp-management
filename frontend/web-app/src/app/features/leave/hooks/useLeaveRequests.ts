import { useCallback, useEffect, useState } from 'react'
import type { LeaveListFilters, LeaveRequest } from '../api'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { AppError } from '@/app/shared/models/appError'
import { leaveRepository } from '../api'

interface UseLeaveRequestsResult {
  result: PagedResult<LeaveRequest> | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useLeaveRequests(filters: LeaveListFilters = {}): UseLeaveRequestsResult {
  const [result, setResult] = useState<PagedResult<LeaveRequest> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const { page, pageSize, employeeId, leaveTypeId, year, status } = filters

  useEffect(() => {
    let cancelled = false
    leaveRepository
      .list({ page, pageSize, employeeId, leaveTypeId, year, status })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load leave requests.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, pageSize, employeeId, leaveTypeId, year, status, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { result, isLoading, error, refresh }
}
