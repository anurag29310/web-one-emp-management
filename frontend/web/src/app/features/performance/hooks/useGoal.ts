import { useCallback, useEffect, useState } from 'react'
import type { PerformanceGoal } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { performanceRepository } from '../api'

interface UseGoalResult {
  goal: PerformanceGoal | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useGoal(id: string | undefined): UseGoalResult {
  const [goal, setGoal] = useState<PerformanceGoal | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    performanceRepository
      .getGoalById(id)
      .then((data) => {
        if (!cancelled) setGoal(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof AppError ? err.message : 'Failed to load goal.')
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [id, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { goal, isLoading, error, refresh }
}
