import { z } from 'zod'

export const companyFormSchema = z.object({
  name: z.string().min(1, 'Company name is required.').max(200, 'Company name must be 200 characters or fewer.'),
  timezone: z.string().min(1, 'Timezone is required.').max(100, 'Timezone must be 100 characters or fewer.'),
  currency: z.string().min(1, 'Currency is required.').max(10, 'Currency must be 10 characters or fewer.'),
  logoUrl: z
    .string()
    .max(2000, 'Logo URL must be 2000 characters or fewer.')
    .url('Enter a valid URL.')
    .optional()
    .or(z.literal('')),
})

export type CompanyFormValues = z.infer<typeof companyFormSchema>
