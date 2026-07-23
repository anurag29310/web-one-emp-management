import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import { authRepository } from '@/app/features/auth/api'
import type { Notification, NotificationListFilters, NotificationRepository } from './notificationRepository'
import { mockNotifications } from './mockData'

const notifications = [...mockNotifications]

export const mockNotificationRepository: NotificationRepository = {
  async listForUser(userId: string, filters: NotificationListFilters = {}): Promise<Notification[]> {
    await delay(250)
    const current = await authRepository.getCurrentUser()
    if (current.role !== 'Admin' && current.id !== userId) {
      throw new AppError("You do not have permission to view another user's notifications.", 403, 'FORBIDDEN')
    }

    const { page = 1, pageSize = 20, onlyUnread = false } = filters
    let items = notifications.filter((n) => n.userId === userId)
    if (onlyUnread) items = items.filter((n) => !n.isRead)
    items = [...items].sort((a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime())

    const start = (page - 1) * pageSize
    return items.slice(start, start + pageSize)
  },

  async markRead(id: string): Promise<void> {
    await delay(150)
    const record = notifications.find((n) => n.id === id)
    if (!record) {
      throw new AppError('Notification was not found.', 404, 'NOT_FOUND')
    }
    record.isRead = true
  },
}
