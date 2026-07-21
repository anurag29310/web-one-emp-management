import { delay } from '@/app/shared/utils/delay'
import type { DashboardRepository } from './dashboardRepository'
import { mockDashboardSummary } from './mockData'

export const mockDashboardRepository: DashboardRepository = {
  async getSummary() {
    await delay(300)
    return mockDashboardSummary
  },
}
