import type { Team } from './teamRepository'

export const mockTeams: Team[] = [
  {
    id: '00000000-0000-0000-0000-000000000401',
    departmentId: '00000000-0000-0000-0000-000000000301',
    departmentName: 'Engineering',
    name: 'Platform',
    code: 'PLAT',
    leadEmployeeId: '10000000-0000-0000-0000-000000000001',
    isDeleted: false,
    createdAtUtc: '2021-04-01T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000402',
    departmentId: '00000000-0000-0000-0000-000000000301',
    departmentName: 'Engineering',
    name: 'Mobile',
    code: 'MOBILE',
    leadEmployeeId: null,
    isDeleted: false,
    createdAtUtc: '2021-05-01T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000403',
    departmentId: '00000000-0000-0000-0000-000000000302',
    departmentName: 'Human Resources',
    name: 'Talent Acquisition',
    code: 'TA',
    leadEmployeeId: '10000000-0000-0000-0000-000000000002',
    isDeleted: false,
    createdAtUtc: '2021-06-01T00:00:00Z',
    updatedAtUtc: null,
  },
  {
    id: '00000000-0000-0000-0000-000000000404',
    departmentId: '00000000-0000-0000-0000-000000000303',
    departmentName: 'Sales',
    name: 'Enterprise Accounts',
    code: 'ENT',
    leadEmployeeId: null,
    isDeleted: false,
    createdAtUtc: '2021-07-01T00:00:00Z',
    updatedAtUtc: null,
  },
]

// Maps team id -> employee ids from the employees feature's mock data, so the
// GET /teams/{id}/employees endpoint can be simulated in mock mode.
export const mockTeamEmployeeIds: Record<string, string[]> = {
  '00000000-0000-0000-0000-000000000401': ['10000000-0000-0000-0000-000000000001'],
  '00000000-0000-0000-0000-000000000403': ['10000000-0000-0000-0000-000000000002'],
  '00000000-0000-0000-0000-000000000404': ['10000000-0000-0000-0000-000000000003'],
}
