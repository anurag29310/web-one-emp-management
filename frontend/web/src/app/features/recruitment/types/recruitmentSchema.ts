import { z } from 'zod'

const INTERVIEW_MODES = ['Onsite', 'Phone', 'VideoCall'] as const
const INTERVIEW_OUTCOMES = ['Passed', 'Failed', 'OnHold'] as const

export const createCandidateFormSchema = z.object({
  firstName: z.string().min(1, 'First name is required.').max(100, 'First name must be 100 characters or fewer.'),
  lastName: z.string().min(1, 'Last name is required.').max(100, 'Last name must be 100 characters or fewer.'),
  email: z.string().min(1, 'Email is required.').email('Enter a valid email address.'),
  phoneNumber: z.string().max(30, 'Phone number must be 30 characters or fewer.').optional().or(z.literal('')),
  designationId: z.string().min(1, 'Designation is required.'),
  departmentId: z.string().optional().or(z.literal('')),
  source: z.string().max(100, 'Source must be 100 characters or fewer.').optional().or(z.literal('')),
  appliedDate: z.string().min(1, 'Applied date is required.'),
  notes: z.string().max(1000, 'Notes must be 1000 characters or fewer.').optional().or(z.literal('')),
})
export type CreateCandidateFormValues = z.infer<typeof createCandidateFormSchema>

export const editCandidateFormSchema = createCandidateFormSchema.omit({ appliedDate: true })
export type EditCandidateFormValues = z.infer<typeof editCandidateFormSchema>

export const reasonFormSchema = z.object({
  reason: z.string().max(500, 'Reason must be 500 characters or fewer.').optional().or(z.literal('')),
})
export type ReasonFormValues = z.infer<typeof reasonFormSchema>

export const scheduleInterviewFormSchema = z.object({
  interviewerEmployeeId: z.string().min(1, 'Interviewer is required.'),
  round: z.string().min(1, 'Round is required.').max(150, 'Round must be 150 characters or fewer.'),
  mode: z.enum(INTERVIEW_MODES),
  scheduledAtUtc: z.string().min(1, 'Scheduled date/time is required.'),
  durationMinutes: z
    .string()
    .optional()
    .or(z.literal(''))
    .refine((v) => !v || (!Number.isNaN(Number(v)) && Number(v) > 0), { message: 'Must be greater than 0.' }),
})
export type ScheduleInterviewFormValues = z.infer<typeof scheduleInterviewFormSchema>

export const rescheduleInterviewFormSchema = scheduleInterviewFormSchema.pick({
  scheduledAtUtc: true,
  durationMinutes: true,
})
export type RescheduleInterviewFormValues = z.infer<typeof rescheduleInterviewFormSchema>

export const interviewFeedbackFormSchema = z.object({
  feedback: z.string().min(1, 'Feedback is required.').max(2000, 'Feedback must be 2000 characters or fewer.'),
  rating: z.coerce.number().int().min(1, 'Rating must be between 1 and 5.').max(5, 'Rating must be between 1 and 5.'),
  outcome: z.enum(INTERVIEW_OUTCOMES),
})
export type InterviewFeedbackFormInput = z.input<typeof interviewFeedbackFormSchema>
export type InterviewFeedbackFormValues = z.infer<typeof interviewFeedbackFormSchema>

export const createOfferFormSchema = z.object({
  designationId: z.string().min(1, 'Designation is required.'),
  departmentId: z.string().optional().or(z.literal('')),
  offeredSalary: z.coerce.number().gt(0, 'Offered salary must be greater than 0.'),
  joiningDate: z.string().min(1, 'Joining date is required.'),
  expiresAtUtc: z.string().optional().or(z.literal('')),
  notes: z.string().max(1000, 'Notes must be 1000 characters or fewer.').optional().or(z.literal('')),
})
export type CreateOfferFormInput = z.input<typeof createOfferFormSchema>
export type CreateOfferFormValues = z.infer<typeof createOfferFormSchema>

export const addChecklistItemFormSchema = z.object({
  itemName: z.string().min(1, 'Item name is required.').max(200, 'Item name must be 200 characters or fewer.'),
  notes: z.string().max(500, 'Notes must be 500 characters or fewer.').optional().or(z.literal('')),
})
export type AddChecklistItemFormValues = z.infer<typeof addChecklistItemFormSchema>

export const convertToEmployeeFormSchema = z.object({
  employeeCode: z.string().min(1, 'Employee code is required.').max(50, 'Employee code must be 50 characters or fewer.'),
  officeLocationId: z.string().min(1, 'Office location is required.'),
  teamId: z.string().optional().or(z.literal('')),
  managerId: z.string().optional().or(z.literal('')),
  joinDate: z.string().optional().or(z.literal('')),
})
export type ConvertToEmployeeFormValues = z.infer<typeof convertToEmployeeFormSchema>
