import { httpClient, unwrap } from '@/app/core/api/httpClient'
import { extractFileName } from '@/app/shared/utils/downloadBlob'
import type {
  DateRangeFilter,
  DepartmentCount,
  EmployeeJoinExit,
  EmployeeReport,
  FileDownload,
  LeaveSummaryReport,
  ReportsRepository,
} from './reportsRepository'

async function downloadReport(url: string, params: DateRangeFilter | undefined, fallbackName: string): Promise<FileDownload> {
  const response = await httpClient.get<Blob>(url, { params, responseType: 'blob' })
  return {
    blob: response.data,
    fileName: extractFileName(response.headers['content-disposition'] as string | undefined, fallbackName),
  }
}

export const apiReportsRepository: ReportsRepository = {
  async getEmployeeReport(): Promise<EmployeeReport> {
    const response = await httpClient.get<{ data: EmployeeReport }>('/reports/employees')
    return unwrap(response)
  },

  async getDepartmentCounts(): Promise<DepartmentCount[]> {
    const response = await httpClient.get<{ data: DepartmentCount[] }>('/reports/departments')
    return unwrap(response)
  },

  async exportDepartmentCounts(): Promise<FileDownload> {
    return downloadReport('/reports/departments/export', undefined, 'departments.csv')
  },

  async getLeaveSummary(filter: DateRangeFilter): Promise<LeaveSummaryReport> {
    const response = await httpClient.get<{ data: LeaveSummaryReport }>('/reports/leave-summary', {
      params: filter,
    })
    return unwrap(response)
  },

  async getEmployeeTurnover(filter: DateRangeFilter): Promise<EmployeeJoinExit[]> {
    const response = await httpClient.get<{ data: EmployeeJoinExit[] }>('/reports/employee-turnover', {
      params: filter,
    })
    return unwrap(response)
  },

  async exportEmployeeTurnover(filter: DateRangeFilter): Promise<FileDownload> {
    return downloadReport('/reports/employee-turnover/export', filter, 'employee-turnover.csv')
  },
}
