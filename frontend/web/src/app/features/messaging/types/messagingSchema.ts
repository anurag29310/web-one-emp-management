import { z } from 'zod'

export const createConversationFormSchema = z.object({
  participantUserIds: z.array(z.string()).min(1, 'At least one other participant is required.'),
  title: z.string().max(250, 'Title must be 250 characters or fewer.').optional().or(z.literal('')),
  initialMessageBody: z
    .string()
    .min(1, 'A first message is required.')
    .max(4000, 'Message must be 4000 characters or fewer.'),
})

export type CreateConversationFormValues = z.infer<typeof createConversationFormSchema>

export const sendMessageFormSchema = z.object({
  body: z.string().min(1, 'Message cannot be empty.').max(4000, 'Message must be 4000 characters or fewer.'),
})

export type SendMessageFormValues = z.infer<typeof sendMessageFormSchema>

export const addParticipantsFormSchema = z.object({
  userIds: z.array(z.string()).min(1, 'At least one user is required.'),
})

export type AddParticipantsFormValues = z.infer<typeof addParticipantsFormSchema>
