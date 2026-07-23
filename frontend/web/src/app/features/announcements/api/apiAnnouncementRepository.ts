import { httpClient } from '@/app/core/api/httpClient'
import type {
  Announcement,
  AnnouncementListFilters,
  AnnouncementRepository,
  CreateAnnouncementInput,
} from './announcementRepository'

// AnnouncementsController.cs returns raw JSON bodies (a bare array from GetAll, a bare Guid from
// Create) rather than the app's usual `{ data, message, correlationId }` envelope — do not route
// these calls through `unwrap()`.

export const apiAnnouncementRepository: AnnouncementRepository = {
  async list(filters: AnnouncementListFilters = {}): Promise<Announcement[]> {
    const response = await httpClient.get<Announcement[]>('/announcements', { params: filters })
    return response.data
  },

  async getById(id: string): Promise<Announcement> {
    const response = await httpClient.get<Announcement>(`/announcements/${id}`)
    return response.data
  },

  async create(input: CreateAnnouncementInput): Promise<{ id: string }> {
    const response = await httpClient.post<string>('/announcements', input)
    return { id: response.data }
  },

  async markRead(id: string): Promise<void> {
    await httpClient.post(`/announcements/${id}/mark-read`)
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/announcements/${id}`)
  },
}
