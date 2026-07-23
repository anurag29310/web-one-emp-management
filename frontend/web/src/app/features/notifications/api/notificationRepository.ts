export type NotificationChannel = 'InApp' | 'Email'

export interface Notification {
  id: string
  userId: string | null
  title: string
  message: string
  channel: NotificationChannel
  isRead: boolean
  createdAtUtc: string
  expiresAtUtc: string | null
}

export interface NotificationListFilters {
  page?: number
  pageSize?: number
  onlyUnread?: boolean
}

export interface NotificationRepository {
  /** Mirrors GET /notifications/user/{userId} — the backend 403s a non-Admin caller who
   * requests another user's notifications. */
  listForUser(userId: string, filters?: NotificationListFilters): Promise<Notification[]>
  markRead(id: string): Promise<void>
}
