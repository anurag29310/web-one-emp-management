import { useEffect, useState } from 'react'
import { AppError } from '@/app/shared/models/appError'
import type { EmployeeReport } from '../api'
import { reportsRepository } from '../api'

interface UseEmployeeReportResult {
  report: EmployeeReport | null
  isLoading: boolean
  error: string | null
}

export function useEmployeeReport(): UseEmployeeReportResult {
  const [report, setReport] = useState<EmployeeReport | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    reportsRepository
      .getEmployeeReport()
      .then((data) => {
        if (!cancelled) setReport(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load the employee report.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  return { report, isLoading, error }
}
