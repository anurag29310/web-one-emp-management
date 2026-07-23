import { z } from 'zod'

export const leaveTypeFormSchema = z.object({
  name: z.string().min(1, 'Name is required.').max(100, 'Name must be 100 characters or fewer.'),
  code: z
    .string()
    .max(50, 'Code must be 50 characters or fewer.')
    .optional()
    .or(z.literal('')),
  isPaid: z.boolean(),
  requiresApproval: z.boolean(),
  annualEntitlementDays: z
    .string()
    .optional()
    .or(z.literal(''))
    .refine((value) => !value || (!Number.isNaN(Number(value)) && Number(value) >= 0), {
      message: 'Must be a non-negative number.',
    }),
})

export type LeaveTypeFormValues = z.infer<typeof leaveTypeFormSchema>
