import { z } from 'zod'
import { passwordPolicySchema } from '@/app/features/auth/types/passwordPolicy'

export const companyRegistrationSchema = z
  .object({
    companyName: z
      .string()
      .min(2, 'Company name must be at least 2 characters long.')
      .max(200, 'Company name must be 200 characters or fewer.'),
    timezone: z.string().min(1, 'Timezone is required.'),
    currency: z.string().min(1, 'Currency is required.'),
    adminUserName: z
      .string()
      .min(3, 'Username must be at least 3 characters long.')
      .max(50, 'Username must be 50 characters or fewer.'),
    adminEmail: z.string().min(1, 'Email is required.').email('Enter a valid email address.'),
    adminPassword: passwordPolicySchema,
    confirmPassword: z.string().min(1, 'Confirm your password.'),
  })
  .refine((values) => values.adminPassword === values.confirmPassword, {
    message: 'Passwords do not match.',
    path: ['confirmPassword'],
  })

export type CompanyRegistrationFormValues = z.infer<typeof companyRegistrationSchema>
