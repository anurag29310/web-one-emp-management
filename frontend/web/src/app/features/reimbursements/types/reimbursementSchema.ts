import { z } from 'zod'

function decimalString(mustBePositive: string) {
  return z
    .string()
    .optional()
    .or(z.literal(''))
    .refine((v) => !v || (!Number.isNaN(Number(v)) && Number(v) > 0), { message: mustBePositive })
}

export const reimbursementFormSchema = z
  .object({
    expenseTitle: z.string().min(1, 'Expense title is required.').max(200, 'Title must be 200 characters or fewer.'),
    expenseCategory: z
      .string()
      .min(1, 'Expense category is required.')
      .max(100, 'Category must be 100 characters or fewer.'),
    expenseDate: z.string().min(1, 'Expense date is required.'),
    currency: z.string().min(1, 'Currency is required.').max(10, 'Currency must be 10 characters or fewer.'),
    claimType: z.enum(['Amount', 'Mileage']),
    amount: decimalString('Amount must be greater than 0.'),
    distanceKm: decimalString('Distance must be greater than 0.'),
    description: z.string().max(2000, 'Description must be 2000 characters or fewer.').optional().or(z.literal('')),
    notes: z.string().max(1000, 'Notes must be 1000 characters or fewer.').optional().or(z.literal('')),
  })
  .superRefine((values, ctx) => {
    if (values.claimType === 'Amount' && !values.amount) {
      ctx.addIssue({ code: 'custom', path: ['amount'], message: 'Amount is required.' })
    }
    if (values.claimType === 'Mileage' && !values.distanceKm) {
      ctx.addIssue({ code: 'custom', path: ['distanceKm'], message: 'Distance is required.' })
    }
  })

export type ReimbursementFormValues = z.infer<typeof reimbursementFormSchema>

export const reviewRemarksFormSchema = z.object({
  remarks: z.string().min(1, 'A remark is required.').max(1000, 'Remarks must be 1000 characters or fewer.'),
})

export type ReviewRemarksFormValues = z.infer<typeof reviewRemarksFormSchema>
