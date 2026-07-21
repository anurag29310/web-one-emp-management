import type { LeaveRequest } from './leaveRepository'
import { KNOWN_LEAVE_TYPES } from './leaveTypes'

const [casual, sick, earned] = KNOWN_LEAVE_TYPES

export const mockLeaveRequests: LeaveRequest[] = [
  {
    id: '40000000-0000-0000-0000-000000000001',
    employeeId: '10000000-0000-0000-0000-000000000001',
    leaveTypeId: casual.id,
    approverEmployeeId: null,
    startDate: '2026-07-20',
    endDate: '2026-07-21',
    totalDays: 2,
    reason: 'Family event',
    status: 'Pending',
    createdAtUtc: '2026-07-10T09:00:00Z',
    decisionAtUtc: null,
    decisionComments: null,
  },
  {
    id: '40000000-0000-0000-0000-000000000002',
    employeeId: '10000000-0000-0000-0000-000000000003',
    leaveTypeId: sick.id,
    approverEmployeeId: '10000000-0000-0000-0000-000000000002',
    startDate: '2026-07-08',
    endDate: '2026-07-09',
    totalDays: 2,
    reason: 'Not feeling well',
    status: 'Approved',
    createdAtUtc: '2026-07-07T08:30:00Z',
    decisionAtUtc: '2026-07-07T10:00:00Z',
    decisionComments: 'Get well soon.',
  },
  {
    id: '40000000-0000-0000-0000-000000000003',
    employeeId: '10000000-0000-0000-0000-000000000002',
    leaveTypeId: earned.id,
    approverEmployeeId: '10000000-0000-0000-0000-000000000001',
    startDate: '2026-06-01',
    endDate: '2026-06-05',
    totalDays: 5,
    reason: 'Vacation',
    status: 'Rejected',
    createdAtUtc: '2026-05-20T12:00:00Z',
    decisionAtUtc: '2026-05-21T09:00:00Z',
    decisionComments: 'Team is short-staffed that week — please pick different dates.',
  },
]
