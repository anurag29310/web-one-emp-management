import { z } from 'zod'

const PRIORITIES = ['Low', 'Medium', 'High', 'Critical'] as const

export const createTaskFormSchema = z.object({
  title: z.string().min(1, 'Title is required.').max(200, 'Title must be 200 characters or fewer.'),
  description: z.string().max(2000, 'Description must be 2000 characters or fewer.').optional().or(z.literal('')),
  clientId: z.string().optional().or(z.literal('')),
  assignedEmployeeId: z.string().min(1, 'Assignee is required.'),
  dueDate: z.string().optional().or(z.literal('')),
  priority: z.enum(PRIORITIES),
  notes: z.string().max(1000, 'Notes must be 1000 characters or fewer.').optional().or(z.literal('')),
})

export type CreateTaskFormValues = z.infer<typeof createTaskFormSchema>

export const editTaskFormSchema = createTaskFormSchema.omit({ assignedEmployeeId: true })

export type EditTaskFormValues = z.infer<typeof editTaskFormSchema>

export const reassignTaskFormSchema = z.object({
  assignedEmployeeId: z.string().min(1, 'Assignee is required.'),
})

export type ReassignTaskFormValues = z.infer<typeof reassignTaskFormSchema>

export const rejectTaskFormSchema = z.object({
  reason: z.string().max(500, 'Reason must be 500 characters or fewer.').optional().or(z.literal('')),
})

export type RejectTaskFormValues = z.infer<typeof rejectTaskFormSchema>

export const taskCommentFormSchema = z.object({
  comment: z.string().min(1, 'Comment is required.').max(2000, 'Comment must be 2000 characters or fewer.'),
})

export type TaskCommentFormValues = z.infer<typeof taskCommentFormSchema>
