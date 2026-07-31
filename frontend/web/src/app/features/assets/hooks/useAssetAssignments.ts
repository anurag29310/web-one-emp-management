import { useCallback, useEffect, useState } from 'react'
import type { AssetAssignment } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { assetRepository } from '../api'

interface UseAssetAssignmentsResult {
  assignments: AssetAssignment[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useAssetAssignments(assetId: string | undefined): UseAssetAssignmentsResult {
  const [assignments, setAssignments] = useState<AssetAssignment[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!assetId) return
    let cancelled = false
    assetRepository
      .getAssignments(assetId)
      .then((data) => {
        if (!cancelled) setAssignments(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load assignment history.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [assetId, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { assignments, isLoading, error, refresh }
}
