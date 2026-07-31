import { selectRepository } from '@/app/core/config/selectRepository'
import { mockPerformanceRepository } from './mockPerformanceRepository'
import { apiPerformanceRepository } from './apiPerformanceRepository'
import type { PerformanceRepository } from './performanceRepository'

export const performanceRepository: PerformanceRepository = selectRepository({
  mock: mockPerformanceRepository,
  api: apiPerformanceRepository,
})

export type {
  AddGoalKpiInput,
  CancelReviewInput,
  CreateGoalInput,
  CreateReviewInput,
  GoalListFilters,
  GoalStatus,
  PerformanceGoal,
  PerformanceGoalKpi,
  PerformanceRepository,
  PerformanceReview,
  Promotion,
  PromotionDecisionInput,
  PromotionListFilters,
  PromotionStatus,
  ProposePromotionInput,
  ReviewListFilters,
  ReviewStatus,
  SubmitManagerReviewInput,
  SubmitSelfAssessmentInput,
  UpdateGoalInput,
  UpdateGoalKpiProgressInput,
  UpdateGoalProgressInput,
} from './performanceRepository'
