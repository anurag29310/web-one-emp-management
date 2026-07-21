import type { DashboardSummary } from '@/app/shared/models/dashboard'

export const mockDashboardSummary: DashboardSummary = {
  totalEmployees: 128,
  activeEmployees: 119,
  inactiveEmployees: 9,
  attendance: {
    present: 104,
    absent: 6,
    late: 9,
    onLeave: 9,
  },
  leave: {
    pending: 5,
    approvedToday: 3,
    rejectedToday: 1,
  },
  departments: [
    { departmentId: '00000000-0000-0000-0000-000000000301', departmentName: 'Engineering', activeEmployees: 46 },
    { departmentId: '00000000-0000-0000-0000-000000000302', departmentName: 'Human Resources', activeEmployees: 12 },
    { departmentId: '00000000-0000-0000-0000-000000000303', departmentName: 'Sales', activeEmployees: 28 },
    { departmentId: '00000000-0000-0000-0000-000000000304', departmentName: 'Finance', activeEmployees: 15 },
    { departmentId: '00000000-0000-0000-0000-000000000305', departmentName: 'Operations', activeEmployees: 18 },
  ],
}
