import { z } from 'zod'

export const leaveBalanceFormSchema = z.object({
  leaveTypeId: z.string().min(1, 'Leave type is required.'),
  year: z
    .string()
    .min(1, 'Year is required.')
    .refine((value) => !Number.isNaN(Number(value)) && Number(value) >= 2000 && Number(value) <= 2100, {
      message: 'Year must be between 2000 and 2100.',
    }),
  adjusted: z
    .string()
    .min(1, 'Adjustment amount is required.')
    .refine((value) => !Number.isNaN(Number(value)), { message: 'Must be a number.' }),
  reason: z
    .string()
    .max(500, 'Reason must be 500 characters or fewer.')
    .optional()
    .or(z.literal('')),
})

export type LeaveBalanceFormValues = z.infer<typeof leaveBalanceFormSchema>
