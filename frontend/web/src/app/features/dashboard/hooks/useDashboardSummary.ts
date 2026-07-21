import { useEffect, useState } from 'react'
import type { DashboardSummary } from '@/app/shared/models/dashboard'
import { AppError } from '@/app/shared/models/appError'
import { dashboardRepository } from '../api'

interface UseDashboardSummaryResult {
  summary: DashboardSummary | null
  isLoading: boolean
  error: string | null
}

export function useDashboardSummary(): UseDashboardSummaryResult {
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    dashboardRepository
      .getSummary()
      .then((result) => {
        if (!cancelled) setSummary(result)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load dashboard summary.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  return { summary, isLoading, error }
}
