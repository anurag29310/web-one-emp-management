import { useCallback, useEffect, useState } from 'react'
import type { Announcement } from '../api'
import { AppError } from '@/app/shared/models/appError'
import { announcementRepository } from '../api'

interface UseAnnouncementsResult {
  announcements: Announcement[]
  isLoading: boolean
  error: string | null
  refresh: () => void
}

// architecture.md §8: delivery is poll-based (no SignalR/push infra), so the list re-fetches on
// this interval in addition to on mount and after user actions.
const POLL_INTERVAL_MS = 60_000

export function useAnnouncements(onlyUnread = false): UseAnnouncementsResult {
  const [announcements, setAnnouncements] = useState<Announcement[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    let cancelled = false

    function load(showSpinner: boolean) {
      if (showSpinner) setIsLoading(true)
      announcementRepository
        .list({ onlyUnread })
        .then((data) => {
          if (!cancelled) setAnnouncements(data)
        })
        .catch((err: unknown) => {
          if (!cancelled) {
            setError(err instanceof AppError ? err.message : 'Failed to load announcements.')
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
  }, [onlyUnread, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { announcements, isLoading, error, refresh }
}
