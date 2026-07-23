import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import { authRepository } from '@/app/features/auth/api'
import type {
  Announcement,
  AnnouncementListFilters,
  AnnouncementRepository,
  CreateAnnouncementInput,
} from './announcementRepository'
import { mockAnnouncements, type MockAnnouncementRecord } from './mockData'

const announcements = [...mockAnnouncements]

function nextId(): string {
  return `40000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

function isExpired(record: MockAnnouncementRecord): boolean {
  return Boolean(record.expiresAtUtc) && new Date(record.expiresAtUtc!).getTime() <= Date.now()
}

/** Mirrors the visibility rule described in docs/api-specification.md §19.2. Department-scoped
 * visibility can't be fully evaluated in mock mode (no employee-department lookup for the
 * current mock user), so it falls back to visible only for Admin/HR, who manage announcements. */
function isVisible(record: MockAnnouncementRecord, role: string): boolean {
  if (record.isRetracted || isExpired(record)) return false
  if (record.audienceType === 'All') return true
  if (record.audienceType === 'Role') return record.targetRole === role
  return role === 'Admin' || role === 'HR'
}

function toDto(record: MockAnnouncementRecord, userId: string): Announcement {
  return {
    id: record.id,
    title: record.title,
    message: record.message,
    priority: record.priority,
    audienceType: record.audienceType,
    departmentId: record.departmentId,
    targetRole: record.targetRole,
    createdByUserId: record.createdByUserId,
    createdAtUtc: record.createdAtUtc,
    expiresAtUtc: record.expiresAtUtc,
    isReadByMe: record.readByUserIds.includes(userId),
  }
}

export const mockAnnouncementRepository: AnnouncementRepository = {
  async list(filters: AnnouncementListFilters = {}): Promise<Announcement[]> {
    await delay(300)
    const user = await authRepository.getCurrentUser()
    const role = user.role ?? 'Employee'
    const { page = 1, pageSize = 20, onlyUnread = false } = filters

    let visible = announcements.filter((a) => isVisible(a, role))
    if (onlyUnread) {
      visible = visible.filter((a) => !a.readByUserIds.includes(user.id))
    }
    visible = [...visible].sort((a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime())

    const start = (page - 1) * pageSize
    return visible.slice(start, start + pageSize).map((a) => toDto(a, user.id))
  },

  async getById(id: string): Promise<Announcement> {
    await delay(200)
    const user = await authRepository.getCurrentUser()
    const record = announcements.find((a) => a.id === id)
    if (!record || !isVisible(record, user.role ?? 'Employee')) {
      throw new AppError('Announcement was not found.', 404, 'NOT_FOUND')
    }
    return toDto(record, user.id)
  },

  async create(input: CreateAnnouncementInput): Promise<{ id: string }> {
    await delay(300)
    const user = await authRepository.getCurrentUser()

    if (input.audienceType === 'Department' && !input.departmentId) {
      throw new AppError('DepartmentId is required when AudienceType is Department.', 400, 'VALIDATION_ERROR')
    }
    if (input.audienceType === 'Role' && !input.targetRole) {
      throw new AppError('TargetRole is required when AudienceType is Role.', 400, 'VALIDATION_ERROR')
    }
    if (input.expiresAtUtc && new Date(input.expiresAtUtc).getTime() <= Date.now()) {
      throw new AppError('ExpiresAtUtc must be in the future.', 400, 'VALIDATION_ERROR')
    }

    const record: MockAnnouncementRecord = {
      id: nextId(),
      title: input.title,
      message: input.message,
      priority: input.priority,
      audienceType: input.audienceType,
      departmentId: input.audienceType === 'Department' ? (input.departmentId ?? null) : null,
      targetRole: input.audienceType === 'Role' ? (input.targetRole ?? null) : null,
      createdByUserId: user.id,
      createdAtUtc: new Date().toISOString(),
      expiresAtUtc: input.expiresAtUtc ?? null,
      isRetracted: false,
      readByUserIds: [],
    }
    announcements.unshift(record)
    return { id: record.id }
  },

  async markRead(id: string): Promise<void> {
    await delay(150)
    const user = await authRepository.getCurrentUser()
    const record = announcements.find((a) => a.id === id)
    if (!record) {
      throw new AppError('Announcement was not found.', 404, 'NOT_FOUND')
    }
    if (!record.readByUserIds.includes(user.id)) {
      record.readByUserIds.push(user.id)
    }
  },

  async remove(id: string): Promise<void> {
    await delay(200)
    const record = announcements.find((a) => a.id === id)
    if (!record) {
      throw new AppError('Announcement was not found.', 404, 'NOT_FOUND')
    }
    record.isRetracted = true
  },
}
