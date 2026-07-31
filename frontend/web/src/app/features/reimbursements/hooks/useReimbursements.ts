import { useCallback, useEffect, useState } from 'react'
import type { Reimbursement, ReimbursementListFilters } from '../api'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { AppError } from '@/app/shared/models/appError'
import { reimbursementRepository } from '../api'

interface UseReimbursementsResult {
  result: PagedResult<Reimbursement> | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useReimbursements(filters: ReimbursementListFilters = {}): UseReimbursementsResult {
  const [result, setResult] = useState<PagedResult<Reimbursement> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const { page, pageSize, employeeId, status } = filters

  useEffect(() => {
    let cancelled = false
    reimbursementRepository
      .list({ page, pageSize, employeeId, status })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load reimbursements.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, pageSize, employeeId, status, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { result, isLoading, error, refresh }
}
