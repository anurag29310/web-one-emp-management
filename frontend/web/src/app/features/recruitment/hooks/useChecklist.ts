import { useCallback, useEffect, useState } from 'react'
import type { ChecklistItem } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { recruitmentRepository } from '../api'

interface UseChecklistResult {
  items: ChecklistItem[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useChecklist(candidateId: string | undefined): UseChecklistResult {
  const [items, setItems] = useState<ChecklistItem[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!candidateId) return
    let cancelled = false
    recruitmentRepository
      .getChecklist(candidateId)
      .then((data) => {
        if (!cancelled) setItems(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof AppError ? err.message : 'Failed to load the onboarding checklist.')
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

  return { items, isLoading, error, refresh }
}
