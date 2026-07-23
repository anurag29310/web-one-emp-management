import { z } from 'zod'

export const leaveDecisionFormSchema = z.object({
  comments: z
    .string()
    .max(500, 'Comments must be 500 characters or fewer.')
    .optional()
    .or(z.literal('')),
})

export type LeaveDecisionFormValues = z.infer<typeof leaveDecisionFormSchema>
