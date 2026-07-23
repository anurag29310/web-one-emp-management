import { useCallback, useEffect, useState } from 'react'
import type { PayrollRun } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { payrollRepository } from '../api'

interface UsePayrollRunsResult {
  runs: PayrollRun[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function usePayrollRuns(): UsePayrollRunsResult {
  const [runs, setRuns] = useState<PayrollRun[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    let cancelled = false
    payrollRepository
      .listRuns()
      .then((data) => {
        if (!cancelled) setRuns(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load payroll runs.')
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

  return { runs, isLoading, error, refresh }
}
