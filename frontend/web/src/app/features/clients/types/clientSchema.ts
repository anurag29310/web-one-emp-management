import { z } from 'zod'

const optionalDecimalString = (min: number, max: number) =>
  z
    .string()
    .optional()
    .or(z.literal(''))
    .refine((v) => !v || (!Number.isNaN(Number(v)) && Number(v) >= min && Number(v) <= max), {
      message: `Must be a number between ${min} and ${max}.`,
    })

export const clientFormSchema = z.object({
  clientName: z.string().min(1, 'Client name is required.').max(200, 'Client name must be 200 characters or fewer.'),
  companyName: z
    .string()
    .min(1, 'Company name is required.')
    .max(200, 'Company name must be 200 characters or fewer.'),
  contactPerson: z
    .string()
    .min(1, 'Contact person is required.')
    .max(200, 'Contact person must be 200 characters or fewer.'),
  mobileNumber: z
    .string()
    .min(1, 'Mobile number is required.')
    .max(20, 'Mobile number must be 20 characters or fewer.'),
  alternateMobile: z.string().max(20, 'Alternate mobile must be 20 characters or fewer.').optional().or(z.literal('')),
  email: z.string().min(1, 'Email is required.').email('Enter a valid email address.'),
  gstNumber: z.string().max(30, 'GST number must be 30 characters or fewer.').optional().or(z.literal('')),
  addressLine1: z.string().min(1, 'Address is required.').max(200, 'Address must be 200 characters or fewer.'),
  addressLine2: z.string().max(200, 'Address must be 200 characters or fewer.').optional().or(z.literal('')),
  city: z.string().min(1, 'City is required.').max(100, 'City must be 100 characters or fewer.'),
  state: z.string().max(100, 'State must be 100 characters or fewer.').optional().or(z.literal('')),
  country: z.string().min(1, 'Country is required.').max(100, 'Country must be 100 characters or fewer.'),
  postalCode: z.string().min(1, 'Postal code is required.').max(20, 'Postal code must be 20 characters or fewer.'),
  latitude: optionalDecimalString(-90, 90),
  longitude: optionalDecimalString(-180, 180),
  notes: z.string().max(1000, 'Notes must be 1000 characters or fewer.').optional().or(z.literal('')),
})

export type ClientFormValues = z.infer<typeof clientFormSchema>
