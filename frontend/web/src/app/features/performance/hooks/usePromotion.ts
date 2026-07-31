import { useCallback, useEffect, useState } from 'react'
import type { Promotion } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { performanceRepository } from '../api'

interface UsePromotionResult {
  promotion: Promotion | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function usePromotion(id: string | undefined): UsePromotionResult {
  const [promotion, setPromotion] = useState<Promotion | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    performanceRepository
      .getPromotionById(id)
      .then((data) => {
        if (!cancelled) setPromotion(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof AppError ? err.message : 'Failed to load promotion.')
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [id, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { promotion, isLoading, error, refresh }
}
