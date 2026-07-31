import { selectRepository } from '@/app/core/config/selectRepository'
import { mockMessagingRepository } from './mockMessagingRepository'
import { apiMessagingRepository } from './apiMessagingRepository'
import type { MessagingRepository } from './messagingRepository'

export const messagingRepository: MessagingRepository = selectRepository({
  mock: mockMessagingRepository,
  api: apiMessagingRepository,
})

export type {
  AddParticipantsInput,
  Conversation,
  ConversationListFilters,
  CreateConversationInput,
  Message,
  MessageListFilters,
  MessageParticipant,
  MessagingRepository,
  SendMessageInput,
} from './messagingRepository'
