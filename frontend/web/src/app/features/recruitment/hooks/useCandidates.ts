import { useCallback, useEffect, useState } from 'react'
import type { Candidate, CandidateListFilters } from '../api'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { AppError } from '@/app/shared/models/appError'
import { recruitmentRepository } from '../api'

interface UseCandidatesResult {
  result: PagedResult<Candidate> | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useCandidates(filters: CandidateListFilters = {}): UseCandidatesResult {
  const [result, setResult] = useState<PagedResult<Candidate> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const { page, pageSize, status, designationId, search } = filters

  useEffect(() => {
    let cancelled = false
    recruitmentRepository
      .listCandidates({ page, pageSize, status, designationId, search })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load candidates.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, pageSize, status, designationId, search, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { result, isLoading, error, refresh }
}
