import type { DashboardSummary, DashboardSummaryFilters } from '@/app/shared/models/dashboard'

export interface DashboardRepository {
  getSummary(filters?: DashboardSummaryFilters): Promise<DashboardSummary>
}
