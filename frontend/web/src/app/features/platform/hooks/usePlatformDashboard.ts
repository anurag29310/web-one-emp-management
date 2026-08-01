import { useCallback, useEffect, useState } from 'react'
import type { PlatformDashboardSummary } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { platformDashboardRepository } from '../api'

interface UsePlatformDashboardResult {
  summary: PlatformDashboardSummary | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function usePlatformDashboard(): UsePlatformDashboardResult {
  const [summary, setSummary] = useState<PlatformDashboardSummary | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    let cancelled = false
    platformDashboardRepository
      .getSummary()
      .then((data) => {
        if (!cancelled) setSummary(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load the platform dashboard.')
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

  return { summary, isLoading, error, refresh }
}
