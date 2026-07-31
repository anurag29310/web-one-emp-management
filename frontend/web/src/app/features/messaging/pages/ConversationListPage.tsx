import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useConversations } from '../hooks/useConversations'
import { messagingRepository } from '../api'
import { createConversationFormSchema, type CreateConversationFormValues } from '../types/messagingSchema'
import { useUsers } from '@/app/features/administration/hooks/useUsers'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

function timeAgo(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime()
  const diffMin = Math.round(diffMs / 60_000)
  if (diffMin < 1) return 'just now'
  if (diffMin < 60) return `${diffMin}m ago`
  const diffHr = Math.round(diffMin / 60)
  if (diffHr < 24) return `${diffHr}h ago`
  const diffDay = Math.round(diffHr / 24)
  return `${diffDay}d ago`
}

function NewConversationForm({ onCreated }: { onCreated: (id: string) => void }) {
  const [formError, setFormError] = useState<string | null>(null)
  const { user } = useAuth()
  const { users } = useUsers({ isActive: true })
  const otherUsers = users.filter((u) => u.id !== user?.id)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<CreateConversationFormValues>({
    resolver: zodResolver(createConversationFormSchema),
    defaultValues: { participantUserIds: [], title: '', initialMessageBody: '' },
  })

  async function onSubmit(values: CreateConversationFormValues) {
    setFormError(null)
    try {
      const { id } = await messagingRepository.createConversation({
        participantUserIds: values.participantUserIds,
        title: values.title?.trim() || undefined,
        initialMessageBody: values.initialMessageBody.trim(),
      })
      onCreated(id)
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to start conversation.')
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-3">
      <div>
        <label className="mb-1 block text-sm font-medium text-ink-muted">Participants</label>
        <div className="max-h-40 space-y-1 overflow-y-auto rounded-md border border-hairline-strong bg-surface-2 p-2">
          {otherUsers.length === 0 && <p className="px-1 py-1 text-sm text-ink-subtle">No other users found.</p>}
          {otherUsers.map((candidate) => (
            <label key={candidate.id} className="flex items-center gap-2 rounded px-1 py-1 text-sm text-ink hover:bg-surface-3">
              <input type="checkbox" value={candidate.id} {...register('participantUserIds')} />
              {candidate.userName} <span className="text-ink-subtle">({candidate.email})</span>
            </label>
          ))}
        </div>
        {errors.participantUserIds && <p className="mt-1 text-xs text-danger">{errors.participantUserIds.message}</p>}
      </div>

      <div>
        <label htmlFor="conversation-title" className="mb-1 block text-sm font-medium text-ink-muted">
          Title (optional, for group chats)
        </label>
        <input
          id="conversation-title"
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('title')}
        />
      </div>

      <div>
        <label htmlFor="conversation-first-message" className="mb-1 block text-sm font-medium text-ink-muted">
          First message
        </label>
        <textarea
          id="conversation-first-message"
          rows={3}
          aria-invalid={Boolean(errors.initialMessageBody)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('initialMessageBody')}
        />
        {errors.initialMessageBody && <p className="mt-1 text-xs text-danger">{errors.initialMessageBody.message}</p>}
      </div>

      {formError && (
        <p role="alert" className="text-sm text-danger">
          {formError}
        </p>
      )}

      <button
        type="submit"
        disabled={isSubmitting}
        className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isSubmitting ? 'Starting…' : 'Start conversation'}
      </button>
    </form>
  )
}

export function ConversationListPage() {
  const [search, setSearch] = useState('')
  const { result, isLoading, error, refresh } = useConversations({ pageSize: 50, search: search || undefined })
  const { user } = useAuth()
  const canStartConversation = user?.role === 'Admin'
  const [isFormOpen, setIsFormOpen] = useState(false)
  const navigate = useNavigate()

  function conversationLabel(participants: { userId: string; name: string }[], title: string | null) {
    if (title) return title
    const others = participants.filter((p) => p.userId !== user?.id)
    const names = (others.length > 0 ? others : participants).map((p) => p.name)
    return names.join(', ') || 'Conversation'
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Messages</h1>
          <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
        </div>
        {canStartConversation && (
          <button
            type="button"
            onClick={() => setIsFormOpen((open) => !open)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            {isFormOpen ? 'Cancel' : 'New conversation'}
          </button>
        )}
      </div>

      {canStartConversation && (
        <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="New conversation">
          <NewConversationForm
            onCreated={(id) => {
              setIsFormOpen(false)
              refresh()
              navigate(`/messages/${id}`)
            }}
          />
        </Modal>
      )}

      <input
        type="search"
        aria-label="Search conversations"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder="Search by title or participant…"
        className="w-72 rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
      />

      {error && (
        <p role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}

      <div className="divide-y divide-hairline overflow-hidden rounded-lg border border-hairline bg-surface-1">
        {isLoading &&
          Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="p-4">
              <div className="h-5 animate-pulse rounded bg-surface-2" />
            </div>
          ))}

        {!isLoading && result?.data.length === 0 && (
          <p className="px-4 py-8 text-center text-sm text-ink-subtle">No conversations found.</p>
        )}

        {!isLoading &&
          result?.data.map((conversation) => (
            <Link
              key={conversation.id}
              to={`/messages/${conversation.id}`}
              className="flex items-center justify-between gap-4 p-4 transition hover:bg-surface-2"
            >
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <p className="truncate font-medium text-ink">
                    {conversationLabel(conversation.participants, conversation.title)}
                  </p>
                  {conversation.isGroup && (
                    <span className="rounded-full bg-surface-2 px-2 py-0.5 text-[11px] font-medium text-ink-subtle ring-1 ring-inset ring-hairline-strong">
                      Group
                    </span>
                  )}
                </div>
                <p className="mt-0.5 truncate text-sm text-ink-subtle">{conversation.lastMessagePreview ?? 'No messages yet.'}</p>
              </div>
              <div className="flex shrink-0 items-center gap-2">
                {conversation.lastMessageAtUtc && (
                  <span className="text-xs text-ink-subtle">{timeAgo(conversation.lastMessageAtUtc)}</span>
                )}
                {conversation.unreadCount > 0 && (
                  <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1.5 text-[11px] font-semibold text-white">
                    {conversation.unreadCount > 9 ? '9+' : conversation.unreadCount}
                  </span>
                )}
              </div>
            </Link>
          ))}
      </div>
    </div>
  )
}
