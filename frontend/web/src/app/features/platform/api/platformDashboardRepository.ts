import type { Company } from './companyRepository'

/** Contract for GET /platform/dashboard/summary (docs/api-specification.md §27.5). */
export interface PlatformDashboardSummary {
  totalCompanies: number
  activeCompanies: number
  suspendedCompanies: number
  trialCompanies: number
  totalEmployeesAcrossAllCompanies: number
  recentRegistrations: Company[]
}

export interface PlatformDashboardRepository {
  getSummary(recentCount?: number): Promise<PlatformDashboardSummary>
}
