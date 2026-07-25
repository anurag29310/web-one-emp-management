import { z } from 'zod'

export const teamFormSchema = z.object({
  departmentId: z.string().min(1, 'Department is required.'),
  name: z.string().min(1, 'Name is required.').max(150, 'Name must be 150 characters or fewer.'),
  code: z.string().min(1, 'Code is required.').max(50, 'Code must be 50 characters or fewer.'),
  leadEmployeeId: z.string().optional().or(z.literal('')),
})

export type TeamFormValues = z.infer<typeof teamFormSchema>
