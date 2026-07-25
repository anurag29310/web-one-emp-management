import { useCallback, useEffect, useState } from 'react'
import type { OfficeLocation } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { officeLocationRepository } from '../api'

interface UseOfficeLocationsResult {
  officeLocations: OfficeLocation[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useOfficeLocations(): UseOfficeLocationsResult {
  const [officeLocations, setOfficeLocations] = useState<OfficeLocation[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    let cancelled = false
    officeLocationRepository
      .list()
      .then((data) => {
        if (!cancelled) setOfficeLocations(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load office locations.')
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

  return { officeLocations, isLoading, error, refresh }
}
