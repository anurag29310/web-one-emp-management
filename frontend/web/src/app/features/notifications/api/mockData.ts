import type { Notification } from './notificationRepository'

// userId values line up with frontend/web/src/app/features/auth/api/mockData.ts's seeded
// accounts (Admin, HR, Manager, Employee, in that order).
export const mockNotifications: Notification[] = [
  {
    id: '50000000-0000-0000-0000-000000000001',
    userId: '00000000-0000-0000-0000-000000000004',
    title: 'Leave Approved',
    message: 'Your leave request for 2026-08-01 to 2026-08-03 was approved.',
    channel: 'InApp',
    isRead: false,
    createdAtUtc: '2026-07-23T09:00:00Z',
    expiresAtUtc: null,
  },
  {
    id: '50000000-0000-0000-0000-000000000002',
    userId: '00000000-0000-0000-0000-000000000004',
    title: 'Attendance Correction Approved',
    message: 'Your attendance correction for 2026-07-18 has been approved.',
    channel: 'InApp',
    isRead: true,
    createdAtUtc: '2026-07-19T15:30:00Z',
    expiresAtUtc: null,
  },
  {
    id: '50000000-0000-0000-0000-000000000003',
    userId: '00000000-0000-0000-0000-000000000003',
    title: 'New Leave Request Awaiting Approval',
    message: 'Ava Patel submitted a leave request that needs your review.',
    channel: 'InApp',
    isRead: false,
    createdAtUtc: '2026-07-23T11:20:00Z',
    expiresAtUtc: null,
  },
  {
    id: '50000000-0000-0000-0000-000000000004',
    userId: '00000000-0000-0000-0000-000000000001',
    title: 'Payroll Run Completed',
    message: 'The July payroll run finished processing 42 payslips.',
    channel: 'InApp',
    isRead: false,
    createdAtUtc: '2026-07-20T18:00:00Z',
    expiresAtUtc: null,
  },
]
