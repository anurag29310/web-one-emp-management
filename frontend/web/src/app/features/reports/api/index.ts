import { selectRepository } from '@/app/core/config/selectRepository'
import { mockReportsRepository } from './mockReportsRepository'
import { apiReportsRepository } from './apiReportsRepository'
import { mockExportsRepository } from './mockExportsRepository'
import { apiExportsRepository } from './apiExportsRepository'
import type { ReportsRepository } from './reportsRepository'
import type { ExportsRepository } from './exportsRepository'

export const reportsRepository: ReportsRepository = selectRepository({
  mock: mockReportsRepository,
  api: apiReportsRepository,
})

export const exportsRepository: ExportsRepository = selectRepository({
  mock: mockExportsRepository,
  api: apiExportsRepository,
})

export type {
  DateRangeFilter,
  DepartmentCount,
  EmployeeJoinExit,
  EmployeeReport,
  FileDownload,
  LeaveSummaryReport,
  ReportsRepository,
} from './reportsRepository'
export type {
  AttendanceExportFilters,
  DashboardSummaryExportFilters,
  EmployeeExportFilters,
  ExportsRepository,
  LeaveRequestExportFilters,
} from './exportsRepository'
