import { selectRepository } from '@/app/core/config/selectRepository'
import { mockAttendanceRepository } from './mockAttendanceRepository'
import { apiAttendanceRepository } from './apiAttendanceRepository'
import type { AttendanceRepository } from './attendanceRepository'

export const attendanceRepository: AttendanceRepository = selectRepository({
  mock: mockAttendanceRepository,
  api: apiAttendanceRepository,
})

export type {
  AttendanceCorrection,
  AttendanceCorrectionListFilters,
  AttendanceRecord,
  AttendanceRecordListFilters,
  AttendanceRepository,
  AttendanceStatus,
  CheckInInput,
  CheckOutInput,
  CorrectionDecisionInput,
  CorrectionStatus,
  CreateAttendanceRecordInput,
  RequestCorrectionInput,
  UpdateAttendanceRecordInput,
} from './attendanceRepository'
