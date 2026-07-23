import { z } from 'zod'

// Mirrors CreateAnnouncementCommandValidator.cs.
export const ANNOUNCEMENT_PRIORITIES = ['Normal', 'Important', 'Critical'] as const
export const ANNOUNCEMENT_AUDIENCE_TYPES = ['All', 'Department', 'Role'] as const
export const ANNOUNCEMENT_TARGET_ROLES = ['Admin', 'HR', 'Manager', 'Employee'] as const

export const announcementFormSchema = z
  .object({
    title: z.string().min(1, 'Title is required.').max(250, 'Title must be 250 characters or fewer.'),
    message: z.string().min(1, 'Message is required.').max(2000, 'Message must be 2000 characters or fewer.'),
    priority: z.enum(ANNOUNCEMENT_PRIORITIES),
    audienceType: z.enum(ANNOUNCEMENT_AUDIENCE_TYPES),
    departmentId: z.string().optional().or(z.literal('')),
    targetRole: z.enum(ANNOUNCEMENT_TARGET_ROLES).optional().or(z.literal('')),
    expiresAtUtc: z.string().optional().or(z.literal('')),
  })
  .refine((values) => values.audienceType !== 'Department' || Boolean(values.departmentId), {
    message: 'Select a department for a department-scoped announcement.',
    path: ['departmentId'],
  })
  .refine((values) => values.audienceType !== 'Role' || Boolean(values.targetRole), {
    message: 'Select a role for a role-scoped announcement.',
    path: ['targetRole'],
  })

export type AnnouncementFormValues = z.infer<typeof announcementFormSchema>
