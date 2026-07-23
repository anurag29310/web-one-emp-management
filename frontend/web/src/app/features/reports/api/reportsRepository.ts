/**
 * Contract for /reports (api-specification.md §18), cross-checked against
 * backend/EMS.API/Controllers/ReportsController.cs and the DTOs in
 * EMS.Application/DTOs/Reports. All endpoints require the CanViewReports
 * policy (Admin, HR, Manager) — enforced server-side; the frontend only
 * gates which nav/route entries it *offers* per role, via useAuth().user.role.
 */

export interface EmployeeReport {
  totalEmployees: number
  activeEmployees: number
  inactiveEmployees: number
}

export interface DepartmentCount {
  departmentId: string
  departmentName: string
  employeeCount: number
}

export interface LeaveSummaryReport {
  totalRequests: number
  pending: number
  approved: number
  rejected: number
}

export interface EmployeeJoinExit {
  employeeId: string
  employeeName: string
  joinDate: string
  exitDate: string | null
}

/** `from`/`to` are both required and `from` must be <= `to` (GetLeaveSummaryQueryValidator / GetEmployeeJoinExitQueryValidator). */
export interface DateRangeFilter {
  from: string
  to: string
}

export interface FileDownload {
  blob: Blob
  fileName: string
}

export interface ReportsRepository {
  getEmployeeReport(): Promise<EmployeeReport>
  getDepartmentCounts(): Promise<DepartmentCount[]>
  exportDepartmentCounts(): Promise<FileDownload>
  getLeaveSummary(filter: DateRangeFilter): Promise<LeaveSummaryReport>
  getEmployeeTurnover(filter: DateRangeFilter): Promise<EmployeeJoinExit[]>
  exportEmployeeTurnover(filter: DateRangeFilter): Promise<FileDownload>
}
