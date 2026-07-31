import { useCallback, useEffect, useState } from 'react'
import type { Asset, AssetListFilters } from '../api'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { AppError } from '@/app/shared/models/appError'
import { assetRepository } from '../api'

interface UseAssetsResult {
  result: PagedResult<Asset> | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useAssets(filters: AssetListFilters = {}): UseAssetsResult {
  const [result, setResult] = useState<PagedResult<Asset> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const { page, pageSize, status, category, search } = filters

  useEffect(() => {
    let cancelled = false
    assetRepository
      .list({ page, pageSize, status, category, search })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load assets.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, pageSize, status, category, search, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { result, isLoading, error, refresh }
}
