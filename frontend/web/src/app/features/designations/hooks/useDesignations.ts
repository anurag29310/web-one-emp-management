import { useCallback, useEffect, useState } from 'react'
import type { Designation } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { designationRepository } from '../api'

interface UseDesignationsResult {
  designations: Designation[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useDesignations(): UseDesignationsResult {
  const [designations, setDesignations] = useState<Designation[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    let cancelled = false
    designationRepository
      .list()
      .then((data) => {
        if (!cancelled) setDesignations(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load designations.')
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

  return { designations, isLoading, error, refresh }
}
