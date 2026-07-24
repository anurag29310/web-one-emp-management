import { selectRepository } from '@/app/core/config/selectRepository'
import { mockNotificationRepository } from './mockNotificationRepository'
import { apiNotificationRepository } from './apiNotificationRepository'
import type { NotificationRepository } from './notificationRepository'

export const notificationRepository: NotificationRepository = selectRepository({
  mock: mockNotificationRepository,
  api: apiNotificationRepository,
})

export type {
  Notification,
  NotificationChannel,
  NotificationListFilters,
  NotificationRepository,
} from './notificationRepository'
