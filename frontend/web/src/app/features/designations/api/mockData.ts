import type { Designation } from './designationRepository'

export const mockDesignations: Designation[] = [
  {
    id: '00000000-0000-0000-0000-000000000401',
    name: 'Software Engineer',
    code: 'SE',
    level: 2,
    isDeleted: false,
    createdAtUtc: '2021-01-10T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000402',
    name: 'Senior Software Engineer',
    code: 'SSE',
    level: 3,
    isDeleted: false,
    createdAtUtc: '2021-01-10T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000403',
    name: 'Engineering Manager',
    code: 'EM',
    level: 4,
    isDeleted: false,
    createdAtUtc: '2021-02-01T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000404',
    name: 'HR Executive',
    code: 'HRE',
    level: 1,
    isDeleted: false,
    createdAtUtc: '2021-02-15T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000405',
    name: 'HR Manager',
    code: 'HRM',
    level: 3,
    isDeleted: false,
    createdAtUtc: '2021-03-01T00:00:00Z',
    updatedAtUtc: null,
  },
]
