import { z } from 'zod'

export const officeLocationFormSchema = z.object({
  name: z.string().min(1, 'Name is required.').max(150, 'Name must be 150 characters or fewer.'),
  code: z.string().min(1, 'Code is required.').max(50, 'Code must be 50 characters or fewer.'),
  addressLine1: z.string().max(250, 'Address line 1 must be 250 characters or fewer.').optional().or(z.literal('')),
  addressLine2: z.string().max(250, 'Address line 2 must be 250 characters or fewer.').optional().or(z.literal('')),
  city: z.string().min(1, 'City is required.').max(100, 'City must be 100 characters or fewer.'),
  state: z.string().max(100, 'State must be 100 characters or fewer.').optional().or(z.literal('')),
  country: z.string().min(1, 'Country is required.').max(100, 'Country must be 100 characters or fewer.'),
  timeZoneId: z
    .string()
    .min(1, 'Time zone is required.')
    .max(100, 'Time zone must be 100 characters or fewer.'),
})

export type OfficeLocationFormValues = z.infer<typeof officeLocationFormSchema>
