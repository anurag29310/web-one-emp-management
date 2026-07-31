import { useCallback, useEffect, useState } from 'react'
import type { Reimbursement } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { reimbursementRepository } from '../api'

interface UseReimbursementResult {
  reimbursement: Reimbursement | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useReimbursement(id: string | undefined): UseReimbursementResult {
  const [reimbursement, setReimbursement] = useState<Reimbursement | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    reimbursementRepository
      .getById(id)
      .then((data) => {
        if (!cancelled) setReimbursement(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load reimbursement.')
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

  return { reimbursement, isLoading, error, refresh }
}
