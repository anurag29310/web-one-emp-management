import { useCallback, useEffect, useState } from 'react'
import type { Designation } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { designationRepository } from '../api'

interface UseDesignationResult {
  designation: Designation | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useDesignation(id: string | undefined): UseDesignationResult {
  const [designation, setDesignation] = useState<Designation | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    designationRepository
      .getById(id)
      .then((data) => {
        if (!cancelled) setDesignation(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load designation.')
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

  return { designation, isLoading, error, refresh }
}
