import { useCallback, useEffect, useState } from 'react'
import type { Message } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { messagingRepository } from '../api'

interface UseMessagesResult {
  messages: Message[]
  totalCount: number
  isLoading: boolean
  error: string | null
  refresh: () => void
}

const THREAD_PAGE_SIZE = 100

/**
 * GET /conversations/{id}/messages returns newest-first pages (api-specification.md §25.4-ish /
 * MessagingController doc comment). For a chat thread we want the usual oldest-at-top reading
 * order, so this hook reverses the single page it fetches rather than exposing pagination —
 * there's no "load older messages" affordance yet, matching the scope of the other list pages
 * in this app that don't do infinite scroll either.
 */
export function useMessages(conversationId: string | undefined): UseMessagesResult {
  const [messages, setMessages] = useState<Message[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!conversationId) return
    let cancelled = false
    messagingRepository
      .listMessages(conversationId, { pageSize: THREAD_PAGE_SIZE })
      .then((data) => {
        if (!cancelled) {
          setMessages([...data.data].reverse())
          setTotalCount(data.totalCount)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof AppError ? err.message : 'Failed to load messages.')
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [conversationId, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { messages, totalCount, isLoading, error, refresh }
}
