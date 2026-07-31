import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import type {
  AddParticipantsInput,
  Conversation,
  ConversationListFilters,
  CreateConversationInput,
  Message,
  MessageListFilters,
  MessagingRepository,
  SendMessageInput,
} from './messagingRepository'
import { mockConversations, mockMessages, mockUserDisplayNames } from './mockData'

let conversations = [...mockConversations]
let messages = [...mockMessages]

function nextId(): string {
  return `00000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

function paginate<T>(items: T[], page: number, pageSize: number): PagedResult<T> {
  const start = (page - 1) * pageSize
  return {
    data: items.slice(start, start + pageSize),
    page,
    pageSize,
    totalCount: items.length,
    totalPages: Math.max(1, Math.ceil(items.length / pageSize)),
    correlationId: 'mock-correlation-id',
  }
}

function findConversationOrThrow(id: string): Conversation {
  const conversation = conversations.find((c) => c.id === id)
  if (!conversation) throw new AppError(`Conversation ${id} was not found.`, 404, 'NOT_FOUND')
  return conversation
}

function displayName(userId: string): string {
  return mockUserDisplayNames[userId] ?? userId
}

/**
 * Unlike the real API, this mock has no concept of "the current user" — repository calls carry
 * no identity, so it can't replicate the backend's per-caller participant/unread scoping. It
 * mirrors the same permissive convention already used by the other mock repositories in this
 * codebase (e.g. Performance): every seeded conversation is visible regardless of which mock
 * account is logged in, and unread counts are static illustrative data rather than computed
 * per-viewer.
 */
export const mockMessagingRepository: MessagingRepository = {
  async listConversations(filters: ConversationListFilters = {}): Promise<PagedResult<Conversation>> {
    await delay(300)
    const { page = 1, pageSize = 20, search } = filters
    let filtered = conversations.filter((c) => !c.isDeleted)
    if (search) {
      const term = search.toLowerCase()
      filtered = filtered.filter(
        (c) =>
          c.title?.toLowerCase().includes(term) ||
          c.participants.some((p) => p.name.toLowerCase().includes(term)),
      )
    }
    filtered = [...filtered].sort((a, b) => (b.lastMessageAtUtc ?? '').localeCompare(a.lastMessageAtUtc ?? ''))
    return paginate(filtered, page, pageSize)
  },

  async getConversationById(id: string): Promise<Conversation> {
    await delay(200)
    return findConversationOrThrow(id)
  },

  async getUnreadConversationCount(): Promise<number> {
    await delay(150)
    return conversations.filter((c) => !c.isDeleted && c.unreadCount > 0).length
  },

  async createConversation(input: CreateConversationInput): Promise<{ id: string }> {
    await delay(300)
    const now = new Date().toISOString()
    const participants = input.participantUserIds.map((userId) => ({
      userId,
      name: displayName(userId),
      joinedAtUtc: now,
      leftAtUtc: null,
    }))
    const conversation: Conversation = {
      id: nextId(),
      title: input.title ?? null,
      isGroup: participants.length > 1,
      participants,
      lastMessageAtUtc: now,
      lastMessagePreview: input.initialMessageBody,
      unreadCount: 0,
      isDeleted: false,
      createdAtUtc: now,
      updatedAtUtc: null,
    }
    conversations = [conversation, ...conversations]
    messages = [
      ...messages,
      {
        id: nextId(),
        conversationId: conversation.id,
        senderUserId: participants[0]?.userId ?? '',
        senderName: participants[0] ? displayName(participants[0].userId) : null,
        body: input.initialMessageBody,
        sentAtUtc: now,
      },
    ]
    return { id: conversation.id }
  },

  async addParticipants(conversationId: string, input: AddParticipantsInput): Promise<void> {
    await delay(250)
    const existing = findConversationOrThrow(conversationId)
    const now = new Date().toISOString()
    const existingIds = new Set(existing.participants.map((p) => p.userId))
    const newParticipants = input.userIds
      .filter((userId) => !existingIds.has(userId))
      .map((userId) => ({ userId, name: displayName(userId), joinedAtUtc: now, leftAtUtc: null }))

    conversations = conversations.map((c) =>
      c.id === conversationId
        ? { ...c, isGroup: true, participants: [...c.participants, ...newParticipants], updatedAtUtc: now }
        : c,
    )
  },

  async leaveConversation(conversationId: string): Promise<void> {
    await delay(200)
    const existing = findConversationOrThrow(conversationId)
    if (!existing.isGroup) {
      throw new AppError('Cannot leave a direct conversation.', 409, 'CONFLICT')
    }
  },

  async removeConversation(id: string): Promise<void> {
    await delay(200)
    findConversationOrThrow(id)
    conversations = conversations.map((c) => (c.id === id ? { ...c, isDeleted: true } : c))
  },

  async restoreConversation(id: string): Promise<void> {
    await delay(200)
    conversations = conversations.map((c) => (c.id === id ? { ...c, isDeleted: false } : c))
  },

  async listMessages(conversationId: string, filters: MessageListFilters = {}): Promise<PagedResult<Message>> {
    await delay(250)
    findConversationOrThrow(conversationId)
    const { page = 1, pageSize = 50 } = filters
    const filtered = messages
      .filter((m) => m.conversationId === conversationId)
      .sort((a, b) => b.sentAtUtc.localeCompare(a.sentAtUtc))
    return paginate(filtered, page, pageSize)
  },

  async sendMessage(conversationId: string, input: SendMessageInput): Promise<{ id: string }> {
    await delay(250)
    const existing = findConversationOrThrow(conversationId)
    const now = new Date().toISOString()
    const message: Message = {
      id: nextId(),
      conversationId,
      senderUserId: existing.participants[0]?.userId ?? '',
      senderName: existing.participants[0] ? displayName(existing.participants[0].userId) : null,
      body: input.body,
      sentAtUtc: now,
    }
    messages = [...messages, message]
    conversations = conversations.map((c) =>
      c.id === conversationId ? { ...c, lastMessageAtUtc: now, lastMessagePreview: input.body, updatedAtUtc: now } : c,
    )
    return { id: message.id }
  },

  async markConversationRead(conversationId: string): Promise<void> {
    await delay(150)
    findConversationOrThrow(conversationId)
    conversations = conversations.map((c) => (c.id === conversationId ? { ...c, unreadCount: 0 } : c))
  },
}
