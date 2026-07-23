import { z } from 'zod'

/** Mirrors GetLeaveSummaryQueryValidator / GetEmployeeJoinExitQueryValidator: both dates
 * required, `from` must be on or before `to`. */
export const dateRangeFormSchema = z
  .object({
    from: z.string().min(1, 'From date is required.'),
    to: z.string().min(1, 'To date is required.'),
  })
  .refine((data) => new Date(data.from).getTime() <= new Date(data.to).getTime(), {
    message: 'From date must be on or before the to date.',
    path: ['to'],
  })

export type DateRangeFormValues = z.infer<typeof dateRangeFormSchema>
