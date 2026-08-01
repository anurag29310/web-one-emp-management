import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { PlatformDashboardRepository, PlatformDashboardSummary } from './platformDashboardRepository'

export const apiPlatformDashboardRepository: PlatformDashboardRepository = {
  async getSummary(recentCount?: number): Promise<PlatformDashboardSummary> {
    const response = await httpClient.get<{ data: PlatformDashboardSummary }>('/platform/dashboard/summary', {
      params: recentCount ? { recentCount } : undefined,
    })
    return unwrap(response)
  },
}
