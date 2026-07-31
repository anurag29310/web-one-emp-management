import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { ApiSuccessEnvelope, PagedResult } from '@/app/shared/models/apiEnvelope'
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

/**
 * Backend wraps EMS.Application.Common.DTOs.PagedResult<T> a second time inside ApiResponse<T>
 * (see attendance/audit-logs/performance repositories for the same shape) — pagination fields
 * live one level deeper than the flat shape documented in api-specification.md §2.3.
 */
interface BackendPagedResult<T> {
  data: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

function unwrapPaged<T>(response: { data: ApiSuccessEnvelope<BackendPagedResult<T>> }): PagedResult<T> {
  const envelope = response.data
  const paged = envelope.data
  return {
    data: paged.data,
    page: paged.page,
    pageSize: paged.pageSize,
    totalCount: paged.totalCount,
    totalPages: paged.totalPages,
    correlationId: envelope.correlationId,
  }
}

export const apiMessagingRepository: MessagingRepository = {
  async listConversations(filters: ConversationListFilters = {}): Promise<PagedResult<Conversation>> {
    const response = await httpClient.get<ApiSuccessEnvelope<BackendPagedResult<Conversation>>>('/conversations', {
      params: filters,
    })
    return unwrapPaged(response)
  },

  async getConversationById(id: string): Promise<Conversation> {
    const response = await httpClient.get<{ data: Conversation }>(`/conversations/${id}`)
    return unwrap(response)
  },

  async getUnreadConversationCount(): Promise<number> {
    const response = await httpClient.get<{ data: number }>('/conversations/unread-count')
    return unwrap(response)
  },

  async createConversation(input: CreateConversationInput): Promise<{ id: string }> {
    const response = await httpClient.post<{ data: { id: string } }>('/conversations', input)
    return unwrap(response)
  },

  async addParticipants(conversationId: string, input: AddParticipantsInput): Promise<void> {
    await httpClient.post(`/conversations/${conversationId}/participants`, input)
  },

  async leaveConversation(conversationId: string): Promise<void> {
    await httpClient.post(`/conversations/${conversationId}/leave`)
  },

  async removeConversation(id: string): Promise<void> {
    await httpClient.delete(`/conversations/${id}`)
  },

  async restoreConversation(id: string): Promise<void> {
    await httpClient.post(`/conversations/${id}/restore`)
  },

  async listMessages(conversationId: string, filters: MessageListFilters = {}): Promise<PagedResult<Message>> {
    const response = await httpClient.get<ApiSuccessEnvelope<BackendPagedResult<Message>>>(
      `/conversations/${conversationId}/messages`,
      { params: filters },
    )
    return unwrapPaged(response)
  },

  async sendMessage(conversationId: string, input: SendMessageInput): Promise<{ id: string }> {
    const response = await httpClient.post<{ data: { id: string } }>(`/conversations/${conversationId}/messages`, input)
    return unwrap(response)
  },

  async markConversationRead(conversationId: string): Promise<void> {
    await httpClient.post(`/conversations/${conversationId}/read`)
  },
}
