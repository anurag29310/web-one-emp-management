import type { PagedResult } from '@/app/shared/models/apiEnvelope'

// ─── Goals ───────────────────────────────────────────────────────────────────

export type GoalStatus = 'NotStarted' | 'InProgress' | 'Completed' | 'Cancelled'

export interface PerformanceGoalKpi {
  id: string
  goalId: string
  name: string
  targetValue: number
  currentValue: number
  unit: string | null
  notes: string | null
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface PerformanceGoal {
  id: string
  goalNumber: string
  employeeId: string
  employeeName: string | null
  title: string
  description: string | null
  category: string | null
  startDate: string
  targetDate: string
  weight: number | null
  status: GoalStatus
  progressPercent: number
  kpis: PerformanceGoalKpi[]
  isDeleted: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface GoalListFilters {
  page?: number
  pageSize?: number
  employeeId?: string
  status?: GoalStatus
  category?: string
}

export interface CreateGoalInput {
  employeeId: string
  title: string
  description?: string
  category?: string
  startDate: string
  targetDate: string
  weight?: number
}

export interface UpdateGoalInput {
  title: string
  description?: string
  category?: string
  targetDate: string
  weight?: number
  status: GoalStatus
}

export interface UpdateGoalProgressInput {
  progressPercent: number
}

export interface AddGoalKpiInput {
  name: string
  targetValue: number
  unit?: string
  notes?: string
}

export interface UpdateGoalKpiProgressInput {
  currentValue: number
  notes?: string
}

// ─── Performance Reviews ─────────────────────────────────────────────────────

export type ReviewStatus = 'Draft' | 'SelfAssessmentSubmitted' | 'Completed' | 'Cancelled'

export interface PerformanceReview {
  id: string
  reviewNumber: string
  employeeId: string
  employeeName: string | null
  reviewerEmployeeId: string
  reviewerName: string | null
  reviewPeriodStart: string
  reviewPeriodEnd: string
  status: ReviewStatus
  selfAssessment: string | null
  managerAssessment: string | null
  overallRating: number | null
  selfSubmittedAtUtc: string | null
  completedAtUtc: string | null
  notes: string | null
  isDeleted: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface ReviewListFilters {
  page?: number
  pageSize?: number
  employeeId?: string
  reviewerEmployeeId?: string
  status?: ReviewStatus
}

export interface CreateReviewInput {
  employeeId: string
  reviewerEmployeeId: string
  reviewPeriodStart: string
  reviewPeriodEnd: string
  notes?: string
}

export interface SubmitSelfAssessmentInput {
  selfAssessment: string
}

export interface SubmitManagerReviewInput {
  managerAssessment: string
  overallRating: number
}

export interface CancelReviewInput {
  reason?: string
}

// ─── Promotions ───────────────────────────────────────────────────────────────

export type PromotionStatus = 'Proposed' | 'Approved' | 'Rejected' | 'Withdrawn'

export interface Promotion {
  id: string
  promotionNumber: string
  employeeId: string
  employeeName: string | null
  fromDesignationId: string
  fromDesignationName: string | null
  toDesignationId: string
  toDesignationName: string | null
  fromDepartmentId: string | null
  fromDepartmentName: string | null
  toDepartmentId: string | null
  toDepartmentName: string | null
  effectiveDate: string
  reason: string
  status: PromotionStatus
  proposedByUserId: string
  decidedByUserId: string | null
  decidedAtUtc: string | null
  decisionNotes: string | null
  appliedAtUtc: string | null
  isDeleted: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface PromotionListFilters {
  page?: number
  pageSize?: number
  employeeId?: string
  status?: PromotionStatus
}

export interface ProposePromotionInput {
  employeeId: string
  toDesignationId: string
  toDepartmentId?: string
  effectiveDate: string
  reason: string
}

export interface PromotionDecisionInput {
  decisionNotes?: string
}

// ─── Repository ───────────────────────────────────────────────────────────────

export interface PerformanceRepository {
  // Goals
  listGoals(filters?: GoalListFilters): Promise<PagedResult<PerformanceGoal>>
  getGoalById(id: string): Promise<PerformanceGoal>
  createGoal(input: CreateGoalInput): Promise<{ id: string }>
  updateGoal(id: string, input: UpdateGoalInput): Promise<void>
  updateGoalProgress(id: string, input: UpdateGoalProgressInput): Promise<void>
  removeGoal(id: string): Promise<void>
  restoreGoal(id: string): Promise<void>
  addGoalKpi(goalId: string, input: AddGoalKpiInput): Promise<{ id: string }>
  updateGoalKpiProgress(kpiId: string, input: UpdateGoalKpiProgressInput): Promise<void>

  // Reviews
  listReviews(filters?: ReviewListFilters): Promise<PagedResult<PerformanceReview>>
  getReviewById(id: string): Promise<PerformanceReview>
  createReview(input: CreateReviewInput): Promise<{ id: string }>
  submitSelfAssessment(id: string, input: SubmitSelfAssessmentInput): Promise<void>
  submitManagerReview(id: string, input: SubmitManagerReviewInput): Promise<void>
  cancelReview(id: string, input?: CancelReviewInput): Promise<void>
  removeReview(id: string): Promise<void>
  restoreReview(id: string): Promise<void>

  // Promotions
  listPromotions(filters?: PromotionListFilters): Promise<PagedResult<Promotion>>
  getPromotionById(id: string): Promise<Promotion>
  proposePromotion(input: ProposePromotionInput): Promise<{ id: string }>
  approvePromotion(id: string, input?: PromotionDecisionInput): Promise<void>
  rejectPromotion(id: string, input?: PromotionDecisionInput): Promise<void>
  withdrawPromotion(id: string): Promise<void>
  removePromotion(id: string): Promise<void>
  restorePromotion(id: string): Promise<void>
}
