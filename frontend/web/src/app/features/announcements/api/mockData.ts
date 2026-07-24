import type { AnnouncementAudienceType, AnnouncementPriority } from './announcementRepository'

export interface MockAnnouncementRecord {
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
  isRetracted: boolean
  /** User ids (see frontend/web/src/app/features/auth/api/mockData.ts) who have read this. */
  readByUserIds: string[]
}

// createdByUserId '00000000-0000-0000-0000-000000000001' is the seeded Admin account in
// frontend/web/src/app/features/auth/api/mockData.ts.
export const mockAnnouncements: MockAnnouncementRecord[] = [
  {
    id: '40000000-0000-0000-0000-000000000001',
    title: 'Office Closed for Maintenance',
    message: 'The Bengaluru office will be closed on 2026-08-15 for electrical maintenance.',
    priority: 'Important',
    audienceType: 'All',
    departmentId: null,
    targetRole: null,
    createdByUserId: '00000000-0000-0000-0000-000000000001',
    createdAtUtc: '2026-07-20T10:00:00Z',
    expiresAtUtc: '2026-08-16T00:00:00Z',
    isRetracted: false,
    readByUserIds: [],
  },
  {
    id: '40000000-0000-0000-0000-000000000002',
    title: 'Payroll Processed Early This Month',
    message: 'Payroll for July has been processed two days early ahead of the bank holiday.',
    priority: 'Normal',
    audienceType: 'All',
    departmentId: null,
    targetRole: null,
    createdByUserId: '00000000-0000-0000-0000-000000000002',
    createdAtUtc: '2026-07-18T08:30:00Z',
    expiresAtUtc: null,
    isRetracted: false,
    readByUserIds: ['00000000-0000-0000-0000-000000000004'],
  },
  {
    id: '40000000-0000-0000-0000-000000000003',
    title: 'Manager Sync Moved to Thursdays',
    message: 'Starting next week, the weekly manager sync moves from Wednesday 4pm to Thursday 10am.',
    priority: 'Critical',
    audienceType: 'Role',
    departmentId: null,
    targetRole: 'Manager',
    createdByUserId: '00000000-0000-0000-0000-000000000001',
    createdAtUtc: '2026-07-22T13:15:00Z',
    expiresAtUtc: null,
    isRetracted: false,
    readByUserIds: [],
  },
]
