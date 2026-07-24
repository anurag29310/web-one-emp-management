import { selectRepository } from '@/app/core/config/selectRepository'
import { mockAnnouncementRepository } from './mockAnnouncementRepository'
import { apiAnnouncementRepository } from './apiAnnouncementRepository'
import type { AnnouncementRepository } from './announcementRepository'

export const announcementRepository: AnnouncementRepository = selectRepository({
  mock: mockAnnouncementRepository,
  api: apiAnnouncementRepository,
})

export type {
  Announcement,
  AnnouncementAudienceType,
  AnnouncementListFilters,
  AnnouncementPriority,
  AnnouncementRepository,
  CreateAnnouncementInput,
} from './announcementRepository'
