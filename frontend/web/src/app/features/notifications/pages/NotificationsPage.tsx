import { useAuth } from '@/app/core/auth/useAuth'
import { useNotifications } from '../hooks/useNotifications'

export function NotificationsPage() {
  const { user } = useAuth()
  const { notifications, unreadCount, isLoading, error, markRead } = useNotifications(user?.id)

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Notifications</h1>
        <p className="text-sm text-ink-subtle">
          {notifications.length} notifications{unreadCount > 0 ? ` · ${unreadCount} unread` : ''}
        </p>
      </div>

      {error && (
        <p role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        {isLoading &&
          Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="border-b border-hairline p-4 last:border-b-0">
              <div className="h-5 animate-pulse rounded bg-surface-2" />
            </div>
          ))}

        {!isLoading && notifications.length === 0 && (
          <p className="p-8 text-center text-sm text-ink-subtle">No notifications yet.</p>
        )}

        {!isLoading &&
          notifications.map((notification) => (
            <div
              key={notification.id}
              className={`flex items-start justify-between gap-4 border-b border-hairline p-4 last:border-b-0 ${
                notification.isRead ? '' : 'bg-primary/5'
              }`}
            >
              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-2">
                  <p className="text-sm font-medium text-ink">{notification.title}</p>
                  {!notification.isRead && (
                    <span className="rounded-full bg-primary/15 px-2 py-0.5 text-[10px] font-medium text-primary-hover ring-1 ring-inset ring-primary/30">
                      New
                    </span>
                  )}
                </div>
                <p className="mt-1 text-sm text-ink-muted">{notification.message}</p>
                <p className="mt-1 text-xs text-ink-subtle">
                  {new Date(notification.createdAtUtc).toLocaleString()} · {notification.channel}
                </p>
              </div>
              {!notification.isRead && (
                <button
                  type="button"
                  onClick={() => void markRead(notification.id)}
                  className="shrink-0 text-xs font-medium text-primary-hover hover:underline"
                >
                  Mark as read
                </button>
              )}
            </div>
          ))}
      </div>
    </div>
  )
}
