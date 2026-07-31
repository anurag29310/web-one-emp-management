import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { ApiSuccessEnvelope, PagedResult } from '@/app/shared/models/apiEnvelope'
import type {
  AddGoalKpiInput,
  CancelReviewInput,
  CreateGoalInput,
  CreateReviewInput,
  GoalListFilters,
  PerformanceGoal,
  PerformanceRepository,
  PerformanceReview,
  Promotion,
  PromotionDecisionInput,
  PromotionListFilters,
  ProposePromotionInput,
  ReviewListFilters,
  SubmitManagerReviewInput,
  SubmitSelfAssessmentInput,
  UpdateGoalInput,
  UpdateGoalKpiProgressInput,
  UpdateGoalProgressInput,
} from './performanceRepository'

/**
 * Backend wraps EMS.Application.Common.DTOs.PagedResult<T> a second time inside ApiResponse<T>
 * (see attendance/audit-logs repositories for the same shape) — pagination fields live one
 * level deeper than the flat shape documented in api-specification.md §2.3.
 */
interface BackendPagedResult<T> {
  data: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

function unwrapPaged<T>(response: { data: ApiSuccessEnvelope<BackendPagedResult<T>> }): PagedResult<T> {
  const envelope = response.data
  const paged = envelope.data
  return {
    data: paged.data,
    page: paged.page,
    pageSize: paged.pageSize,
    totalCount: paged.totalCount,
    totalPages: paged.totalPages,
    correlationId: envelope.correlationId,
  }
}

export const apiPerformanceRepository: PerformanceRepository = {
  // Goals
  async listGoals(filters: GoalListFilters = {}): Promise<PagedResult<PerformanceGoal>> {
    const response = await httpClient.get<ApiSuccessEnvelope<BackendPagedResult<PerformanceGoal>>>('/goals', {
      params: filters,
    })
    return unwrapPaged(response)
  },

  async getGoalById(id: string): Promise<PerformanceGoal> {
    const response = await httpClient.get<{ data: PerformanceGoal }>(`/goals/${id}`)
    return unwrap(response)
  },

  async createGoal(input: CreateGoalInput): Promise<{ id: string }> {
    const response = await httpClient.post<{ data: { id: string } }>('/goals', input)
    return unwrap(response)
  },

  async updateGoal(id: string, input: UpdateGoalInput): Promise<void> {
    await httpClient.put(`/goals/${id}`, { id, ...input })
  },

  async updateGoalProgress(id: string, input: UpdateGoalProgressInput): Promise<void> {
    await httpClient.post(`/goals/${id}/progress`, input)
  },

  async removeGoal(id: string): Promise<void> {
    await httpClient.delete(`/goals/${id}`)
  },

  async restoreGoal(id: string): Promise<void> {
    await httpClient.post(`/goals/${id}/restore`)
  },

  async addGoalKpi(goalId: string, input: AddGoalKpiInput): Promise<{ id: string }> {
    const response = await httpClient.post<{ data: string }>(`/goals/${goalId}/kpis`, input)
    return { id: unwrap(response) }
  },

  async updateGoalKpiProgress(kpiId: string, input: UpdateGoalKpiProgressInput): Promise<void> {
    await httpClient.post(`/kpis/${kpiId}/progress`, input)
  },

  // Reviews
  async listReviews(filters: ReviewListFilters = {}): Promise<PagedResult<PerformanceReview>> {
    const response = await httpClient.get<ApiSuccessEnvelope<BackendPagedResult<PerformanceReview>>>('/reviews', {
      params: filters,
    })
    return unwrapPaged(response)
  },

  async getReviewById(id: string): Promise<PerformanceReview> {
    const response = await httpClient.get<{ data: PerformanceReview }>(`/reviews/${id}`)
    return unwrap(response)
  },

  async createReview(input: CreateReviewInput): Promise<{ id: string }> {
    const response = await httpClient.post<{ data: { id: string } }>('/reviews', input)
    return unwrap(response)
  },

  async submitSelfAssessment(id: string, input: SubmitSelfAssessmentInput): Promise<void> {
    await httpClient.post(`/reviews/${id}/self-assessment`, input)
  },

  async submitManagerReview(id: string, input: SubmitManagerReviewInput): Promise<void> {
    await httpClient.post(`/reviews/${id}/manager-review`, input)
  },

  async cancelReview(id: string, input?: CancelReviewInput): Promise<void> {
    await httpClient.post(`/reviews/${id}/cancel`, input ?? {})
  },

  async removeReview(id: string): Promise<void> {
    await httpClient.delete(`/reviews/${id}`)
  },

  async restoreReview(id: string): Promise<void> {
    await httpClient.post(`/reviews/${id}/restore`)
  },

  // Promotions
  async listPromotions(filters: PromotionListFilters = {}): Promise<PagedResult<Promotion>> {
    const response = await httpClient.get<ApiSuccessEnvelope<BackendPagedResult<Promotion>>>('/promotions', {
      params: filters,
    })
    return unwrapPaged(response)
  },

  async getPromotionById(id: string): Promise<Promotion> {
    const response = await httpClient.get<{ data: Promotion }>(`/promotions/${id}`)
    return unwrap(response)
  },

  async proposePromotion(input: ProposePromotionInput): Promise<{ id: string }> {
    const response = await httpClient.post<{ data: { id: string } }>('/promotions', input)
    return unwrap(response)
  },

  async approvePromotion(id: string, input?: PromotionDecisionInput): Promise<void> {
    await httpClient.post(`/promotions/${id}/approve`, input ?? {})
  },

  async rejectPromotion(id: string, input?: PromotionDecisionInput): Promise<void> {
    await httpClient.post(`/promotions/${id}/reject`, input ?? {})
  },

  async withdrawPromotion(id: string): Promise<void> {
    await httpClient.post(`/promotions/${id}/withdraw`)
  },

  async removePromotion(id: string): Promise<void> {
    await httpClient.delete(`/promotions/${id}`)
  },

  async restorePromotion(id: string): Promise<void> {
    await httpClient.post(`/promotions/${id}/restore`)
  },
}
