import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '@/app/core/auth/useAuth'
import { useNotifications } from '../hooks/useNotifications'

function timeAgo(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime()
  const diffMin = Math.round(diffMs / 60_000)
  if (diffMin < 1) return 'just now'
  if (diffMin < 60) return `${diffMin}m ago`
  const diffHr = Math.round(diffMin / 60)
  if (diffHr < 24) return `${diffHr}h ago`
  const diffDay = Math.round(diffHr / 24)
  return `${diffDay}d ago`
}

export function NotificationBell() {
  const { user } = useAuth()
  const { notifications, unreadCount, isLoading, error, markRead } = useNotifications(user?.id)
  const [isOpen, setIsOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!isOpen) return

    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') setIsOpen(false)
    }
    document.addEventListener('mousedown', handleClickOutside)
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [isOpen])

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((open) => !open)}
        aria-label={`Notifications${unreadCount > 0 ? ` (${unreadCount} unread)` : ''}`}
        aria-expanded={isOpen}
        className="relative rounded-md p-2 text-ink-subtle transition hover:bg-surface-2 hover:text-ink"
      >
        <svg viewBox="0 0 24 24" fill="none" strokeWidth={1.5} stroke="currentColor" className="h-5 w-5">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M14.857 17.082a23.848 23.848 0 0 0 5.454-1.31A8.967 8.967 0 0 1 18 9.75V9A6 6 0 0 0 6 9v.75a8.967 8.967 0 0 1-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 0 1-5.714 0m5.714 0a3 3 0 1 1-5.714 0"
          />
        </svg>
        {unreadCount > 0 && (
          <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-danger px-1 text-[10px] font-semibold text-white">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div role="menu" className="absolute right-0 z-40 mt-2 w-80 rounded-lg border border-hairline bg-surface-1 shadow-2xl">
          <div className="flex items-center justify-between border-b border-hairline px-4 py-3">
            <p className="text-sm font-medium text-ink">Notifications</p>
            {unreadCount > 0 && <span className="text-xs text-ink-subtle">{unreadCount} unread</span>}
          </div>

          <div className="max-h-96 overflow-y-auto">
            {isLoading && <p className="px-4 py-6 text-center text-sm text-ink-subtle">Loading…</p>}
            {!isLoading && error && <p className="px-4 py-6 text-center text-sm text-danger">{error}</p>}
            {!isLoading && !error && notifications.length === 0 && (
              <p className="px-4 py-6 text-center text-sm text-ink-subtle">No notifications yet.</p>
            )}
            {!isLoading &&
              !error &&
              notifications.slice(0, 8).map((notification) => (
                <button
                  key={notification.id}
                  type="button"
                  onClick={() => void markRead(notification.id)}
                  disabled={notification.isRead}
                  className={`block w-full border-b border-hairline px-4 py-3 text-left transition last:border-b-0 hover:bg-surface-2 disabled:cursor-default ${
                    notification.isRead ? '' : 'bg-primary/5'
                  }`}
                >
                  <div className="flex items-start justify-between gap-2">
                    <p className="text-sm font-medium text-ink">{notification.title}</p>
                    {!notification.isRead && <span className="mt-1 h-2 w-2 shrink-0 rounded-full bg-primary" />}
                  </div>
                  <p className="mt-0.5 line-clamp-2 text-xs text-ink-subtle">{notification.message}</p>
                  <p className="mt-1 text-[11px] text-ink-tertiary">{timeAgo(notification.createdAtUtc)}</p>
                </button>
              ))}
          </div>

          <Link
            to="/notifications"
            onClick={() => setIsOpen(false)}
            className="block border-t border-hairline px-4 py-2.5 text-center text-xs font-medium text-primary-hover hover:underline"
          >
            View all
          </Link>
        </div>
      )}
    </div>
  )
}
