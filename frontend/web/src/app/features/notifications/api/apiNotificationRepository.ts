import { httpClient } from '@/app/core/api/httpClient'
import type { Notification, NotificationListFilters, NotificationRepository } from './notificationRepository'

// NotificationsController.cs returns raw JSON bodies (a bare array from GetForUser) rather than
// the app's usual `{ data, message, correlationId }` envelope — do not route these calls through
// `unwrap()`.

export const apiNotificationRepository: NotificationRepository = {
  async listForUser(userId: string, filters: NotificationListFilters = {}): Promise<Notification[]> {
    const response = await httpClient.get<Notification[]>(`/notifications/user/${userId}`, {
      params: filters,
    })
    return response.data
  },

  async markRead(id: string): Promise<void> {
    await httpClient.post(`/notifications/${id}/mark-read`)
  },
}
