import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import type {
  AttendanceCorrection,
  AttendanceCorrectionListFilters,
  AttendanceRecord,
  AttendanceRecordListFilters,
  AttendanceRepository,
  CheckInInput,
  CheckOutInput,
  CorrectionDecisionInput,
  CreateAttendanceRecordInput,
  RequestCorrectionInput,
  UpdateAttendanceRecordInput,
} from './attendanceRepository'
import { mockAttendanceCorrections, mockAttendanceRecords } from './mockData'

let records = [...mockAttendanceRecords]
let corrections = [...mockAttendanceCorrections]

function nextId(prefix: string): string {
  return `${prefix}-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

function sameUtcDate(a: string, b: string): boolean {
  return a.slice(0, 10) === b.slice(0, 10)
}

function paginate<T>(items: T[], page: number, pageSize: number): PagedResult<T> {
  const start = (page - 1) * pageSize
  const pageItems = items.slice(start, start + pageSize)
  return {
    data: pageItems,
    page,
    pageSize,
    totalCount: items.length,
    totalPages: Math.max(1, Math.ceil(items.length / pageSize)),
    correlationId: 'mock-correlation-id',
  }
}

export const mockAttendanceRepository: AttendanceRepository = {
  async checkIn(input: CheckInInput): Promise<AttendanceRecord> {
    await delay(300)
    const existing = records.find(
      (r) => r.employeeId === input.employeeId && sameUtcDate(r.attendanceDate, input.checkInAtUtc),
    )
    if (existing?.checkInAtUtc) {
      throw new AppError('Already checked in for this date.', 409, 'CONFLICT')
    }
    const record: AttendanceRecord = existing
      ? { ...existing, checkInAtUtc: input.checkInAtUtc, notes: input.notes ?? existing.notes, updatedAtUtc: new Date().toISOString() }
      : {
          id: nextId('40000000'),
          employeeId: input.employeeId,
          shiftId: null,
          attendanceDate: input.checkInAtUtc.slice(0, 10) + 'T00:00:00.000Z',
          checkInAtUtc: input.checkInAtUtc,
          checkOutAtUtc: null,
          status: 'Present',
          isLateArrival: false,
          isEarlyLeave: false,
          totalWorkMinutes: null,
          notes: input.notes ?? null,
          createdAtUtc: new Date().toISOString(),
          updatedAtUtc: null,
        }
    records = existing ? records.map((r) => (r.id === record.id ? record : r)) : [record, ...records]
    return record
  },

  async checkOut(input: CheckOutInput): Promise<AttendanceRecord> {
    await delay(300)
    const existing = records.find(
      (r) => r.employeeId === input.employeeId && sameUtcDate(r.attendanceDate, input.checkOutAtUtc),
    )
    if (!existing?.checkInAtUtc) {
      throw new AppError('You must check in before checking out.', 409, 'CONFLICT')
    }
    const checkInMs = new Date(existing.checkInAtUtc).getTime()
    const checkOutMs = new Date(input.checkOutAtUtc).getTime()
    const updated: AttendanceRecord = {
      ...existing,
      checkOutAtUtc: input.checkOutAtUtc,
      totalWorkMinutes: Math.max(0, Math.round((checkOutMs - checkInMs) / 60000)),
      notes: input.notes ?? existing.notes,
      updatedAtUtc: new Date().toISOString(),
    }
    records = records.map((r) => (r.id === updated.id ? updated : r))
    return updated
  },

  async list(filters: AttendanceRecordListFilters = {}): Promise<PagedResult<AttendanceRecord>> {
    await delay(250)
    const { page = 1, pageSize = 20, employeeId, dateFrom, dateTo, status, isLateArrival, isEarlyLeave } = filters

    let filtered = records
    if (employeeId) filtered = filtered.filter((r) => r.employeeId === employeeId)
    if (dateFrom) filtered = filtered.filter((r) => r.attendanceDate >= dateFrom)
    if (dateTo) filtered = filtered.filter((r) => r.attendanceDate <= dateTo)
    if (status) filtered = filtered.filter((r) => r.status === status)
    if (isLateArrival !== undefined) filtered = filtered.filter((r) => r.isLateArrival === isLateArrival)
    if (isEarlyLeave !== undefined) filtered = filtered.filter((r) => r.isEarlyLeave === isEarlyLeave)

    filtered = [...filtered].sort((a, b) => b.attendanceDate.localeCompare(a.attendanceDate))
    return paginate(filtered, page, pageSize)
  },

  async getById(id: string): Promise<AttendanceRecord> {
    await delay(200)
    const record = records.find((r) => r.id === id)
    if (!record) throw new AppError(`Attendance record ${id} was not found.`, 404, 'NOT_FOUND')
    return record
  },

  async create(input: CreateAttendanceRecordInput): Promise<AttendanceRecord> {
    await delay(300)
    const record: AttendanceRecord = {
      id: nextId('40000000'),
      employeeId: input.employeeId,
      shiftId: input.shiftId ?? null,
      attendanceDate: input.attendanceDate,
      checkInAtUtc: input.checkInAtUtc ?? null,
      checkOutAtUtc: input.checkOutAtUtc ?? null,
      status: input.status,
      isLateArrival: false,
      isEarlyLeave: false,
      totalWorkMinutes: null,
      notes: input.notes ?? null,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    records = [record, ...records]
    return record
  },

  async update(input: UpdateAttendanceRecordInput): Promise<AttendanceRecord> {
    await delay(300)
    const existing = records.find((r) => r.id === input.id)
    if (!existing) throw new AppError(`Attendance record ${input.id} was not found.`, 404, 'NOT_FOUND')
    const updated: AttendanceRecord = {
      ...existing,
      shiftId: input.shiftId ?? existing.shiftId,
      checkInAtUtc: input.checkInAtUtc ?? existing.checkInAtUtc,
      checkOutAtUtc: input.checkOutAtUtc ?? existing.checkOutAtUtc,
      status: input.status,
      notes: input.notes ?? existing.notes,
      updatedAtUtc: new Date().toISOString(),
    }
    records = records.map((r) => (r.id === input.id ? updated : r))
    return updated
  },

  async remove(id: string): Promise<void> {
    await delay(200)
    records = records.filter((r) => r.id !== id)
  },

  async listCorrections(filters: AttendanceCorrectionListFilters = {}): Promise<PagedResult<AttendanceCorrection>> {
    await delay(250)
    const { page = 1, pageSize = 20, employeeId, status } = filters
    let filtered = corrections
    if (employeeId) filtered = filtered.filter((c) => c.requestedByEmployeeId === employeeId)
    if (status) filtered = filtered.filter((c) => c.status === status)
    filtered = [...filtered].sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc))
    return paginate(filtered, page, pageSize)
  },

  async requestCorrection(input: RequestCorrectionInput): Promise<AttendanceCorrection> {
    await delay(300)
    const record = records.find((r) => r.id === input.attendanceRecordId)
    if (!record) throw new AppError('Attendance record was not found.', 404, 'NOT_FOUND')
    const correction: AttendanceCorrection = {
      id: nextId('50000000'),
      attendanceRecordId: input.attendanceRecordId,
      requestedByEmployeeId: record.employeeId,
      approvedByEmployeeId: null,
      requestedCheckInAtUtc: input.requestedCheckInAtUtc ?? null,
      requestedCheckOutAtUtc: input.requestedCheckOutAtUtc ?? null,
      reason: input.reason,
      status: 'Pending',
      decisionAtUtc: null,
      decisionComments: null,
      createdAtUtc: new Date().toISOString(),
    }
    corrections = [correction, ...corrections]
    return correction
  },

  async approveCorrection(id: string, input?: CorrectionDecisionInput): Promise<void> {
    await delay(250)
    const correction = corrections.find((c) => c.id === id)
    if (!correction) throw new AppError('Correction request was not found.', 404, 'NOT_FOUND')
    if (correction.status !== 'Pending') {
      throw new AppError('Only pending correction requests can be approved.', 409, 'CONFLICT')
    }
    const record = records.find((r) => r.id === correction.attendanceRecordId)
    if (record) {
      records = records.map((r) =>
        r.id === record.id
          ? {
              ...r,
              checkInAtUtc: correction.requestedCheckInAtUtc ?? r.checkInAtUtc,
              checkOutAtUtc: correction.requestedCheckOutAtUtc ?? r.checkOutAtUtc,
              updatedAtUtc: new Date().toISOString(),
            }
          : r,
      )
    }
    corrections = corrections.map((c) =>
      c.id === id
        ? { ...c, status: 'Approved', decisionAtUtc: new Date().toISOString(), decisionComments: input?.comments ?? null }
        : c,
    )
  },

  async rejectCorrection(id: string, input?: CorrectionDecisionInput): Promise<void> {
    await delay(250)
    const correction = corrections.find((c) => c.id === id)
    if (!correction) throw new AppError('Correction request was not found.', 404, 'NOT_FOUND')
    if (correction.status !== 'Pending') {
      throw new AppError('Only pending correction requests can be rejected.', 409, 'CONFLICT')
    }
    corrections = corrections.map((c) =>
      c.id === id
        ? { ...c, status: 'Rejected', decisionAtUtc: new Date().toISOString(), decisionComments: input?.comments ?? null }
        : c,
    )
  },
}
