import type { Department } from './departmentRepository'

// IDs/names match the department references used in the dashboard and
// employee mock data, so the three modules stay cross-consistent in mock mode.
export const mockDepartments: Department[] = [
  {
    id: '00000000-0000-0000-0000-000000000301',
    name: 'Engineering',
    code: 'ENG',
    description: 'Product engineering and platform teams.',
    headEmployeeId: '10000000-0000-0000-0000-000000000001',
    isDeleted: false,
    createdAtUtc: '2021-01-10T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000302',
    name: 'Human Resources',
    code: 'HR',
    description: 'People operations, hiring, and employee relations.',
    headEmployeeId: '10000000-0000-0000-0000-000000000002',
    isDeleted: false,
    createdAtUtc: '2021-01-10T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000303',
    name: 'Sales',
    code: 'SALES',
    description: 'Account management and new business.',
    headEmployeeId: null,
    isDeleted: false,
    createdAtUtc: '2021-02-01T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000304',
    name: 'Finance',
    code: 'FIN',
    description: 'Accounting, payroll, and financial planning.',
    headEmployeeId: null,
    isDeleted: false,
    createdAtUtc: '2021-02-15T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000305',
    name: 'Operations',
    code: 'OPS',
    description: 'Facilities, logistics, and vendor management.',
    headEmployeeId: null,
    isDeleted: false,
    createdAtUtc: '2021-03-01T00:00:00Z',
    updatedAtUtc: null,
  },
]
