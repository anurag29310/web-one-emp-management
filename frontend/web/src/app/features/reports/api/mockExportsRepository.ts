import { delay } from '@/app/shared/utils/delay'
import type { ExportsRepository, FileDownload } from './exportsRepository'

function mockCsv(title: string): Blob {
  return new Blob([`"${title}"\r\n"Generated in mock mode — no live data source configured."\r\n`], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  })
}

function mockPdf(title: string): Blob {
  return new Blob([`%PDF mock export: ${title}`], { type: 'application/pdf' })
}

export const mockExportsRepository: ExportsRepository = {
  async exportEmployees(): Promise<FileDownload> {
    await delay(300)
    return { blob: mockCsv('Employees'), fileName: 'employees.xlsx' }
  },

  async exportAttendance(): Promise<FileDownload> {
    await delay(300)
    return { blob: mockCsv('Attendance'), fileName: 'attendance.xlsx' }
  },

  async exportLeaveRequests(): Promise<FileDownload> {
    await delay(300)
    return { blob: mockCsv('Leave Requests'), fileName: 'leave-requests.xlsx' }
  },

  async exportDashboardSummary(): Promise<FileDownload> {
    await delay(300)
    return { blob: mockPdf('Dashboard Summary'), fileName: 'dashboard-summary.pdf' }
  },
}
