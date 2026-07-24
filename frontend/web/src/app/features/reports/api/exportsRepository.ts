/**
 * Contract for /exports (api-specification.md §13), cross-checked against
 * backend/EMS.API/Controllers/ExportsController.cs. Every endpoint returns the
 * file directly (Content-Disposition: attachment), not the usual JSON
 * envelope, so callers must use responseType: 'blob'.
 */
import type { FileDownload } from './reportsRepository'

export type { FileDownload } from './reportsRepository'

/** Same filters as GET /employees (api-specification.md §5.1). Admin/HR only (CanManageEmployees). */
export interface EmployeeExportFilters {
  search?: string
  sortBy?: string
  sortDir?: string
  departmentId?: string
  status?: string
}

/**
 * Same filters as GET /attendance (api-specification.md §8.3). Manager-role callers are
 * scoped server-side to their own team regardless of employeeId/departmentId supplied here.
 */
export interface AttendanceExportFilters {
  employeeId?: string
  departmentId?: string
  managerId?: string
  dateFrom?: string
  dateTo?: string
  status?: string
  isLateArrival?: boolean
  isEarlyLeave?: boolean
}

/** Same filters as GET /leave (api-specification.md §9.2). Admin, HR, Manager only. */
export interface LeaveRequestExportFilters {
  employeeId?: string
  leaveTypeId?: string
  year?: number
  status?: string
}

/** Same filters as GET /dashboard/summary (api-specification.md §10.1). */
export interface DashboardSummaryExportFilters {
  departmentId?: string
  officeLocationId?: string
  date?: string
}

export interface ExportsRepository {
  exportEmployees(filters?: EmployeeExportFilters): Promise<FileDownload>
  exportAttendance(filters?: AttendanceExportFilters): Promise<FileDownload>
  exportLeaveRequests(filters?: LeaveRequestExportFilters): Promise<FileDownload>
  exportDashboardSummary(filters?: DashboardSummaryExportFilters): Promise<FileDownload>
}
