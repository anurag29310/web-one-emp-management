import { z } from 'zod'

export const departmentFormSchema = z.object({
  name: z.string().min(1, 'Name is required.').max(150, 'Name must be 150 characters or fewer.'),
  code: z
    .string()
    .max(50, 'Code must be 50 characters or fewer.')
    .optional()
    .or(z.literal('')),
  description: z
    .string()
    .max(500, 'Description must be 500 characters or fewer.')
    .optional()
    .or(z.literal('')),
  headEmployeeId: z.string().optional().or(z.literal('')),
})

export type DepartmentFormValues = z.infer<typeof departmentFormSchema>
