import { useCallback, useEffect, useState } from 'react'
import type { Promotion, PromotionListFilters } from '../api'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { AppError } from '@/app/shared/models/appError'
import { performanceRepository } from '../api'

interface UsePromotionsResult {
  result: PagedResult<Promotion> | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function usePromotions(filters: PromotionListFilters = {}): UsePromotionsResult {
  const [result, setResult] = useState<PagedResult<Promotion> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const { page, pageSize, employeeId, status } = filters

  useEffect(() => {
    let cancelled = false
    performanceRepository
      .listPromotions({ page, pageSize, employeeId, status })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof AppError ? err.message : 'Failed to load promotions.')
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, pageSize, employeeId, status, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { result, isLoading, error, refresh }
}
