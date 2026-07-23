import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type {
  DateRangeFilter,
  DepartmentCount,
  EmployeeJoinExit,
  EmployeeReport,
  FileDownload,
  LeaveSummaryReport,
  ReportsRepository,
} from './reportsRepository'
import { mockDepartmentCounts, mockEmployeeReport, mockEmployeeTurnover, mockLeaveSummary } from './mockData'

function assertValidRange(filter: DateRangeFilter): void {
  if (!filter.from || !filter.to) {
    throw new AppError('from and to are required.', 400, 'VALIDATION_ERROR')
  }
  if (new Date(filter.from).getTime() > new Date(filter.to).getTime()) {
    throw new AppError('from must be before or equal to to.', 400, 'VALIDATION_ERROR')
  }
}

function toCsv(rows: string[][]): Blob {
  const content = rows.map((row) => row.map((cell) => `"${cell.replace(/"/g, '""')}"`).join(',')).join('\r\n')
  return new Blob([content], { type: 'text/csv' })
}

export const mockReportsRepository: ReportsRepository = {
  async getEmployeeReport(): Promise<EmployeeReport> {
    await delay(200)
    return mockEmployeeReport
  },

  async getDepartmentCounts(): Promise<DepartmentCount[]> {
    await delay(200)
    return mockDepartmentCounts
  },

  async exportDepartmentCounts(): Promise<FileDownload> {
    await delay(300)
    const rows = [
      ['Department', 'Employee Count'],
      ...mockDepartmentCounts.map((d) => [d.departmentName, String(d.employeeCount)]),
    ]
    return { blob: toCsv(rows), fileName: 'departments.csv' }
  },

  async getLeaveSummary(filter: DateRangeFilter): Promise<LeaveSummaryReport> {
    await delay(250)
    assertValidRange(filter)
    return mockLeaveSummary
  },

  async getEmployeeTurnover(filter: DateRangeFilter): Promise<EmployeeJoinExit[]> {
    await delay(250)
    assertValidRange(filter)
    const from = new Date(filter.from).getTime()
    const to = new Date(filter.to).getTime()
    return mockEmployeeTurnover.filter((entry) => {
      const joined = new Date(entry.joinDate).getTime()
      const exited = entry.exitDate ? new Date(entry.exitDate).getTime() : null
      return (joined >= from && joined <= to) || (exited !== null && exited >= from && exited <= to)
    })
  },

  async exportEmployeeTurnover(filter: DateRangeFilter): Promise<FileDownload> {
    await delay(300)
    assertValidRange(filter)
    const rows = [
      ['Employee', 'Join Date', 'Exit Date'],
      ...mockEmployeeTurnover.map((e) => [e.employeeName, e.joinDate, e.exitDate ?? '']),
    ]
    return { blob: toCsv(rows), fileName: 'employee-turnover.csv' }
  },
}
