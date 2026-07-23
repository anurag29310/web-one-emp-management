import type { EmployeeShift, Shift } from './shiftRepository'

export const mockShifts: Shift[] = [
  {
    id: '30000000-0000-0000-0000-000000000001',
    name: 'Morning Shift',
    startTime: '09:00:00',
    endTime: '17:30:00',
    graceMinutes: 10,
    isNightShift: false,
  },
  {
    id: '30000000-0000-0000-0000-000000000002',
    name: 'Evening Shift',
    startTime: '14:00:00',
    endTime: '22:30:00',
    graceMinutes: 10,
    isNightShift: false,
  },
  {
    id: '30000000-0000-0000-0000-000000000003',
    name: 'Night Shift',
    startTime: '22:00:00',
    endTime: '06:00:00',
    graceMinutes: 15,
    isNightShift: true,
  },
]

export const mockEmployeeShifts: EmployeeShift[] = [
  {
    id: '60000000-0000-0000-0000-000000000001',
    employeeId: '10000000-0000-0000-0000-000000000001',
    shiftId: '30000000-0000-0000-0000-000000000001',
    effectiveFrom: '2022-03-01T00:00:00.000Z',
    effectiveTo: null,
  },
  {
    id: '60000000-0000-0000-0000-000000000002',
    employeeId: '10000000-0000-0000-0000-000000000002',
    shiftId: '30000000-0000-0000-0000-000000000001',
    effectiveFrom: '2021-07-12T00:00:00.000Z',
    effectiveTo: null,
  },
]
