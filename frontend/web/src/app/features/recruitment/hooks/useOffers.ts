import { useCallback, useEffect, useState } from 'react'
import type { Offer } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { recruitmentRepository } from '../api'

interface UseOffersResult {
  offers: Offer[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useOffers(candidateId: string | undefined): UseOffersResult {
  const [offers, setOffers] = useState<Offer[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!candidateId) return
    let cancelled = false
    recruitmentRepository
      .getOffers(candidateId)
      .then((data) => {
        if (!cancelled) setOffers(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load offers.')
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

  return { offers, isLoading, error, refresh }
}
