import { useCallback, useEffect, useState } from 'react'
import type { OfficeLocation } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { officeLocationRepository } from '../api'

interface UseOfficeLocationResult {
  officeLocation: OfficeLocation | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useOfficeLocation(id: string | undefined): UseOfficeLocationResult {
  const [officeLocation, setOfficeLocation] = useState<OfficeLocation | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    officeLocationRepository
      .getById(id)
      .then((data) => {
        if (!cancelled) setOfficeLocation(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load office location.')
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

  return { officeLocation, isLoading, error, refresh }
}
