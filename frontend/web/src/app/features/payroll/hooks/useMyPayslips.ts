import { useCallback, useEffect, useState } from 'react'
import type { Payslip } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { payrollRepository } from '../api'

interface UseMyPayslipsResult {
  payslips: Payslip[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

/**
 * Lists payslips for the current caller. Deliberately never passes an
 * employeeId — the backend forces self-scoping for non-privileged callers,
 * and privileged (Admin/HR) callers viewing their own payslips is out of
 * scope for this self-service view.
 */
export function useMyPayslips(): UseMyPayslipsResult {
  const [payslips, setPayslips] = useState<Payslip[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    let cancelled = false
    payrollRepository
      .listPayslips()
      .then((data) => {
        if (!cancelled) setPayslips(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load payslips.')
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

  return { payslips, isLoading, error, refresh }
}
