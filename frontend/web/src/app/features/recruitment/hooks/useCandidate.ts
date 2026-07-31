import { useCallback, useEffect, useState } from 'react'
import type { Candidate } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { recruitmentRepository } from '../api'

interface UseCandidateResult {
  candidate: Candidate | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useCandidate(id: string | undefined): UseCandidateResult {
  const [candidate, setCandidate] = useState<Candidate | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    recruitmentRepository
      .getCandidateById(id)
      .then((data) => {
        if (!cancelled) setCandidate(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load candidate.')
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

  return { candidate, isLoading, error, refresh }
}
