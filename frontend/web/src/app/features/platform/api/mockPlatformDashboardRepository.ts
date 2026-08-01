import { delay } from '@/app/shared/utils/delay'
import type { PlatformDashboardRepository, PlatformDashboardSummary } from './platformDashboardRepository'
import { mockCompanies, mockCompanyEmployeeCounts } from './mockData'

export const mockPlatformDashboardRepository: PlatformDashboardRepository = {
  async getSummary(recentCount = 5): Promise<PlatformDashboardSummary> {
    await delay(250)
    const companies = mockCompanies.filter((c) => !c.isDeleted)
    const totalEmployeesAcrossAllCompanies = companies.reduce(
      (sum, c) => sum + (mockCompanyEmployeeCounts[c.id] ?? 0),
      0,
    )
    const recentRegistrations = [...companies]
      .sort((a, b) => b.registeredAtUtc.localeCompare(a.registeredAtUtc))
      .slice(0, recentCount)

    return {
      totalCompanies: companies.length,
      activeCompanies: companies.filter((c) => c.status === 'Active').length,
      suspendedCompanies: companies.filter((c) => c.status === 'Suspended').length,
      trialCompanies: companies.filter((c) => c.status === 'Trial').length,
      totalEmployeesAcrossAllCompanies,
      recentRegistrations,
    }
  },
}
