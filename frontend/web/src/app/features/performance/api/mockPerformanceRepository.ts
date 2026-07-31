import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
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
import { mockGoals, mockPromotions, mockReviews } from './mockData'

let goals = [...mockGoals]
let reviews = [...mockReviews]
let promotions = [...mockPromotions]

function nextId(): string {
  return `00000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

function nextNumber(prefix: string, count: number): string {
  return `${prefix}-${new Date().getFullYear()}-${String(count + 1).padStart(4, '0')}`
}

function paginate<T>(items: T[], page: number, pageSize: number): PagedResult<T> {
  const start = (page - 1) * pageSize
  return {
    data: items.slice(start, start + pageSize),
    page,
    pageSize,
    totalCount: items.length,
    totalPages: Math.max(1, Math.ceil(items.length / pageSize)),
    correlationId: 'mock-correlation-id',
  }
}

function findGoalOrThrow(id: string): PerformanceGoal {
  const goal = goals.find((g) => g.id === id)
  if (!goal) throw new AppError(`Goal ${id} was not found.`, 404, 'NOT_FOUND')
  return goal
}

function findReviewOrThrow(id: string): PerformanceReview {
  const review = reviews.find((r) => r.id === id)
  if (!review) throw new AppError(`Review ${id} was not found.`, 404, 'NOT_FOUND')
  return review
}

function findPromotionOrThrow(id: string): Promotion {
  const promotion = promotions.find((p) => p.id === id)
  if (!promotion) throw new AppError(`Promotion ${id} was not found.`, 404, 'NOT_FOUND')
  return promotion
}

export const mockPerformanceRepository: PerformanceRepository = {
  // Goals
  async listGoals(filters: GoalListFilters = {}): Promise<PagedResult<PerformanceGoal>> {
    await delay(300)
    const { page = 1, pageSize = 20, employeeId, status, category } = filters
    let filtered = goals.filter((g) => !g.isDeleted)
    if (employeeId) filtered = filtered.filter((g) => g.employeeId === employeeId)
    if (status) filtered = filtered.filter((g) => g.status === status)
    if (category) filtered = filtered.filter((g) => g.category?.toLowerCase() === category.toLowerCase())
    filtered = [...filtered].sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc))
    return paginate(filtered, page, pageSize)
  },

  async getGoalById(id: string): Promise<PerformanceGoal> {
    await delay(200)
    return findGoalOrThrow(id)
  },

  async createGoal(input: CreateGoalInput): Promise<{ id: string }> {
    await delay(300)
    const goal: PerformanceGoal = {
      id: nextId(),
      goalNumber: nextNumber('GOAL', goals.length),
      employeeId: input.employeeId,
      employeeName: null,
      title: input.title,
      description: input.description ?? null,
      category: input.category ?? null,
      startDate: input.startDate,
      targetDate: input.targetDate,
      weight: input.weight ?? null,
      status: 'NotStarted',
      progressPercent: 0,
      kpis: [],
      isDeleted: false,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    goals = [goal, ...goals]
    return { id: goal.id }
  },

  async updateGoal(id: string, input: UpdateGoalInput): Promise<void> {
    await delay(300)
    const existing = findGoalOrThrow(id)
    const updated: PerformanceGoal = {
      ...existing,
      title: input.title,
      description: input.description ?? null,
      category: input.category ?? null,
      targetDate: input.targetDate,
      weight: input.weight ?? null,
      status: input.status,
      updatedAtUtc: new Date().toISOString(),
    }
    goals = goals.map((g) => (g.id === id ? updated : g))
  },

  async updateGoalProgress(id: string, input: UpdateGoalProgressInput): Promise<void> {
    await delay(250)
    findGoalOrThrow(id)
    goals = goals.map((g) =>
      g.id === id ? { ...g, progressPercent: input.progressPercent, updatedAtUtc: new Date().toISOString() } : g,
    )
  },

  async removeGoal(id: string): Promise<void> {
    await delay(200)
    findGoalOrThrow(id)
    goals = goals.map((g) => (g.id === id ? { ...g, isDeleted: true } : g))
  },

  async restoreGoal(id: string): Promise<void> {
    await delay(200)
    goals = goals.map((g) => (g.id === id ? { ...g, isDeleted: false } : g))
  },

  async addGoalKpi(goalId: string, input: AddGoalKpiInput): Promise<{ id: string }> {
    await delay(250)
    findGoalOrThrow(goalId)
    const kpiId = nextId()
    const kpi = {
      id: kpiId,
      goalId,
      name: input.name,
      targetValue: input.targetValue,
      currentValue: 0,
      unit: input.unit ?? null,
      notes: input.notes ?? null,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    goals = goals.map((g) => (g.id === goalId ? { ...g, kpis: [...g.kpis, kpi] } : g))
    return { id: kpiId }
  },

  async updateGoalKpiProgress(kpiId: string, input: UpdateGoalKpiProgressInput): Promise<void> {
    await delay(250)
    goals = goals.map((g) => ({
      ...g,
      kpis: g.kpis.map((k) =>
        k.id === kpiId
          ? { ...k, currentValue: input.currentValue, notes: input.notes ?? k.notes, updatedAtUtc: new Date().toISOString() }
          : k,
      ),
    }))
  },

  // Reviews
  async listReviews(filters: ReviewListFilters = {}): Promise<PagedResult<PerformanceReview>> {
    await delay(300)
    const { page = 1, pageSize = 20, employeeId, reviewerEmployeeId, status } = filters
    let filtered = reviews.filter((r) => !r.isDeleted)
    if (employeeId) filtered = filtered.filter((r) => r.employeeId === employeeId)
    if (reviewerEmployeeId) filtered = filtered.filter((r) => r.reviewerEmployeeId === reviewerEmployeeId)
    if (status) filtered = filtered.filter((r) => r.status === status)
    filtered = [...filtered].sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc))
    return paginate(filtered, page, pageSize)
  },

  async getReviewById(id: string): Promise<PerformanceReview> {
    await delay(200)
    return findReviewOrThrow(id)
  },

  async createReview(input: CreateReviewInput): Promise<{ id: string }> {
    await delay(300)
    if (input.employeeId === input.reviewerEmployeeId) {
      throw new AppError('An employee cannot review themselves.', 409, 'CONFLICT')
    }
    const review: PerformanceReview = {
      id: nextId(),
      reviewNumber: nextNumber('REV', reviews.length),
      employeeId: input.employeeId,
      employeeName: null,
      reviewerEmployeeId: input.reviewerEmployeeId,
      reviewerName: null,
      reviewPeriodStart: input.reviewPeriodStart,
      reviewPeriodEnd: input.reviewPeriodEnd,
      status: 'Draft',
      selfAssessment: null,
      managerAssessment: null,
      overallRating: null,
      selfSubmittedAtUtc: null,
      completedAtUtc: null,
      notes: input.notes ?? null,
      isDeleted: false,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    reviews = [review, ...reviews]
    return { id: review.id }
  },

  async submitSelfAssessment(id: string, input: SubmitSelfAssessmentInput): Promise<void> {
    await delay(300)
    findReviewOrThrow(id)
    reviews = reviews.map((r) =>
      r.id === id
        ? {
            ...r,
            selfAssessment: input.selfAssessment,
            status: 'SelfAssessmentSubmitted',
            selfSubmittedAtUtc: new Date().toISOString(),
            updatedAtUtc: new Date().toISOString(),
          }
        : r,
    )
  },

  async submitManagerReview(id: string, input: SubmitManagerReviewInput): Promise<void> {
    await delay(300)
    findReviewOrThrow(id)
    reviews = reviews.map((r) =>
      r.id === id
        ? {
            ...r,
            managerAssessment: input.managerAssessment,
            overallRating: input.overallRating,
            status: 'Completed',
            completedAtUtc: new Date().toISOString(),
            updatedAtUtc: new Date().toISOString(),
          }
        : r,
    )
  },

  async cancelReview(id: string, input?: CancelReviewInput): Promise<void> {
    await delay(250)
    const existing = findReviewOrThrow(id)
    if (existing.status === 'Completed' || existing.status === 'Cancelled') {
      throw new AppError('This review can no longer be cancelled.', 409, 'CONFLICT')
    }
    reviews = reviews.map((r) =>
      r.id === id
        ? { ...r, status: 'Cancelled', notes: input?.reason ?? r.notes, updatedAtUtc: new Date().toISOString() }
        : r,
    )
  },

  async removeReview(id: string): Promise<void> {
    await delay(200)
    findReviewOrThrow(id)
    reviews = reviews.map((r) => (r.id === id ? { ...r, isDeleted: true } : r))
  },

  async restoreReview(id: string): Promise<void> {
    await delay(200)
    reviews = reviews.map((r) => (r.id === id ? { ...r, isDeleted: false } : r))
  },

  // Promotions
  async listPromotions(filters: PromotionListFilters = {}): Promise<PagedResult<Promotion>> {
    await delay(300)
    const { page = 1, pageSize = 20, employeeId, status } = filters
    let filtered = promotions.filter((p) => !p.isDeleted)
    if (employeeId) filtered = filtered.filter((p) => p.employeeId === employeeId)
    if (status) filtered = filtered.filter((p) => p.status === status)
    filtered = [...filtered].sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc))
    return paginate(filtered, page, pageSize)
  },

  async getPromotionById(id: string): Promise<Promotion> {
    await delay(200)
    return findPromotionOrThrow(id)
  },

  async proposePromotion(input: ProposePromotionInput): Promise<{ id: string }> {
    await delay(300)
    const promotion: Promotion = {
      id: nextId(),
      promotionNumber: nextNumber('PROMO', promotions.length),
      employeeId: input.employeeId,
      employeeName: null,
      fromDesignationId: '00000000-0000-0000-0000-000000000401',
      fromDesignationName: null,
      toDesignationId: input.toDesignationId,
      toDesignationName: null,
      fromDepartmentId: null,
      fromDepartmentName: null,
      toDepartmentId: input.toDepartmentId ?? null,
      toDepartmentName: null,
      effectiveDate: input.effectiveDate,
      reason: input.reason,
      status: 'Proposed',
      proposedByUserId: '00000000-0000-0000-0000-000000000001',
      decidedByUserId: null,
      decidedAtUtc: null,
      decisionNotes: null,
      appliedAtUtc: null,
      isDeleted: false,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    promotions = [promotion, ...promotions]
    return { id: promotion.id }
  },

  async approvePromotion(id: string, input?: PromotionDecisionInput): Promise<void> {
    await delay(300)
    const existing = findPromotionOrThrow(id)
    if (existing.status !== 'Proposed') {
      throw new AppError('Only a proposed promotion can be approved.', 409, 'CONFLICT')
    }
    const now = new Date()
    const appliesImmediately = new Date(existing.effectiveDate) <= now
    promotions = promotions.map((p) =>
      p.id === id
        ? {
            ...p,
            status: 'Approved',
            decidedByUserId: '00000000-0000-0000-0000-000000000001',
            decidedAtUtc: now.toISOString(),
            decisionNotes: input?.decisionNotes ?? null,
            appliedAtUtc: appliesImmediately ? now.toISOString() : null,
            updatedAtUtc: now.toISOString(),
          }
        : p,
    )
  },

  async rejectPromotion(id: string, input?: PromotionDecisionInput): Promise<void> {
    await delay(300)
    const existing = findPromotionOrThrow(id)
    if (existing.status !== 'Proposed') {
      throw new AppError('Only a proposed promotion can be rejected.', 409, 'CONFLICT')
    }
    promotions = promotions.map((p) =>
      p.id === id
        ? {
            ...p,
            status: 'Rejected',
            decidedByUserId: '00000000-0000-0000-0000-000000000001',
            decidedAtUtc: new Date().toISOString(),
            decisionNotes: input?.decisionNotes ?? null,
            updatedAtUtc: new Date().toISOString(),
          }
        : p,
    )
  },

  async withdrawPromotion(id: string): Promise<void> {
    await delay(250)
    const existing = findPromotionOrThrow(id)
    if (existing.status !== 'Proposed') {
      throw new AppError('Only a proposed promotion can be withdrawn.', 409, 'CONFLICT')
    }
    promotions = promotions.map((p) => (p.id === id ? { ...p, status: 'Withdrawn', updatedAtUtc: new Date().toISOString() } : p))
  },

  async removePromotion(id: string): Promise<void> {
    await delay(200)
    findPromotionOrThrow(id)
    promotions = promotions.map((p) => (p.id === id ? { ...p, isDeleted: true } : p))
  },

  async restorePromotion(id: string): Promise<void> {
    await delay(200)
    promotions = promotions.map((p) => (p.id === id ? { ...p, isDeleted: false } : p))
  },
}
