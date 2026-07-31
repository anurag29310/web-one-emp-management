import { useCallback, useEffect, useState } from 'react'
import type { PerformanceReview } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { performanceRepository } from '../api'

interface UseReviewResult {
  review: PerformanceReview | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useReview(id: string | undefined): UseReviewResult {
  const [review, setReview] = useState<PerformanceReview | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    performanceRepository
      .getReviewById(id)
      .then((data) => {
        if (!cancelled) setReview(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof AppError ? err.message : 'Failed to load review.')
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [id, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { review, isLoading, error, refresh }
}
