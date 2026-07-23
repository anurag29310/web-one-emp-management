import { useCallback, useEffect, useState } from 'react'
import type { Holiday, HolidayListFilters } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { holidayRepository } from '../api'

interface UseHolidaysResult {
  holidays: Holiday[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useHolidays(filters: HolidayListFilters = {}): UseHolidaysResult {
  const [holidays, setHolidays] = useState<Holiday[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const { officeLocationId, year, isOptional } = filters

  useEffect(() => {
    let cancelled = false
    holidayRepository
      .list({ officeLocationId, year, isOptional })
      .then((data) => {
        if (!cancelled) setHolidays(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load holidays.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [officeLocationId, year, isOptional, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { holidays, isLoading, error, refresh }
}
