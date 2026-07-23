import type { LeaveType } from './leaveTypeRepository'

// IDs are referenced by leave/api/mockData.ts (leave requests + balances) and
// attendance/dashboard mocks that mention leave types, so keep them stable.
export const mockLeaveTypes: LeaveType[] = [
  {
    id: '30000000-0000-0000-0000-000000000001',
    name: 'Casual Leave',
    code: 'CL',
    isPaid: true,
    requiresApproval: true,
    annualEntitlementDays: 12,
    createdAtUtc: '2021-01-01T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '30000000-0000-0000-0000-000000000002',
    name: 'Sick Leave',
    code: 'SL',
    isPaid: true,
    requiresApproval: true,
    annualEntitlementDays: 10,
    createdAtUtc: '2021-01-01T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '30000000-0000-0000-0000-000000000003',
    name: 'Earned Leave',
    code: 'EL',
    isPaid: true,
    requiresApproval: true,
    annualEntitlementDays: 15,
    createdAtUtc: '2021-01-01T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '30000000-0000-0000-0000-000000000004',
    name: 'Unpaid Leave',
    code: 'UL',
    isPaid: false,
    requiresApproval: true,
    annualEntitlementDays: null,
    createdAtUtc: '2021-01-01T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '30000000-0000-0000-0000-000000000005',
    name: 'Work From Home',
    code: 'WFH',
    isPaid: true,
    requiresApproval: false,
    annualEntitlementDays: null,
    createdAtUtc: '2021-01-01T00:00:00Z',
    updatedAtUtc: null,
  },
]
