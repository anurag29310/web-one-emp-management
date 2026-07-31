import { useCallback, useEffect, useState } from 'react'
import type { Interview } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { recruitmentRepository } from '../api'

interface UseInterviewsResult {
  interviews: Interview[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useInterviews(candidateId: string | undefined): UseInterviewsResult {
  const [interviews, setInterviews] = useState<Interview[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!candidateId) return
    let cancelled = false
    recruitmentRepository
      .getInterviews(candidateId)
      .then((data) => {
        if (!cancelled) setInterviews(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load interviews.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [candidateId, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { interviews, isLoading, error, refresh }
}
