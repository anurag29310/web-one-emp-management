import { useCallback, useEffect, useState } from 'react'
import type { GoalListFilters, PerformanceGoal } from '../api'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { AppError } from '@/app/shared/models/appError'
import { performanceRepository } from '../api'

interface UseGoalsResult {
  result: PagedResult<PerformanceGoal> | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useGoals(filters: GoalListFilters = {}): UseGoalsResult {
  const [result, setResult] = useState<PagedResult<PerformanceGoal> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const { page, pageSize, employeeId, status, category } = filters

  useEffect(() => {
    let cancelled = false
    performanceRepository
      .listGoals({ page, pageSize, employeeId, status, category })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof AppError ? err.message : 'Failed to load goals.')
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, pageSize, employeeId, status, category, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { result, isLoading, error, refresh }
}
