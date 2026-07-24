export type AnnouncementPriority = 'Normal' | 'Important' | 'Critical'
export type AnnouncementAudienceType = 'All' | 'Department' | 'Role'

export interface Announcement {
  id: string
  title: string
  message: string
  priority: AnnouncementPriority
  audienceType: AnnouncementAudienceType
  departmentId: string | null
  targetRole: string | null
  createdByUserId: string
  createdAtUtc: string
  expiresAtUtc: string | null
  isReadByMe: boolean
}

export interface AnnouncementListFilters {
  page?: number
  pageSize?: number
  onlyUnread?: boolean
}

export interface CreateAnnouncementInput {
  title: string
  message: string
  priority: AnnouncementPriority
  audienceType: AnnouncementAudienceType
  departmentId?: string | null
  targetRole?: string | null
  expiresAtUtc?: string | null
}

export interface AnnouncementRepository {
  /**
   * The caller's visible list is always filtered server-side from their id/role claim — there is
   * no client-suppliable userId/departmentId filter, matching AnnouncementsController.GetAll.
   */
  list(filters?: AnnouncementListFilters): Promise<Announcement[]>
  getById(id: string): Promise<Announcement>
  create(input: CreateAnnouncementInput): Promise<{ id: string }>
  markRead(id: string): Promise<void>
  remove(id: string): Promise<void>
}
