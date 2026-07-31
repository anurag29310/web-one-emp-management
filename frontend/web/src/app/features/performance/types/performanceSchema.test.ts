import { describe, expect, it } from 'vitest'
import {
  addGoalKpiFormSchema,
  cancelReviewFormSchema,
  createGoalFormSchema,
  createReviewFormSchema,
  goalProgressFormSchema,
  managerReviewFormSchema,
  proposePromotionFormSchema,
  selfAssessmentFormSchema,
  updateGoalFormSchema,
} from './performanceSchema'

describe('createGoalFormSchema', () => {
  const validInput = {
    employeeId: 'employee-1',
    title: 'Ship the v2 onboarding flow',
    description: '',
    category: 'Delivery',
    startDate: '2026-01-01',
    targetDate: '2026-06-30',
  }

  it('accepts a fully valid goal', () => {
    expect(createGoalFormSchema.safeParse(validInput).success).toBe(true)
  })

  it('rejects a blank employee, title, start date, or target date', () => {
    for (const field of ['employeeId', 'title', 'startDate', 'targetDate'] as const) {
      expect(createGoalFormSchema.safeParse({ ...validInput, [field]: '' }).success).toBe(false)
    }
  })

  it('rejects a target date before the start date', () => {
    const result = createGoalFormSchema.safeParse({ ...validInput, targetDate: '2025-12-31' })
    expect(result.success).toBe(false)
  })

  it('coerces a string weight and rejects out-of-range values', () => {
    const coerced = createGoalFormSchema.safeParse({ ...validInput, weight: '50' })
    expect(coerced.success).toBe(true)
    if (coerced.success) expect(coerced.data.weight).toBe(50)

    expect(createGoalFormSchema.safeParse({ ...validInput, weight: 150 }).success).toBe(false)
    expect(createGoalFormSchema.safeParse({ ...validInput, weight: -1 }).success).toBe(false)
  })
})

describe('updateGoalFormSchema', () => {
  it('requires a valid GoalStatus enum value', () => {
    const base = { title: 'Goal', targetDate: '2026-06-30', status: 'InProgress' }
    expect(updateGoalFormSchema.safeParse(base).success).toBe(true)
    expect(updateGoalFormSchema.safeParse({ ...base, status: 'NotAStatus' }).success).toBe(false)
  })
})

describe('goalProgressFormSchema', () => {
  it('accepts values between 0 and 100 and rejects out-of-range values', () => {
    expect(goalProgressFormSchema.safeParse({ progressPercent: 0 }).success).toBe(true)
    expect(goalProgressFormSchema.safeParse({ progressPercent: 100 }).success).toBe(true)
    expect(goalProgressFormSchema.safeParse({ progressPercent: 101 }).success).toBe(false)
    expect(goalProgressFormSchema.safeParse({ progressPercent: -1 }).success).toBe(false)
  })
})

describe('addGoalKpiFormSchema', () => {
  it('requires a name and a non-negative target value', () => {
    expect(addGoalKpiFormSchema.safeParse({ name: '', targetValue: 10 }).success).toBe(false)
    expect(addGoalKpiFormSchema.safeParse({ name: 'Uptime', targetValue: -1 }).success).toBe(false)
    expect(addGoalKpiFormSchema.safeParse({ name: 'Uptime', targetValue: 99.9 }).success).toBe(true)
  })
})

describe('createReviewFormSchema', () => {
  const validInput = {
    employeeId: 'employee-1',
    reviewerEmployeeId: 'employee-2',
    reviewPeriodStart: '2026-01-01',
    reviewPeriodEnd: '2026-06-30',
  }

  it('accepts a valid review cycle', () => {
    expect(createReviewFormSchema.safeParse(validInput).success).toBe(true)
  })

  it('rejects an employee reviewing themselves', () => {
    const result = createReviewFormSchema.safeParse({ ...validInput, reviewerEmployeeId: validInput.employeeId })
    expect(result.success).toBe(false)
  })

  it('rejects a review period end before its start', () => {
    const result = createReviewFormSchema.safeParse({ ...validInput, reviewPeriodEnd: '2025-12-31' })
    expect(result.success).toBe(false)
  })
})

describe('selfAssessmentFormSchema', () => {
  it('requires non-empty text within 4000 characters', () => {
    expect(selfAssessmentFormSchema.safeParse({ selfAssessment: '' }).success).toBe(false)
    expect(selfAssessmentFormSchema.safeParse({ selfAssessment: 'a'.repeat(4001) }).success).toBe(false)
    expect(selfAssessmentFormSchema.safeParse({ selfAssessment: 'Solid quarter.' }).success).toBe(true)
  })
})

describe('managerReviewFormSchema', () => {
  it('requires a rating between 1 and 5', () => {
    const base = { managerAssessment: 'Great work.' }
    expect(managerReviewFormSchema.safeParse({ ...base, overallRating: 0 }).success).toBe(false)
    expect(managerReviewFormSchema.safeParse({ ...base, overallRating: 6 }).success).toBe(false)
    expect(managerReviewFormSchema.safeParse({ ...base, overallRating: 3 }).success).toBe(true)
  })
})

describe('cancelReviewFormSchema', () => {
  it('allows an omitted reason', () => {
    expect(cancelReviewFormSchema.safeParse({}).success).toBe(true)
  })
})

describe('proposePromotionFormSchema', () => {
  const validInput = {
    employeeId: 'employee-1',
    toDesignationId: 'designation-1',
    effectiveDate: '2026-09-01',
    reason: 'Consistently exceeding delivery goals.',
  }

  it('accepts a valid promotion proposal without a department change', () => {
    expect(proposePromotionFormSchema.safeParse(validInput).success).toBe(true)
  })

  it('requires an employee, designation, effective date, and reason', () => {
    for (const field of ['employeeId', 'toDesignationId', 'effectiveDate', 'reason'] as const) {
      expect(proposePromotionFormSchema.safeParse({ ...validInput, [field]: '' }).success).toBe(false)
    }
  })
})
