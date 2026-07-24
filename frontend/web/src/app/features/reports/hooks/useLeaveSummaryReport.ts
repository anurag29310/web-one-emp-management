import { useEffect, useState } from 'react'
import { AppError } from '@/app/shared/models/appError'
import type { DateRangeFilter, LeaveSummaryReport } from '../api'
import { reportsRepository } from '../api'

interface UseLeaveSummaryReportResult {
  report: LeaveSummaryReport | null
  isLoading: boolean
  error: string | null
}

/** Fetches once both `from` and `to` are set — matches the backend's required-range validation. */
export function useLeaveSummaryReport(filter: DateRangeFilter): UseLeaveSummaryReportResult {
  const [report, setReport] = useState<LeaveSummaryReport | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const { from, to } = filter

  useEffect(() => {
    if (!from || !to) {
      return
    }

    let cancelled = false
    reportsRepository
      .getLeaveSummary({ from, to })
      .then((data) => {
        if (!cancelled) {
          setReport(data)
          setError(null)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load the leave summary report.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [from, to])

  return { report, isLoading, error }
}
