import type { DepartmentCount, EmployeeJoinExit, EmployeeReport, LeaveSummaryReport } from './reportsRepository'

// Department IDs/names match the ones used in dashboard and employee mock data,
// so mock mode stays cross-consistent across modules.
export const mockEmployeeReport: EmployeeReport = {
  totalEmployees: 128,
  activeEmployees: 119,
  inactiveEmployees: 9,
}

export const mockDepartmentCounts: DepartmentCount[] = [
  { departmentId: '00000000-0000-0000-0000-000000000301', departmentName: 'Engineering', employeeCount: 46 },
  { departmentId: '00000000-0000-0000-0000-000000000302', departmentName: 'Human Resources', employeeCount: 12 },
  { departmentId: '00000000-0000-0000-0000-000000000303', departmentName: 'Sales', employeeCount: 28 },
  { departmentId: '00000000-0000-0000-0000-000000000304', departmentName: 'Finance', employeeCount: 15 },
  { departmentId: '00000000-0000-0000-0000-000000000305', departmentName: 'Operations', employeeCount: 18 },
]

export const mockLeaveSummary: LeaveSummaryReport = {
  totalRequests: 42,
  pending: 6,
  approved: 31,
  rejected: 5,
}

export const mockEmployeeTurnover: EmployeeJoinExit[] = [
  {
    employeeId: '10000000-0000-0000-0000-000000000001',
    employeeName: 'Ava Patel',
    joinDate: '2022-03-01',
    exitDate: null,
  },
  {
    employeeId: '10000000-0000-0000-0000-000000000002',
    employeeName: 'Liam Chen',
    joinDate: '2021-07-12',
    exitDate: null,
  },
  {
    employeeId: '10000000-0000-0000-0000-000000000009',
    employeeName: 'Noah Kim',
    joinDate: '2020-05-18',
    exitDate: '2024-11-30',
  },
]
