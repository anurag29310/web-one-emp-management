import { httpClient } from '@/app/core/api/httpClient'
import { extractFileName } from '@/app/shared/utils/downloadBlob'
import type {
  AttendanceExportFilters,
  DashboardSummaryExportFilters,
  EmployeeExportFilters,
  ExportsRepository,
  FileDownload,
  LeaveRequestExportFilters,
} from './exportsRepository'

async function download(
  url: string,
  params:
    | EmployeeExportFilters
    | AttendanceExportFilters
    | LeaveRequestExportFilters
    | DashboardSummaryExportFilters
    | undefined,
  fallbackName: string,
): Promise<FileDownload> {
  const response = await httpClient.get<Blob>(url, { params, responseType: 'blob' })
  return {
    blob: response.data,
    fileName: extractFileName(response.headers['content-disposition'] as string | undefined, fallbackName),
  }
}

export const apiExportsRepository: ExportsRepository = {
  async exportEmployees(filters: EmployeeExportFilters = {}): Promise<FileDownload> {
    return download('/exports/employees', filters, 'employees.xlsx')
  },

  async exportAttendance(filters: AttendanceExportFilters = {}): Promise<FileDownload> {
    return download('/exports/attendance', filters, 'attendance.xlsx')
  },

  async exportLeaveRequests(filters: LeaveRequestExportFilters = {}): Promise<FileDownload> {
    return download('/exports/leave-requests', filters, 'leave-requests.xlsx')
  },

  async exportDashboardSummary(filters: DashboardSummaryExportFilters = {}): Promise<FileDownload> {
    return download('/exports/dashboard-summary', filters, 'dashboard-summary.pdf')
  },
}
