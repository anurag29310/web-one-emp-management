import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { DashboardSummary, DashboardSummaryFilters } from '@/app/shared/models/dashboard'
import type { DashboardRepository } from './dashboardRepository'

export const apiDashboardRepository: DashboardRepository = {
  async getSummary(filters?: DashboardSummaryFilters): Promise<DashboardSummary> {
    const response = await httpClient.get<{ data: DashboardSummary }>('/dashboard/summary', {
      params: filters,
    })
    return unwrap(response)
  },
}
