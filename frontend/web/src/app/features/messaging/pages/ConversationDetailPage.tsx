import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useConversation } from '../hooks/useConversation'
import { useMessages } from '../hooks/useMessages'
import { messagingRepository } from '../api'
import {
  addParticipantsFormSchema,
  sendMessageFormSchema,
  type AddParticipantsFormValues,
  type SendMessageFormValues,
} from '../types/messagingSchema'
import { useUsers } from '@/app/features/administration/hooks/useUsers'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

function timestamp(iso: string): string {
  return new Date(iso).toLocaleString()
}

export function ConversationDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { conversation, isLoading, error, refresh } = useConversation(id)
  const { messages, isLoading: isLoadingMessages, error: messagesError, refresh: refreshMessages } = useMessages(id)
  const { user } = useAuth()
  const canModerate = user?.role === 'Admin' || user?.role === 'HR'
  const canAddParticipants = user?.role === 'Admin'

  const [isAddingParticipants, setIsAddingParticipants] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [isLeaving, setIsLeaving] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)

  const sendForm = useForm<SendMessageFormValues>({
    resolver: zodResolver(sendMessageFormSchema),
    defaultValues: { body: '' },
  })
  const addParticipantsForm = useForm<AddParticipantsFormValues>({
    resolver: zodResolver(addParticipantsFormSchema),
    defaultValues: { userIds: [] },
  })
  const { users: allUsers } = useUsers({ isActive: true }, canAddParticipants)

  useEffect(() => {
    if (id) {
      messagingRepository.markConversationRead(id).catch(() => {
        // Non-critical — a failed read receipt just leaves the unread badge stale until next visit.
      })
    }
  }, [id])

  async function onSendMessage(values: SendMessageFormValues) {
    if (!id) return
    setFormError(null)
    try {
      await messagingRepository.sendMessage(id, { body: values.body.trim() })
      sendForm.reset()
      refreshMessages()
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to send message.')
    }
  }

  async function onAddParticipants(values: AddParticipantsFormValues) {
    if (!id) return
    setFormError(null)
    try {
      await messagingRepository.addParticipants(id, { userIds: values.userIds })
      addParticipantsForm.reset()
      setIsAddingParticipants(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to add participants.')
    }
  }

  async function handleLeave() {
    if (!id) return
    setIsLeaving(true)
    setFormError(null)
    try {
      await messagingRepository.leaveConversation(id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to leave conversation.')
    } finally {
      setIsLeaving(false)
    }
  }

  async function handleDelete() {
    if (!id) return
    setIsDeleting(true)
    setFormError(null)
    try {
      await messagingRepository.removeConversation(id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete conversation.')
    } finally {
      setIsDeleting(false)
    }
  }

  async function handleRestore() {
    if (!id) return
    setFormError(null)
    try {
      await messagingRepository.restoreConversation(id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to restore conversation.')
    }
  }

  if (isLoading) {
    return <div className="h-64 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!conversation) return null

  const activeParticipants = conversation.participants.filter((p) => !p.leftAtUtc)
  const isActiveParticipant = conversation.participants.some((p) => p.userId === user?.id && !p.leftAtUtc)
  const otherParticipants = activeParticipants.filter((p) => p.userId !== user?.id)
  const heading = conversation.title ?? (otherParticipants.length > 0 ? otherParticipants.map((p) => p.name).join(', ') : 'Conversation')
  const eligibleParticipants = allUsers.filter(
    (candidate) => !activeParticipants.some((p) => p.userId === candidate.id),
  )

  return (
    <div className="flex h-[calc(100vh-8rem)] flex-col space-y-4">
      <Link to="/messages" className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover">
        ← Back to messages
      </Link>

      <div className="flex items-center justify-between rounded-lg border border-hairline bg-surface-1 p-4">
        <div>
          <h1 className="text-[20px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">{heading}</h1>
          <p className="text-sm text-ink-subtle">
            {activeParticipants.map((p) => p.name).join(', ')}
            {conversation.isDeleted && ' · Deleted'}
          </p>
        </div>
        <div className="flex items-center gap-2">
          {conversation.isDeleted && canModerate && (
            <button
              type="button"
              onClick={() => void handleRestore()}
              className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
            >
              Restore
            </button>
          )}
          {!conversation.isDeleted && canAddParticipants && (
            <button
              type="button"
              onClick={() => setIsAddingParticipants((open) => !open)}
              className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
            >
              {isAddingParticipants ? 'Cancel' : 'Add participants'}
            </button>
          )}
          {!conversation.isDeleted && conversation.isGroup && isActiveParticipant && (
            <button
              type="button"
              disabled={isLeaving}
              onClick={() => void handleLeave()}
              className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isLeaving ? 'Leaving…' : 'Leave'}
            </button>
          )}
          {!conversation.isDeleted && canModerate && (
            <button
              type="button"
              disabled={isDeleting}
              onClick={() => void handleDelete()}
              className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isDeleting ? 'Deleting…' : 'Delete'}
            </button>
          )}
        </div>
      </div>

      {formError && !isAddingParticipants && (
        <p role="alert" className="text-sm text-danger">
          {formError}
        </p>
      )}

      <div className="flex-1 space-y-3 overflow-y-auto rounded-lg border border-hairline bg-surface-1 p-4">
        {isLoadingMessages && <div className="h-5 animate-pulse rounded bg-surface-2" />}
        {!isLoadingMessages && messagesError && <p className="text-sm text-danger">{messagesError}</p>}
        {!isLoadingMessages && !messagesError && messages.length === 0 && (
          <p className="text-center text-sm text-ink-subtle">No messages yet.</p>
        )}
        {!isLoadingMessages &&
          messages.map((message) => {
            const isOwn = message.senderUserId === user?.id
            return (
              <div key={message.id} className={`flex ${isOwn ? 'justify-end' : 'justify-start'}`}>
                <div
                  className={`max-w-[70%] rounded-lg px-3 py-2 text-sm ${
                    isOwn ? 'bg-primary text-white' : 'bg-surface-2 text-ink'
                  }`}
                >
                  {!isOwn && <p className="mb-0.5 text-[11px] font-medium text-ink-subtle">{message.senderName ?? 'Unknown'}</p>}
                  <p className="whitespace-pre-wrap">{message.body}</p>
                  <p className={`mt-1 text-[11px] ${isOwn ? 'text-white/70' : 'text-ink-subtle'}`}>{timestamp(message.sentAtUtc)}</p>
                </div>
              </div>
            )
          })}
      </div>

      {!conversation.isDeleted && isActiveParticipant && (
        <form onSubmit={sendForm.handleSubmit(onSendMessage)} noValidate className="flex items-start gap-2">
          <div className="flex-1">
            <textarea
              rows={2}
              aria-label="Message"
              placeholder="Type a message…"
              aria-invalid={Boolean(sendForm.formState.errors.body)}
              className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              {...sendForm.register('body')}
            />
            {sendForm.formState.errors.body && <p className="mt-1 text-xs text-danger">{sendForm.formState.errors.body.message}</p>}
          </div>
          <button
            type="submit"
            disabled={sendForm.formState.isSubmitting}
            className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
          >
            Send
          </button>
        </form>
      )}

      <Modal isOpen={isAddingParticipants} onClose={() => setIsAddingParticipants(false)} title="Add participants">
        <form onSubmit={addParticipantsForm.handleSubmit(onAddParticipants)} noValidate className="space-y-3">
          <div className="max-h-40 space-y-1 overflow-y-auto rounded-md border border-hairline-strong bg-surface-2 p-2">
            {eligibleParticipants.length === 0 && <p className="px-1 py-1 text-sm text-ink-subtle">No other users available.</p>}
            {eligibleParticipants.map((candidate) => (
              <label key={candidate.id} className="flex items-center gap-2 rounded px-1 py-1 text-sm text-ink hover:bg-surface-3">
                <input type="checkbox" value={candidate.id} {...addParticipantsForm.register('userIds')} />
                {candidate.userName} <span className="text-ink-subtle">({candidate.email})</span>
              </label>
            ))}
          </div>
          {addParticipantsForm.formState.errors.userIds && (
            <p className="text-xs text-danger">{addParticipantsForm.formState.errors.userIds.message}</p>
          )}
          {formError && (
            <p role="alert" className="text-sm text-danger">
              {formError}
            </p>
          )}
          <button
            type="submit"
            disabled={addParticipantsForm.formState.isSubmitting}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
          >
            {addParticipantsForm.formState.isSubmitting ? 'Adding…' : 'Add'}
          </button>
        </form>
      </Modal>
    </div>
  )
}
