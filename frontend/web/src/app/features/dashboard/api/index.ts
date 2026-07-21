import { selectRepository } from '@/app/core/config/selectRepository'
import { mockDashboardRepository } from './mockDashboardRepository'
import { apiDashboardRepository } from './apiDashboardRepository'
import type { DashboardRepository } from './dashboardRepository'

export const dashboardRepository: DashboardRepository = selectRepository({
  mock: mockDashboardRepository,
  api: apiDashboardRepository,
})

export type { DashboardRepository } from './dashboardRepository'
