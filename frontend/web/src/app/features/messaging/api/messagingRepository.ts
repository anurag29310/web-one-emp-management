import type { PagedResult } from '@/app/shared/models/apiEnvelope'

export interface MessageParticipant {
  userId: string
  name: string
  joinedAtUtc: string
  leftAtUtc: string | null
}

export interface Conversation {
  id: string
  title: string | null
  isGroup: boolean
  participants: MessageParticipant[]
  lastMessageAtUtc: string | null
  lastMessagePreview: string | null
  unreadCount: number
  isDeleted: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface ConversationListFilters {
  page?: number
  pageSize?: number
  search?: string
}

export interface CreateConversationInput {
  participantUserIds: string[]
  title?: string
  initialMessageBody: string
}

export interface AddParticipantsInput {
  userIds: string[]
}

export interface Message {
  id: string
  conversationId: string
  senderUserId: string
  senderName: string | null
  body: string
  sentAtUtc: string
}

export interface MessageListFilters {
  page?: number
  pageSize?: number
}

export interface SendMessageInput {
  body: string
}

export interface MessagingRepository {
  listConversations(filters?: ConversationListFilters): Promise<PagedResult<Conversation>>
  getConversationById(id: string): Promise<Conversation>
  getUnreadConversationCount(): Promise<number>
  createConversation(input: CreateConversationInput): Promise<{ id: string }>
  addParticipants(conversationId: string, input: AddParticipantsInput): Promise<void>
  leaveConversation(conversationId: string): Promise<void>
  removeConversation(id: string): Promise<void>
  restoreConversation(id: string): Promise<void>

  listMessages(conversationId: string, filters?: MessageListFilters): Promise<PagedResult<Message>>
  sendMessage(conversationId: string, input: SendMessageInput): Promise<{ id: string }>
  markConversationRead(conversationId: string): Promise<void>
}
