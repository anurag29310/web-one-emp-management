import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { PerformanceRepository } from './performanceRepository'

// The mock repository holds its "database" as module-level mutable state, so each test
// needs a fresh module instance — otherwise mutations from one test would leak into the next.
async function loadRepository(): Promise<PerformanceRepository> {
  const module = await import('./mockPerformanceRepository')
  return module.mockPerformanceRepository
}

beforeEach(() => {
  vi.resetModules()
})

const GOAL_IN_PROGRESS_ID = 'a0000000-0000-0000-0000-000000000001'
const GOAL_COMPLETED_ID = 'a0000000-0000-0000-0000-000000000003'
const REVIEW_DRAFT_ID = 'c0000000-0000-0000-0000-000000000003'
const REVIEW_SELF_SUBMITTED_ID = 'c0000000-0000-0000-0000-000000000001'
const REVIEW_COMPLETED_ID = 'c0000000-0000-0000-0000-000000000002'
const PROMOTION_PROPOSED_ID = 'd0000000-0000-0000-0000-000000000001'
const PROMOTION_APPROVED_ID = 'd0000000-0000-0000-0000-000000000002'

describe('mockPerformanceRepository goals', () => {
  it('excludes soft-deleted goals from list', async () => {
    const repository = await loadRepository()
    const result = await repository.listGoals()
    expect(result.data.every((goal) => !goal.isDeleted)).toBe(true)
  })

  it('filters goals by status', async () => {
    const repository = await loadRepository()
    const result = await repository.listGoals({ status: 'Completed' })
    expect(result.data.length).toBeGreaterThan(0)
    expect(result.data.every((goal) => goal.status === 'Completed')).toBe(true)
  })

  it('throws a 404 AppError for an unknown goal id', async () => {
    const repository = await loadRepository()
    await expect(repository.getGoalById('does-not-exist')).rejects.toMatchObject({ status: 404, code: 'NOT_FOUND' })
  })

  it('creates a goal starting at NotStarted with zero progress', async () => {
    const repository = await loadRepository()
    const { id } = await repository.createGoal({
      employeeId: 'employee-1',
      title: 'New goal',
      startDate: '2026-01-01',
      targetDate: '2026-06-30',
    })
    const goal = await repository.getGoalById(id)
    expect(goal.status).toBe('NotStarted')
    expect(goal.progressPercent).toBe(0)
  })

  it('updates goal progress', async () => {
    const repository = await loadRepository()
    await repository.updateGoalProgress(GOAL_IN_PROGRESS_ID, { progressPercent: 80 })
    const goal = await repository.getGoalById(GOAL_IN_PROGRESS_ID)
    expect(goal.progressPercent).toBe(80)
  })

  it('adds a KPI and records progress against it', async () => {
    const repository = await loadRepository()
    const { id: kpiId } = await repository.addGoalKpi(GOAL_IN_PROGRESS_ID, {
      name: 'Deploys per week',
      targetValue: 10,
    })
    await repository.updateGoalKpiProgress(kpiId, { currentValue: 4 })

    const goal = await repository.getGoalById(GOAL_IN_PROGRESS_ID)
    const kpi = goal.kpis.find((k) => k.id === kpiId)
    expect(kpi?.currentValue).toBe(4)
  })

  it('soft-deletes a goal and restore brings it back', async () => {
    const repository = await loadRepository()
    await repository.removeGoal(GOAL_COMPLETED_ID)
    const afterDelete = await repository.listGoals()
    expect(afterDelete.data.some((g) => g.id === GOAL_COMPLETED_ID)).toBe(false)

    await repository.restoreGoal(GOAL_COMPLETED_ID)
    const afterRestore = await repository.listGoals()
    expect(afterRestore.data.some((g) => g.id === GOAL_COMPLETED_ID)).toBe(true)
  })
})

describe('mockPerformanceRepository reviews', () => {
  it('rejects an employee reviewing themselves', async () => {
    const repository = await loadRepository()
    await expect(
      repository.createReview({
        employeeId: 'employee-1',
        reviewerEmployeeId: 'employee-1',
        reviewPeriodStart: '2026-01-01',
        reviewPeriodEnd: '2026-06-30',
      }),
    ).rejects.toMatchObject({ status: 409 })
  })

  it('moves Draft to SelfAssessmentSubmitted on self-assessment', async () => {
    const repository = await loadRepository()
    await repository.submitSelfAssessment(REVIEW_DRAFT_ID, { selfAssessment: 'Did great work this cycle.' })
    const review = await repository.getReviewById(REVIEW_DRAFT_ID)
    expect(review.status).toBe('SelfAssessmentSubmitted')
    expect(review.selfSubmittedAtUtc).not.toBeNull()
  })

  it('completes a review on manager submission', async () => {
    const repository = await loadRepository()
    await repository.submitManagerReview(REVIEW_SELF_SUBMITTED_ID, {
      managerAssessment: 'Strong quarter, exceeded targets.',
      overallRating: 5,
    })
    const review = await repository.getReviewById(REVIEW_SELF_SUBMITTED_ID)
    expect(review.status).toBe('Completed')
    expect(review.overallRating).toBe(5)
    expect(review.completedAtUtc).not.toBeNull()
  })

  it('rejects cancelling a review that already completed', async () => {
    const repository = await loadRepository()
    await expect(repository.cancelReview(REVIEW_COMPLETED_ID)).rejects.toMatchObject({ status: 409 })
  })

  it('cancels a review still in progress', async () => {
    const repository = await loadRepository()
    await repository.cancelReview(REVIEW_DRAFT_ID, { reason: 'Employee left the team.' })
    const review = await repository.getReviewById(REVIEW_DRAFT_ID)
    expect(review.status).toBe('Cancelled')
  })
})

describe('mockPerformanceRepository promotions', () => {
  it('approves a proposed promotion and stamps the decision', async () => {
    const repository = await loadRepository()
    await repository.approvePromotion(PROMOTION_PROPOSED_ID, { decisionNotes: 'Well deserved.' })
    const promotion = await repository.getPromotionById(PROMOTION_PROPOSED_ID)
    expect(promotion.status).toBe('Approved')
    expect(promotion.decidedAtUtc).not.toBeNull()
  })

  it('defers appliedAtUtc when the effective date is in the future', async () => {
    const repository = await loadRepository()
    const farFutureDate = new Date(Date.now() + 1000 * 60 * 60 * 24 * 365 * 10).toISOString()
    const { id } = await repository.proposePromotion({
      employeeId: 'employee-1',
      toDesignationId: 'designation-1',
      effectiveDate: farFutureDate,
      reason: 'Future-dated promotion for deferred-apply coverage.',
    })

    await repository.approvePromotion(id)
    const approved = await repository.getPromotionById(id)
    expect(approved.appliedAtUtc).toBeNull()
  })

  it('applies immediately when the effective date has already passed', async () => {
    const repository = await loadRepository()
    const pastDate = new Date(Date.now() - 1000 * 60 * 60 * 24).toISOString()
    const { id } = await repository.proposePromotion({
      employeeId: 'employee-1',
      toDesignationId: 'designation-1',
      effectiveDate: pastDate,
      reason: 'Past-dated promotion for immediate-apply coverage.',
    })

    await repository.approvePromotion(id)
    const approved = await repository.getPromotionById(id)
    expect(approved.appliedAtUtc).not.toBeNull()
  })

  it('rejects approving a promotion that already has a decision', async () => {
    const repository = await loadRepository()
    await expect(repository.approvePromotion(PROMOTION_APPROVED_ID)).rejects.toMatchObject({ status: 409 })
  })

  it('rejects withdrawing a promotion that already has a decision', async () => {
    const repository = await loadRepository()
    await expect(repository.withdrawPromotion(PROMOTION_APPROVED_ID)).rejects.toMatchObject({ status: 409 })
  })

  it('withdraws a proposed promotion', async () => {
    const repository = await loadRepository()
    await repository.withdrawPromotion(PROMOTION_PROPOSED_ID)
    const promotion = await repository.getPromotionById(PROMOTION_PROPOSED_ID)
    expect(promotion.status).toBe('Withdrawn')
  })
})
