export interface AttendanceSummary {
  present: number
  absent: number
  late: number
  onLeave: number
}

export interface LeaveSummary {
  pending: number
  approvedToday: number
  rejectedToday: number
}

export interface DepartmentSummary {
  departmentId: string
  departmentName: string
  activeEmployees: number
}

export interface DashboardSummary {
  totalEmployees: number
  activeEmployees: number
  inactiveEmployees: number
  attendance: AttendanceSummary
  leave: LeaveSummary
  departments: DepartmentSummary[]
}

export interface DashboardSummaryFilters {
  departmentId?: string
  officeLocationId?: string
  date?: string
}
