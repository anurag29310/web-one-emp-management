import { useCallback, useEffect, useState } from 'react'
import { AppError } from '@/app/shared/models/appError'
import type { DepartmentCount } from '../api'
import { reportsRepository } from '../api'

interface UseDepartmentCountsResult {
  counts: DepartmentCount[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useDepartmentCounts(): UseDepartmentCountsResult {
  const [counts, setCounts] = useState<DepartmentCount[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    let cancelled = false
    reportsRepository
      .getDepartmentCounts()
      .then((data) => {
        if (!cancelled) setCounts(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load department counts.')
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

  return { counts, isLoading, error, refresh }
}
