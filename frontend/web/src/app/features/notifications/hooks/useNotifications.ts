import { useCallback, useEffect, useMemo, useState } from 'react'
import type { Notification } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { notificationRepository } from '../api'

interface UseNotificationsResult {
  notifications: Notification[]
  unreadCount: number
  isLoading: boolean
  error: string | null
  refresh: () => void
  markRead: (id: string) => Promise<void>
}

// architecture.md §8: delivery is poll-based (no SignalR/push infra), so the list re-fetches on
// this interval in addition to on mount and after user actions.
const POLL_INTERVAL_MS = 60_000

export function useNotifications(userId: string | undefined): UseNotificationsResult {
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!userId) return
    let cancelled = false

    function load(showSpinner: boolean) {
      if (showSpinner) setIsLoading(true)
      notificationRepository
        .listForUser(userId as string, { pageSize: 50 })
        .then((data) => {
          if (!cancelled) setNotifications(data)
        })
        .catch((err: unknown) => {
          if (!cancelled) {
            setError(err instanceof AppError ? err.message : 'Failed to load notifications.')
          }
        })
        .finally(() => {
          if (!cancelled && showSpinner) setIsLoading(false)
        })
    }

    load(true)
    const interval = setInterval(() => load(false), POLL_INTERVAL_MS)
    return () => {
      cancelled = true
      clearInterval(interval)
    }
  }, [userId, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  const markRead = useCallback(async (id: string) => {
    await notificationRepository.markRead(id)
    setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)))
  }, [])

  const unreadCount = useMemo(() => notifications.filter((n) => !n.isRead).length, [notifications])

  return { notifications, unreadCount, isLoading, error, refresh, markRead }
}
