import { describe, expect, it } from 'vitest'
import { addParticipantsFormSchema, createConversationFormSchema, sendMessageFormSchema } from './messagingSchema'

describe('createConversationFormSchema', () => {
  const validInput = {
    participantUserIds: ['user-1'],
    title: '',
    initialMessageBody: 'Hey, got a minute?',
  }

  it('accepts a valid single-participant conversation', () => {
    expect(createConversationFormSchema.safeParse(validInput).success).toBe(true)
  })

  it('requires at least one participant', () => {
    const result = createConversationFormSchema.safeParse({ ...validInput, participantUserIds: [] })
    expect(result.success).toBe(false)
  })

  it('requires a non-empty initial message within 4000 characters', () => {
    expect(createConversationFormSchema.safeParse({ ...validInput, initialMessageBody: '' }).success).toBe(false)
    expect(
      createConversationFormSchema.safeParse({ ...validInput, initialMessageBody: 'a'.repeat(4001) }).success,
    ).toBe(false)
  })

  it('allows an omitted title for a direct conversation', () => {
    const { title: _title, ...withoutTitle } = validInput
    expect(createConversationFormSchema.safeParse(withoutTitle).success).toBe(true)
  })
})

describe('sendMessageFormSchema', () => {
  it('rejects an empty message body', () => {
    expect(sendMessageFormSchema.safeParse({ body: '' }).success).toBe(false)
  })

  it('rejects a message body over 4000 characters', () => {
    expect(sendMessageFormSchema.safeParse({ body: 'a'.repeat(4001) }).success).toBe(false)
  })

  it('accepts a normal message', () => {
    expect(sendMessageFormSchema.safeParse({ body: 'Sounds good, thanks!' }).success).toBe(true)
  })
})

describe('addParticipantsFormSchema', () => {
  it('requires at least one user id', () => {
    expect(addParticipantsFormSchema.safeParse({ userIds: [] }).success).toBe(false)
    expect(addParticipantsFormSchema.safeParse({ userIds: ['user-2'] }).success).toBe(true)
  })
})
