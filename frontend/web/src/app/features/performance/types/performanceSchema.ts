import { z } from 'zod'

// ─── Goals ───────────────────────────────────────────────────────────────────

const GOAL_STATUSES = ['NotStarted', 'InProgress', 'Completed', 'Cancelled'] as const

export const createGoalFormSchema = z
  .object({
    employeeId: z.string().min(1, 'Employee is required.'),
    title: z.string().min(1, 'Title is required.').max(200, 'Title must be 200 characters or fewer.'),
    description: z.string().max(2000, 'Description must be 2000 characters or fewer.').optional().or(z.literal('')),
    category: z.string().max(100, 'Category must be 100 characters or fewer.').optional().or(z.literal('')),
    startDate: z.string().min(1, 'Start date is required.'),
    targetDate: z.string().min(1, 'Target date is required.'),
    weight: z.coerce.number().min(0, 'Weight must be between 0 and 100.').max(100, 'Weight must be between 0 and 100.').optional(),
  })
  .refine((data) => data.targetDate >= data.startDate, {
    message: 'Target date must be on or after the start date.',
    path: ['targetDate'],
  })

export type CreateGoalFormInput = z.input<typeof createGoalFormSchema>
export type CreateGoalFormValues = z.infer<typeof createGoalFormSchema>

export const updateGoalFormSchema = z.object({
  title: z.string().min(1, 'Title is required.').max(200, 'Title must be 200 characters or fewer.'),
  description: z.string().max(2000, 'Description must be 2000 characters or fewer.').optional().or(z.literal('')),
  category: z.string().max(100, 'Category must be 100 characters or fewer.').optional().or(z.literal('')),
  targetDate: z.string().min(1, 'Target date is required.'),
  weight: z.coerce.number().min(0, 'Weight must be between 0 and 100.').max(100, 'Weight must be between 0 and 100.').optional(),
  status: z.enum(GOAL_STATUSES),
})

export type UpdateGoalFormInput = z.input<typeof updateGoalFormSchema>
export type UpdateGoalFormValues = z.infer<typeof updateGoalFormSchema>

export const goalProgressFormSchema = z.object({
  progressPercent: z.coerce.number().min(0, 'Progress must be between 0 and 100.').max(100, 'Progress must be between 0 and 100.'),
})

export type GoalProgressFormInput = z.input<typeof goalProgressFormSchema>
export type GoalProgressFormValues = z.infer<typeof goalProgressFormSchema>

export const addGoalKpiFormSchema = z.object({
  name: z.string().min(1, 'KPI name is required.').max(200, 'KPI name must be 200 characters or fewer.'),
  targetValue: z.coerce.number().min(0, 'Target value cannot be negative.'),
  unit: z.string().max(30, 'Unit must be 30 characters or fewer.').optional().or(z.literal('')),
  notes: z.string().max(1000, 'Notes must be 1000 characters or fewer.').optional().or(z.literal('')),
})

export type AddGoalKpiFormInput = z.input<typeof addGoalKpiFormSchema>
export type AddGoalKpiFormValues = z.infer<typeof addGoalKpiFormSchema>

export const kpiProgressFormSchema = z.object({
  currentValue: z.coerce.number().min(0, 'Current value cannot be negative.'),
  notes: z.string().max(1000, 'Notes must be 1000 characters or fewer.').optional().or(z.literal('')),
})

export type KpiProgressFormInput = z.input<typeof kpiProgressFormSchema>
export type KpiProgressFormValues = z.infer<typeof kpiProgressFormSchema>

// ─── Performance Reviews ─────────────────────────────────────────────────────

export const createReviewFormSchema = z
  .object({
    employeeId: z.string().min(1, 'Employee is required.'),
    reviewerEmployeeId: z.string().min(1, 'Reviewer is required.'),
    reviewPeriodStart: z.string().min(1, 'Review period start is required.'),
    reviewPeriodEnd: z.string().min(1, 'Review period end is required.'),
    notes: z.string().max(1000, 'Notes must be 1000 characters or fewer.').optional().or(z.literal('')),
  })
  .refine((data) => data.reviewPeriodEnd >= data.reviewPeriodStart, {
    message: 'Review period end must be on or after the start.',
    path: ['reviewPeriodEnd'],
  })
  .refine((data) => data.employeeId !== data.reviewerEmployeeId, {
    message: 'An employee cannot review themselves.',
    path: ['reviewerEmployeeId'],
  })

export type CreateReviewFormValues = z.infer<typeof createReviewFormSchema>

export const selfAssessmentFormSchema = z.object({
  selfAssessment: z
    .string()
    .min(1, 'Self-assessment is required.')
    .max(4000, 'Self-assessment must be 4000 characters or fewer.'),
})

export type SelfAssessmentFormValues = z.infer<typeof selfAssessmentFormSchema>

export const managerReviewFormSchema = z.object({
  managerAssessment: z
    .string()
    .min(1, 'Manager assessment is required.')
    .max(4000, 'Manager assessment must be 4000 characters or fewer.'),
  overallRating: z.coerce.number().min(1, 'Rating must be between 1 and 5.').max(5, 'Rating must be between 1 and 5.'),
})

export type ManagerReviewFormInput = z.input<typeof managerReviewFormSchema>
export type ManagerReviewFormValues = z.infer<typeof managerReviewFormSchema>

export const cancelReviewFormSchema = z.object({
  reason: z.string().max(500, 'Reason must be 500 characters or fewer.').optional().or(z.literal('')),
})

export type CancelReviewFormValues = z.infer<typeof cancelReviewFormSchema>

// ─── Promotions ───────────────────────────────────────────────────────────────

export const proposePromotionFormSchema = z.object({
  employeeId: z.string().min(1, 'Employee is required.'),
  toDesignationId: z.string().min(1, 'Target designation is required.'),
  toDepartmentId: z.string().optional().or(z.literal('')),
  effectiveDate: z.string().min(1, 'Effective date is required.'),
  reason: z.string().min(1, 'Reason is required.').max(1000, 'Reason must be 1000 characters or fewer.'),
})

export type ProposePromotionFormValues = z.infer<typeof proposePromotionFormSchema>

export const promotionDecisionFormSchema = z.object({
  decisionNotes: z.string().max(1000, 'Decision notes must be 1000 characters or fewer.').optional().or(z.literal('')),
})

export type PromotionDecisionFormValues = z.infer<typeof promotionDecisionFormSchema>
