import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { MessagingRepository } from './messagingRepository'
import { USER_ADMIN, USER_EMPLOYEE, USER_HR, USER_MANAGER } from './mockData'

// The mock repository holds its "database" as module-level mutable state, so each test needs a
// fresh module instance — otherwise mutations from one test would leak into the next.
async function loadRepository(): Promise<MessagingRepository> {
  const module = await import('./mockMessagingRepository')
  return module.mockMessagingRepository
}

beforeEach(() => {
  vi.resetModules()
})

const DIRECT_CONVERSATION_ID = 'e0000000-0000-0000-0000-000000000001'
const GROUP_CONVERSATION_ID = 'e0000000-0000-0000-0000-000000000002'

describe('mockMessagingRepository.listConversations', () => {
  it('excludes soft-deleted conversations and sorts by most recent activity', async () => {
    const repository = await loadRepository()
    const result = await repository.listConversations()

    expect(result.data.every((c) => !c.isDeleted)).toBe(true)
    const timestamps = result.data.map((c) => c.lastMessageAtUtc ?? '')
    expect(timestamps).toEqual([...timestamps].sort().reverse())
  })

  it('filters by title or participant name', async () => {
    const repository = await loadRepository()
    const result = await repository.listConversations({ search: 'Engineering Leads' })

    expect(result.data).toHaveLength(1)
    expect(result.data[0].title).toBe('Engineering Leads')
  })
})

describe('mockMessagingRepository.getConversationById', () => {
  it('throws a 404 AppError for an unknown id', async () => {
    const repository = await loadRepository()
    await expect(repository.getConversationById('does-not-exist')).rejects.toMatchObject({
      status: 404,
      code: 'NOT_FOUND',
    })
  })
})

describe('mockMessagingRepository.createConversation', () => {
  it('creates a group conversation when more than one participant is given', async () => {
    const repository = await loadRepository()
    const { id } = await repository.createConversation({
      participantUserIds: [USER_HR, USER_MANAGER],
      initialMessageBody: 'Kicking off the new project thread.',
    })

    const conversation = await repository.getConversationById(id)
    expect(conversation.isGroup).toBe(true)
    expect(conversation.lastMessagePreview).toBe('Kicking off the new project thread.')
  })

  it('records the initial message in the conversation thread', async () => {
    const repository = await loadRepository()
    const { id } = await repository.createConversation({
      participantUserIds: [USER_EMPLOYEE],
      initialMessageBody: 'Hey, quick question about your onboarding checklist.',
    })

    const messages = await repository.listMessages(id)
    expect(messages.data).toHaveLength(1)
    expect(messages.data[0].body).toBe('Hey, quick question about your onboarding checklist.')
  })
})

describe('mockMessagingRepository.sendMessage', () => {
  it('appends a message and updates the conversation preview', async () => {
    const repository = await loadRepository()
    await repository.sendMessage(DIRECT_CONVERSATION_ID, { body: 'Sent the doc over, take a look when free.' })

    const conversation = await repository.getConversationById(DIRECT_CONVERSATION_ID)
    expect(conversation.lastMessagePreview).toBe('Sent the doc over, take a look when free.')

    const messages = await repository.listMessages(DIRECT_CONVERSATION_ID)
    expect(messages.data.some((m) => m.body === 'Sent the doc over, take a look when free.')).toBe(true)
  })
})

describe('mockMessagingRepository.markConversationRead', () => {
  it('resets the unread count to zero', async () => {
    const repository = await loadRepository()
    const before = await repository.getConversationById(DIRECT_CONVERSATION_ID)
    expect(before.unreadCount).toBeGreaterThan(0)

    await repository.markConversationRead(DIRECT_CONVERSATION_ID)
    const after = await repository.getConversationById(DIRECT_CONVERSATION_ID)
    expect(after.unreadCount).toBe(0)
  })
})

describe('mockMessagingRepository.leaveConversation', () => {
  it('rejects leaving a direct (1:1) conversation', async () => {
    const repository = await loadRepository()
    await expect(repository.leaveConversation(DIRECT_CONVERSATION_ID)).rejects.toMatchObject({ status: 409 })
  })

  it('allows leaving a group conversation', async () => {
    const repository = await loadRepository()
    await expect(repository.leaveConversation(GROUP_CONVERSATION_ID)).resolves.toBeUndefined()
  })
})

describe('mockMessagingRepository.addParticipants', () => {
  it('adds a new participant and promotes the conversation to a group', async () => {
    const repository = await loadRepository()
    await repository.addParticipants(DIRECT_CONVERSATION_ID, { userIds: [USER_EMPLOYEE] })

    const conversation = await repository.getConversationById(DIRECT_CONVERSATION_ID)
    expect(conversation.isGroup).toBe(true)
    expect(conversation.participants.some((p) => p.userId === USER_EMPLOYEE)).toBe(true)
  })

  it('does not duplicate an already-active participant', async () => {
    const repository = await loadRepository()
    await repository.addParticipants(GROUP_CONVERSATION_ID, { userIds: [USER_ADMIN] })

    const conversation = await repository.getConversationById(GROUP_CONVERSATION_ID)
    expect(conversation.participants.filter((p) => p.userId === USER_ADMIN)).toHaveLength(1)
  })
})

describe('mockMessagingRepository.removeConversation / restoreConversation', () => {
  it('soft-deletes a conversation and restore brings it back', async () => {
    const repository = await loadRepository()
    await repository.removeConversation(GROUP_CONVERSATION_ID)
    const afterDelete = await repository.listConversations()
    expect(afterDelete.data.some((c) => c.id === GROUP_CONVERSATION_ID)).toBe(false)

    await repository.restoreConversation(GROUP_CONVERSATION_ID)
    const afterRestore = await repository.listConversations()
    expect(afterRestore.data.some((c) => c.id === GROUP_CONVERSATION_ID)).toBe(true)
  })
})
