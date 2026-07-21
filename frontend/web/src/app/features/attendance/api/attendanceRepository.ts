export type AttendanceStatus = 'Present' | 'Absent' | 'Late' | 'OnLeave'

export interface AttendanceRecord {
  id: string
  employeeId: string
  date: string
  checkInUtc: string | null
  checkOutUtc: string | null
  status: AttendanceStatus
}

/**
 * Contract only for now — no mock/API implementation yet, and no backend
 * AttendanceController exists to call. Follow the pattern in
 * features/employees/api when both are ready.
 */
export interface AttendanceRepository {
  getHistory(employeeId: string): Promise<AttendanceRecord[]>
  checkIn(employeeId: string): Promise<AttendanceRecord>
  checkOut(employeeId: string): Promise<AttendanceRecord>
}
