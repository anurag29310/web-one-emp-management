import { useCallback, useEffect, useState } from 'react'
import { messagingRepository } from '../api'

// architecture.md §8: delivery is poll-based (no SignalR/push infra), matching the same interval
// already used by useNotifications for its unread badge.
const POLL_INTERVAL_MS = 60_000

export function useUnreadConversationCount(enabled = true): { unreadCount: number; refresh: () => void } {
  const [unreadCount, setUnreadCount] = useState(0)
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    if (!enabled) return
    let cancelled = false

    function load() {
      messagingRepository
        .getUnreadConversationCount()
        .then((count) => {
          if (!cancelled) setUnreadCount(count)
        })
        .catch(() => {
          // Non-critical UI badge — a failed poll just leaves the last known count in place.
        })
    }

    load()
    const interval = setInterval(load, POLL_INTERVAL_MS)
    return () => {
      cancelled = true
      clearInterval(interval)
    }
  }, [enabled, refreshToken])

  const refresh = useCallback(() => setRefreshToken((t) => t + 1), [])

  return { unreadCount, refresh }
}
