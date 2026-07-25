import { z } from 'zod'

export const designationFormSchema = z.object({
  name: z.string().min(1, 'Name is required.').max(150, 'Name must be 150 characters or fewer.'),
  code: z.string().min(1, 'Code is required.').max(50, 'Code must be 50 characters or fewer.'),
  level: z
    .string()
    .optional()
    .or(z.literal(''))
    .refine((val) => !val || /^\d+$/.test(val), 'Level must be a whole number.'),
})

export type DesignationFormValues = z.infer<typeof designationFormSchema>
