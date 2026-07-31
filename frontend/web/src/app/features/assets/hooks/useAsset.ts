import { useCallback, useEffect, useState } from 'react'
import type { Asset } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { assetRepository } from '../api'

interface UseAssetResult {
  asset: Asset | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useAsset(id: string | undefined): UseAssetResult {
  const [asset, setAsset] = useState<Asset | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    assetRepository
      .getById(id)
      .then((data) => {
        if (!cancelled) setAsset(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load asset.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [id, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { asset, isLoading, error, refresh }
}
