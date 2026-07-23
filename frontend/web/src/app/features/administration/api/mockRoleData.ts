import type { Role } from './roleRepository'

// IDs are referenced by roleId from mockUserData.ts, so the two mock modules
// stay cross-consistent in mock mode (same pattern as departments/employees).
export const mockRoles: Role[] = [
  {
    id: '00000000-0000-0000-0000-000000000401',
    name: 'Admin',
    description: 'Full administrative access, including user and role management.',
    isDeleted: false,
    createdAtUtc: '2021-01-01T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000402',
    name: 'HR',
    description: 'Manages employees, departments, leave, and payroll on behalf of the company.',
    isDeleted: false,
    createdAtUtc: '2021-01-01T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000403',
    name: 'Manager',
    description: 'Approves leave and views attendance for their direct reports.',
    isDeleted: false,
    createdAtUtc: '2021-01-01T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000404',
    name: 'Employee',
    description: 'Standard self-service access: own profile, attendance, leave, and payslips.',
    isDeleted: false,
    createdAtUtc: '2021-01-01T00:00:00Z',
    updatedAtUtc: null,
  },
]
