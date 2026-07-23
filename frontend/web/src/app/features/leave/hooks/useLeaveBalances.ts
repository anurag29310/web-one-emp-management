import { useCallback, useEffect, useState } from 'react'
import type { LeaveBalance } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { leaveRepository } from '../api'

interface UseLeaveBalancesResult {
  balances: LeaveBalance[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useLeaveBalances(employeeId: string | undefined): UseLeaveBalancesResult {
  const [balances, setBalances] = useState<LeaveBalance[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!employeeId) return
    let cancelled = false
    leaveRepository
      .getBalances(employeeId)
      .then((data) => {
        if (!cancelled) setBalances(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load leave balances.')
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

  return { balances, isLoading, error, refresh }
}
