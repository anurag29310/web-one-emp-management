import { useCallback, useEffect, useState } from 'react'
import type { Shift } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { shiftRepository } from '../api'

interface UseShiftsResult {
  shifts: Shift[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useShifts(): UseShiftsResult {
  const [shifts, setShifts] = useState<Shift[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    let cancelled = false
    shiftRepository
      .list()
      .then((data) => {
        if (!cancelled) setShifts(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load shifts.')
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

  return { shifts, isLoading, error, refresh }
}
