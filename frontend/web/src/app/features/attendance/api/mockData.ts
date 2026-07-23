import type { AttendanceCorrection, AttendanceRecord } from './attendanceRepository'

const EMPLOYEE_AVA = '10000000-0000-0000-0000-000000000001'
const EMPLOYEE_LIAM = '10000000-0000-0000-0000-000000000002'
const SHIFT_MORNING = '30000000-0000-0000-0000-000000000001'

function isoDateOnly(daysAgo: number): string {
  const date = new Date()
  date.setUTCDate(date.getUTCDate() - daysAgo)
  date.setUTCHours(0, 0, 0, 0)
  return date.toISOString()
}

function isoTime(daysAgo: number, hours: number, minutes: number): string {
  const date = new Date()
  date.setUTCDate(date.getUTCDate() - daysAgo)
  date.setUTCHours(hours, minutes, 0, 0)
  return date.toISOString()
}

export const mockAttendanceRecords: AttendanceRecord[] = [
  // Today — Ava checked in, has not checked out yet (drives the "check out" call to action).
  {
    id: '40000000-0000-0000-0000-000000000001',
    employeeId: EMPLOYEE_AVA,
    shiftId: SHIFT_MORNING,
    attendanceDate: isoDateOnly(0),
    checkInAtUtc: isoTime(0, 9, 4),
    checkOutAtUtc: null,
    status: 'Present',
    isLateArrival: false,
    isEarlyLeave: false,
    totalWorkMinutes: null,
    notes: null,
    createdAtUtc: isoTime(0, 9, 4),
    updatedAtUtc: null,
  },
  {
    id: '40000000-0000-0000-0000-000000000002',
    employeeId: EMPLOYEE_AVA,
    shiftId: SHIFT_MORNING,
    attendanceDate: isoDateOnly(1),
    checkInAtUtc: isoTime(1, 9, 18),
    checkOutAtUtc: isoTime(1, 17, 32),
    status: 'Late',
    isLateArrival: true,
    isEarlyLeave: false,
    totalWorkMinutes: 494,
    notes: 'Traffic delay',
    createdAtUtc: isoTime(1, 9, 18),
    updatedAtUtc: isoTime(1, 17, 32),
  },
  {
    id: '40000000-0000-0000-0000-000000000003',
    employeeId: EMPLOYEE_AVA,
    shiftId: SHIFT_MORNING,
    attendanceDate: isoDateOnly(2),
    checkInAtUtc: isoTime(2, 8, 55),
    checkOutAtUtc: isoTime(2, 17, 5),
    status: 'Present',
    isLateArrival: false,
    isEarlyLeave: false,
    totalWorkMinutes: 490,
    notes: null,
    createdAtUtc: isoTime(2, 8, 55),
    updatedAtUtc: isoTime(2, 17, 5),
  },
  {
    id: '40000000-0000-0000-0000-000000000004',
    employeeId: EMPLOYEE_AVA,
    shiftId: null,
    attendanceDate: isoDateOnly(3),
    checkInAtUtc: null,
    checkOutAtUtc: null,
    status: 'Absent',
    isLateArrival: false,
    isEarlyLeave: false,
    totalWorkMinutes: null,
    notes: null,
    createdAtUtc: isoTime(3, 0, 0),
    updatedAtUtc: null,
  },
  {
    id: '40000000-0000-0000-0000-000000000005',
    employeeId: EMPLOYEE_LIAM,
    shiftId: SHIFT_MORNING,
    attendanceDate: isoDateOnly(1),
    checkInAtUtc: isoTime(1, 9, 1),
    checkOutAtUtc: isoTime(1, 18, 10),
    status: 'Present',
    isLateArrival: false,
    isEarlyLeave: false,
    totalWorkMinutes: 549,
    notes: null,
    createdAtUtc: isoTime(1, 9, 1),
    updatedAtUtc: isoTime(1, 18, 10),
  },
]

export const mockAttendanceCorrections: AttendanceCorrection[] = [
  {
    id: '50000000-0000-0000-0000-000000000001',
    attendanceRecordId: '40000000-0000-0000-0000-000000000002',
    requestedByEmployeeId: EMPLOYEE_AVA,
    approvedByEmployeeId: null,
    requestedCheckInAtUtc: isoTime(1, 9, 0),
    requestedCheckOutAtUtc: null,
    reason: 'Badge reader was down, actual arrival was on time.',
    status: 'Pending',
    decisionAtUtc: null,
    decisionComments: null,
    createdAtUtc: isoTime(1, 18, 0),
  },
]
