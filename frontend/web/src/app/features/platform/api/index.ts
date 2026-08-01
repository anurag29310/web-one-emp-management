import { selectRepository } from '@/app/core/config/selectRepository'
import { mockCompanyRepository } from './mockCompanyRepository'
import { apiCompanyRepository } from './apiCompanyRepository'
import type { PlatformCompanyRepository } from './companyRepository'
import { mockPlatformDashboardRepository } from './mockPlatformDashboardRepository'
import { apiPlatformDashboardRepository } from './apiPlatformDashboardRepository'
import type { PlatformDashboardRepository } from './platformDashboardRepository'
import { mockPlatformSettingsRepository } from './mockPlatformSettingsRepository'
import { apiPlatformSettingsRepository } from './apiPlatformSettingsRepository'
import type { PlatformSettingsRepository } from './platformSettingsRepository'
import { mockPlatformAuditLogRepository } from './mockPlatformAuditLogRepository'
import { apiPlatformAuditLogRepository } from './apiPlatformAuditLogRepository'
import type { PlatformAuditLogRepository } from './platformAuditLogRepository'

export const companyRepository: PlatformCompanyRepository = selectRepository({
  mock: mockCompanyRepository,
  api: apiCompanyRepository,
})

export const platformDashboardRepository: PlatformDashboardRepository = selectRepository({
  mock: mockPlatformDashboardRepository,
  api: apiPlatformDashboardRepository,
})

export const platformSettingsRepository: PlatformSettingsRepository = selectRepository({
  mock: mockPlatformSettingsRepository,
  api: apiPlatformSettingsRepository,
})

export const platformAuditLogRepository: PlatformAuditLogRepository = selectRepository({
  mock: mockPlatformAuditLogRepository,
  api: apiPlatformAuditLogRepository,
})

export type {
  Company,
  CompanyAdmin,
  CompanyCreateInput,
  CompanyDetail,
  CompanyListFilters,
  CompanyStatus,
  CompanyUpdateInput,
  PlatformCompanyRepository,
} from './companyRepository'
export type { PlatformDashboardRepository, PlatformDashboardSummary } from './platformDashboardRepository'
export type { PlatformSettings, PlatformSettingsRepository } from './platformSettingsRepository'
export type {
  PlatformAuditLogFilters,
  PlatformAuditLogRepository,
} from './platformAuditLogRepository'
