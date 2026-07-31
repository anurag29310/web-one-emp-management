import { useCallback, useEffect, useState } from 'react'
import type { Conversation, ConversationListFilters } from '../api'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { AppError } from '@/app/shared/models/appError'
import { messagingRepository } from '../api'

interface UseConversationsResult {
  result: PagedResult<Conversation> | null
  isLoading: boolean
  error: string | null
  refresh: () => void
}

export function useConversations(filters: ConversationListFilters = {}): UseConversationsResult {
  const [result, setResult] = useState<PagedResult<Conversation> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const { page, pageSize, search } = filters

  useEffect(() => {
    let cancelled = false
    messagingRepository
      .listConversations({ page, pageSize, search })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof AppError ? err.message : 'Failed to load conversations.')
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, pageSize, search, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { result, isLoading, error, refresh }
}
