import { z } from 'zod'

export const holidayFormSchema = z.object({
  name: z.string().min(1, 'Name is required.').max(150, 'Name must be 150 characters or fewer.'),
  officeLocationId: z.string().optional().or(z.literal('')),
  holidayDate: z.string().min(1, 'Date is required.'),
  isOptional: z.boolean(),
})

export type HolidayFormValues = z.infer<typeof holidayFormSchema>
