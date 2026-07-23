import { z } from 'zod'

export const applyLeaveFormSchema = z
  .object({
    employeeId: z.string().min(1, 'Employee is required.'),
    leaveTypeId: z.string().min(1, 'Leave type is required.'),
    startDate: z.string().min(1, 'Start date is required.'),
    endDate: z.string().min(1, 'End date is required.'),
    reason: z
      .string()
      .max(500, 'Reason must be 500 characters or fewer.')
      .optional()
      .or(z.literal('')),
  })
  .refine((data) => new Date(data.endDate).getTime() >= new Date(data.startDate).getTime(), {
    message: 'End date must be on or after the start date.',
    path: ['endDate'],
  })

export type ApplyLeaveFormValues = z.infer<typeof applyLeaveFormSchema>
