import { useCallback, useEffect, useState } from 'react'
import type { PayrollRun } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { payrollRepository } from '../api'

interface UsePayrollRunResult {
  run: PayrollRun | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function usePayrollRun(id: string | undefined): UsePayrollRunResult {
  const [run, setRun] = useState<PayrollRun | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    payrollRepository
      .getRun(id)
      .then((data) => {
        if (!cancelled) setRun(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load payroll run.')
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

  return { run, isLoading, error, refresh }
}
