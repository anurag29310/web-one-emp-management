import type { Conversation, Message } from './messagingRepository'

export const USER_ADMIN = '00000000-0000-0000-0000-000000000001'
export const USER_HR = '00000000-0000-0000-0000-000000000002'
export const USER_MANAGER = '00000000-0000-0000-0000-000000000003'
export const USER_EMPLOYEE = '00000000-0000-0000-0000-000000000004'

export const mockUserDisplayNames: Record<string, string> = {
  [USER_ADMIN]: 'Admin',
  [USER_HR]: 'HR User',
  [USER_MANAGER]: 'Manager',
  [USER_EMPLOYEE]: 'Employee',
}

const CONVERSATION_DIRECT_ADMIN_HR = 'e0000000-0000-0000-0000-000000000001'
const CONVERSATION_GROUP_LEADS = 'e0000000-0000-0000-0000-000000000002'
const CONVERSATION_DIRECT_HR_EMPLOYEE = 'e0000000-0000-0000-0000-000000000003'

export const mockConversations: Conversation[] = [
  {
    id: CONVERSATION_DIRECT_ADMIN_HR,
    title: null,
    isGroup: false,
    participants: [
      { userId: USER_ADMIN, name: mockUserDisplayNames[USER_ADMIN], joinedAtUtc: '2026-06-01T09:00:00Z', leftAtUtc: null },
      { userId: USER_HR, name: mockUserDisplayNames[USER_HR], joinedAtUtc: '2026-06-01T09:00:00Z', leftAtUtc: null },
    ],
    lastMessageAtUtc: '2026-07-28T14:32:00Z',
    lastMessagePreview: "Can you review the offer letter before it goes out?",
    unreadCount: 2,
    isDeleted: false,
    createdAtUtc: '2026-06-01T09:00:00Z',
    updatedAtUtc: '2026-07-28T14:32:00Z',
  },
  {
    id: CONVERSATION_GROUP_LEADS,
    title: 'Engineering Leads',
    isGroup: true,
    participants: [
      { userId: USER_ADMIN, name: mockUserDisplayNames[USER_ADMIN], joinedAtUtc: '2026-05-15T09:00:00Z', leftAtUtc: null },
      { userId: USER_MANAGER, name: mockUserDisplayNames[USER_MANAGER], joinedAtUtc: '2026-05-15T09:00:00Z', leftAtUtc: null },
      { userId: USER_EMPLOYEE, name: mockUserDisplayNames[USER_EMPLOYEE], joinedAtUtc: '2026-05-15T09:00:00Z', leftAtUtc: null },
    ],
    lastMessageAtUtc: '2026-07-20T11:05:00Z',
    lastMessagePreview: 'Sounds good, see everyone at standup.',
    unreadCount: 0,
    isDeleted: false,
    createdAtUtc: '2026-05-15T09:00:00Z',
    updatedAtUtc: '2026-07-20T11:05:00Z',
  },
  {
    id: CONVERSATION_DIRECT_HR_EMPLOYEE,
    title: null,
    isGroup: false,
    participants: [
      { userId: USER_HR, name: mockUserDisplayNames[USER_HR], joinedAtUtc: '2026-07-25T09:00:00Z', leftAtUtc: null },
      { userId: USER_EMPLOYEE, name: mockUserDisplayNames[USER_EMPLOYEE], joinedAtUtc: '2026-07-25T09:00:00Z', leftAtUtc: null },
    ],
    lastMessageAtUtc: '2026-07-25T09:00:00Z',
    lastMessagePreview: 'Welcome aboard! Let me know if you have any onboarding questions.',
    unreadCount: 1,
    isDeleted: false,
    createdAtUtc: '2026-07-25T09:00:00Z',
    updatedAtUtc: null,
  },
]

export const mockMessages: Message[] = [
  {
    id: 'f0000000-0000-0000-0000-000000000001',
    conversationId: CONVERSATION_DIRECT_ADMIN_HR,
    senderUserId: USER_HR,
    senderName: mockUserDisplayNames[USER_HR],
    body: 'Hey, do you have a minute for the Q3 headcount plan?',
    sentAtUtc: '2026-07-28T14:10:00Z',
  },
  {
    id: 'f0000000-0000-0000-0000-000000000002',
    conversationId: CONVERSATION_DIRECT_ADMIN_HR,
    senderUserId: USER_ADMIN,
    senderName: mockUserDisplayNames[USER_ADMIN],
    body: 'Sure, give me 10 minutes.',
    sentAtUtc: '2026-07-28T14:12:00Z',
  },
  {
    id: 'f0000000-0000-0000-0000-000000000003',
    conversationId: CONVERSATION_DIRECT_ADMIN_HR,
    senderUserId: USER_HR,
    senderName: mockUserDisplayNames[USER_HR],
    body: 'Can you review the offer letter before it goes out?',
    sentAtUtc: '2026-07-28T14:32:00Z',
  },
  {
    id: 'f0000000-0000-0000-0000-000000000004',
    conversationId: CONVERSATION_GROUP_LEADS,
    senderUserId: USER_MANAGER,
    senderName: mockUserDisplayNames[USER_MANAGER],
    body: "Let's move tomorrow's standup 30 minutes earlier.",
    sentAtUtc: '2026-07-20T11:00:00Z',
  },
  {
    id: 'f0000000-0000-0000-0000-000000000005',
    conversationId: CONVERSATION_GROUP_LEADS,
    senderUserId: USER_EMPLOYEE,
    senderName: mockUserDisplayNames[USER_EMPLOYEE],
    body: 'Sounds good, see everyone at standup.',
    sentAtUtc: '2026-07-20T11:05:00Z',
  },
  {
    id: 'f0000000-0000-0000-0000-000000000006',
    conversationId: CONVERSATION_DIRECT_HR_EMPLOYEE,
    senderUserId: USER_HR,
    senderName: mockUserDisplayNames[USER_HR],
    body: 'Welcome aboard! Let me know if you have any onboarding questions.',
    sentAtUtc: '2026-07-25T09:00:00Z',
  },
]
