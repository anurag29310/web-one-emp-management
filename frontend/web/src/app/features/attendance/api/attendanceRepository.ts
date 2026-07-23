import type { PagedResult } from '@/app/shared/models/apiEnvelope'

export type AttendanceStatus = 'Present' | 'Absent' | 'Late' | 'HalfDay' | 'OnLeave' | 'Holiday'
export type CorrectionStatus = 'Pending' | 'Approved' | 'Rejected'

export interface AttendanceRecord {
  id: string
  employeeId: string
  shiftId: string | null
  attendanceDate: string
  checkInAtUtc: string | null
  checkOutAtUtc: string | null
  status: AttendanceStatus
  isLateArrival: boolean
  isEarlyLeave: boolean
  totalWorkMinutes: number | null
  notes: string | null
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface AttendanceRecordListFilters {
  page?: number
  pageSize?: number
  employeeId?: string
  departmentId?: string
  managerId?: string
  dateFrom?: string
  dateTo?: string
  status?: AttendanceStatus
  isLateArrival?: boolean
  isEarlyLeave?: boolean
}

export interface CheckInInput {
  employeeId: string
  checkInAtUtc: string
  notes?: string
}

export interface CheckOutInput {
  employeeId: string
  checkOutAtUtc: string
  notes?: string
}

/** Matches CreateAttendanceRecordCommand — Admin/HR only (api-specification.md 8.5). */
export interface CreateAttendanceRecordInput {
  employeeId: string
  shiftId?: string
  attendanceDate: string
  checkInAtUtc?: string
  checkOutAtUtc?: string
  status: AttendanceStatus
  notes?: string
}

/** Matches UpdateAttendanceRecordCommand — no employeeId/attendanceDate; those are immutable after creation. */
export interface UpdateAttendanceRecordInput {
  id: string
  shiftId?: string
  checkInAtUtc?: string
  checkOutAtUtc?: string
  status: AttendanceStatus
  notes?: string
}

export interface AttendanceCorrection {
  id: string
  attendanceRecordId: string
  requestedByEmployeeId: string
  approvedByEmployeeId: string | null
  requestedCheckInAtUtc: string | null
  requestedCheckOutAtUtc: string | null
  reason: string
  status: CorrectionStatus
  decisionAtUtc: string | null
  decisionComments: string | null
  createdAtUtc: string
}

export interface AttendanceCorrectionListFilters {
  page?: number
  pageSize?: number
  employeeId?: string
  status?: CorrectionStatus
}

export interface RequestCorrectionInput {
  attendanceRecordId: string
  requestedCheckInAtUtc?: string
  requestedCheckOutAtUtc?: string
  reason: string
}

export interface CorrectionDecisionInput {
  comments?: string
}

/**
 * Contract for /attendance and /attendance/corrections (api-specification.md §8.1-8.8),
 * cross-checked against backend/EMS.API/Controllers/AttendanceController.cs. Access control
 * (CanManageAttendanceRecords, CanReviewAttendanceCorrections) is enforced server-side; the
 * frontend only gates which actions it *offers* per role — see useAuth().user.role.
 */
export interface AttendanceRepository {
  checkIn(input: CheckInInput): Promise<AttendanceRecord>
  checkOut(input: CheckOutInput): Promise<AttendanceRecord>
  list(filters?: AttendanceRecordListFilters): Promise<PagedResult<AttendanceRecord>>
  getById(id: string): Promise<AttendanceRecord>
  create(input: CreateAttendanceRecordInput): Promise<AttendanceRecord>
  update(input: UpdateAttendanceRecordInput): Promise<AttendanceRecord>
  remove(id: string): Promise<void>
  listCorrections(filters?: AttendanceCorrectionListFilters): Promise<PagedResult<AttendanceCorrection>>
  requestCorrection(input: RequestCorrectionInput): Promise<AttendanceCorrection>
  approveCorrection(id: string, input?: CorrectionDecisionInput): Promise<void>
  rejectCorrection(id: string, input?: CorrectionDecisionInput): Promise<void>
}
