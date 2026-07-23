import { useEffect, useState } from 'react'
import { AppError } from '@/app/shared/models/appError'
import type { DateRangeFilter, EmployeeJoinExit } from '../api'
import { reportsRepository } from '../api'

interface UseEmployeeTurnoverReportResult {
  entries: EmployeeJoinExit[]
  isLoading: boolean
  error: string | null
}

/** Fetches once both `from` and `to` are set — matches the backend's required-range validation. */
export function useEmployeeTurnoverReport(filter: DateRangeFilter): UseEmployeeTurnoverReportResult {
  const [entries, setEntries] = useState<EmployeeJoinExit[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const { from, to } = filter

  useEffect(() => {
    if (!from || !to) {
      return
    }

    let cancelled = false
    reportsRepository
      .getEmployeeTurnover({ from, to })
      .then((data) => {
        if (!cancelled) {
          setEntries(data)
          setError(null)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load the employee turnover report.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [from, to])

  return { entries, isLoading, error }
}
