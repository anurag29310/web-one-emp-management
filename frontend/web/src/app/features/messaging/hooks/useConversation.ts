import { useCallback, useEffect, useState } from 'react'
import type { Conversation } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { messagingRepository } from '../api'

interface UseConversationResult {
  conversation: Conversation | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useConversation(id: string | undefined): UseConversationResult {
  const [conversation, setConversation] = useState<Conversation | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    messagingRepository
      .getConversationById(id)
      .then((data) => {
        if (!cancelled) setConversation(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof AppError ? err.message : 'Failed to load conversation.')
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [id, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { conversation, isLoading, error, refresh }
}
