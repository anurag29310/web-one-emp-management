import { useEffect, useState, type ChangeEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTask } from '../hooks/useTask'
import { useTaskComments } from '../hooks/useTaskComments'
import { useTaskAttachments } from '../hooks/useTaskAttachments'
import { taskRepository, type TaskAttachment } from '../api'
import {
  editTaskFormSchema,
  reassignTaskFormSchema,
  rejectTaskFormSchema,
  taskCommentFormSchema,
  type EditTaskFormValues,
  type ReassignTaskFormValues,
  type RejectTaskFormValues,
  type TaskCommentFormValues,
} from '../types/taskSchema'
import { TaskStatusBadge } from '../components/TaskStatusBadge'
import { TaskPriorityBadge } from '../components/TaskPriorityBadge'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { useClients } from '@/app/features/clients/hooks/useClients'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

const inputClass =
  'w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'
const labelClass = 'mb-1 block text-sm font-medium text-ink-muted'
const PRIORITY_OPTIONS = ['Low', 'Medium', 'High', 'Critical'] as const

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">{label}</p>
      <p className="mt-0.5 text-sm text-ink">{value}</p>
    </div>
  )
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function CommentsPanel({ taskId, readOnly }: { taskId: string; readOnly: boolean }) {
  const { comments, isLoading, error, refresh } = useTaskComments(taskId)
  const { result: employeeResult } = useEmployees({ pageSize: 100 })
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<TaskCommentFormValues>({ resolver: zodResolver(taskCommentFormSchema), defaultValues: { comment: '' } })

  function authorName(authorUserId: string): string {
    return employeeResult?.data.find((e) => e.id === authorUserId)?.fullName ?? 'Admin'
  }

  async function onSubmit(values: TaskCommentFormValues) {
    setFormError(null)
    try {
      await taskRepository.addComment(taskId, values.comment.trim())
      reset()
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to add comment.')
    }
  }

  return (
    <div className="rounded-lg border border-hairline bg-surface-1">
      <div className="border-b border-hairline p-6">
        <h2 className="text-[18px] font-medium text-ink">Notes</h2>
      </div>
      <div className="space-y-3 p-6">
        {error && <p className="text-sm text-danger">{error}</p>}
        {!isLoading && comments.length === 0 && <p className="text-sm text-ink-subtle">No notes yet.</p>}
        {comments.map((comment) => (
          <div key={comment.id} className="rounded-md bg-surface-2 p-3">
            <p className="text-sm text-ink">{comment.comment}</p>
            <p className="mt-1 text-xs text-ink-subtle">
              {authorName(comment.authorUserId)} · {new Date(comment.createdAtUtc).toLocaleString()}
            </p>
          </div>
        ))}

        {!readOnly && (
          <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-2 pt-2">
            <label htmlFor="new-comment" className="sr-only">
              Add a note
            </label>
            <textarea id="new-comment" rows={2} placeholder="Add a note…" className={inputClass} {...register('comment')} />
            {errors.comment && <p className="text-xs text-danger">{errors.comment.message}</p>}
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
              {isSubmitting ? 'Adding…' : 'Add note'}
            </button>
          </form>
        )}
      </div>
    </div>
  )
}

function AttachmentsPanel({ taskId, readOnly }: { taskId: string; readOnly: boolean }) {
  const { attachments, isLoading, error, refresh } = useTaskAttachments(taskId)
  const [panelError, setPanelError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [isUploading, setIsUploading] = useState(false)

  async function handleUpload(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    setPanelError(null)
    setIsUploading(true)
    try {
      await taskRepository.uploadAttachment(taskId, file)
      refresh()
    } catch (err) {
      setPanelError(err instanceof AppError ? err.message : 'Failed to upload attachment.')
    } finally {
      setIsUploading(false)
    }
  }

  async function handleDownload(attachment: TaskAttachment) {
    setPanelError(null)
    setBusyId(attachment.id)
    try {
      const { blob, fileName } = await taskRepository.downloadAttachment(attachment.id)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = fileName
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (err) {
      setPanelError(err instanceof AppError ? err.message : 'Failed to download attachment.')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="rounded-lg border border-hairline bg-surface-1">
      <div className="flex items-center justify-between border-b border-hairline p-6">
        <h2 className="text-[18px] font-medium text-ink">Photos &amp; attachments</h2>
        {!readOnly && (
          <label className="cursor-pointer rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3">
            {isUploading ? 'Uploading…' : 'Upload'}
            <input
              type="file"
              className="hidden"
              accept="application/pdf,image/jpeg,image/png"
              disabled={isUploading}
              onChange={(e) => void handleUpload(e)}
            />
          </label>
        )}
      </div>
      <div className="p-6">
        {(error || panelError) && <p className="mb-2 text-sm text-danger">{error ?? panelError}</p>}
        {!isLoading && attachments.length === 0 && <p className="text-sm text-ink-subtle">No attachments yet.</p>}
        <ul className="divide-y divide-hairline">
          {attachments.map((attachment) => (
            <li key={attachment.id} className="flex items-center justify-between py-2">
              <div>
                <p className="text-sm font-medium text-ink">{attachment.originalFileName}</p>
                <p className="text-xs text-ink-subtle">
                  {formatFileSize(attachment.fileSizeBytes)} · {new Date(attachment.uploadedAtUtc).toLocaleDateString()}
                </p>
              </div>
              <button
                type="button"
                disabled={busyId === attachment.id}
                onClick={() => void handleDownload(attachment)}
                className="text-xs font-medium text-primary-hover hover:underline disabled:cursor-not-allowed disabled:opacity-60"
              >
                Download
              </button>
            </li>
          ))}
        </ul>
      </div>
    </div>
  )
}

export function TaskDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { task, isLoading, error, refresh } = useTask(id)
  const { result: employeeResult } = useEmployees({ pageSize: 100 })
  const { result: clientResult } = useClients({ pageSize: 100, isActive: true })
  const { user } = useAuth()

  const [isEditing, setIsEditing] = useState(false)
  const [isReassigning, setIsReassigning] = useState(false)
  const [isRejecting, setIsRejecting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [isActing, setIsActing] = useState(false)

  const canManage = user?.role === 'Admin'
  const isAssignee = task?.assignedEmployeeId === user?.id
  const canAct = canManage || isAssignee
  const isReadOnly = task ? task.status === 'Completed' || task.status === 'Cancelled' : true

  const editForm = useForm<EditTaskFormValues>({ resolver: zodResolver(editTaskFormSchema) })
  const resetEditForm = editForm.reset
  const reassignForm = useForm<ReassignTaskFormValues>({
    resolver: zodResolver(reassignTaskFormSchema),
    defaultValues: { assignedEmployeeId: '' },
  })
  const rejectForm = useForm<RejectTaskFormValues>({ resolver: zodResolver(rejectTaskFormSchema), defaultValues: { reason: '' } })

  useEffect(() => {
    if (task) {
      resetEditForm({
        title: task.title,
        description: task.description ?? '',
        clientId: task.clientId ?? '',
        dueDate: task.dueDate ? task.dueDate.slice(0, 10) : '',
        priority: task.priority,
        notes: task.notes ?? '',
      })
    }
  }, [task, resetEditForm])

  async function onSaveEdit(values: EditTaskFormValues) {
    if (!task) return
    setFormError(null)
    try {
      await taskRepository.update(task.id, {
        title: values.title.trim(),
        description: values.description?.trim() || undefined,
        clientId: values.clientId || undefined,
        dueDate: values.dueDate || undefined,
        priority: values.priority,
        notes: values.notes?.trim() || undefined,
      })
      setIsEditing(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update task.')
    }
  }

  async function onReassign(values: ReassignTaskFormValues) {
    if (!task) return
    setFormError(null)
    try {
      await taskRepository.reassign(task.id, values.assignedEmployeeId)
      setIsReassigning(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to reassign task.')
    }
  }

  async function onReject(values: RejectTaskFormValues) {
    if (!task) return
    setFormError(null)
    try {
      await taskRepository.reject(task.id, values.reason?.trim() || undefined)
      setIsRejecting(false)
      rejectForm.reset()
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to reject task.')
    }
  }

  async function runAction(action: () => Promise<void>, failureMessage: string) {
    setIsActing(true)
    setFormError(null)
    try {
      await action()
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : failureMessage)
    } finally {
      setIsActing(false)
    }
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!task) return null

  const mapsUrl =
    task.clientLatitude != null && task.clientLongitude != null
      ? `https://www.google.com/maps/search/?api=1&query=${task.clientLatitude},${task.clientLongitude}`
      : null

  return (
    <div className="space-y-4">
      <Link to="/tasks" className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover">
        ← Back to tasks
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">{task.title}</h1>
            <p className="font-mono text-sm text-ink-subtle">{task.taskNumber}</p>
          </div>
          <div className="flex flex-wrap items-center justify-end gap-2">
            <TaskPriorityBadge priority={task.priority} />
            <TaskStatusBadge status={task.status} />

            {canAct && !isReadOnly && task.status === 'Assigned' && (
              <>
                <button
                  type="button"
                  disabled={isActing}
                  onClick={() => void runAction(() => taskRepository.accept(task.id), 'Failed to accept task.')}
                  className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
                >
                  Accept
                </button>
                <button
                  type="button"
                  onClick={() => setIsRejecting((open) => !open)}
                  className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
                >
                  {isRejecting ? 'Cancel' : 'Reject'}
                </button>
              </>
            )}
            {canAct && !isReadOnly && task.status === 'Accepted' && (
              <button
                type="button"
                disabled={isActing}
                onClick={() => void runAction(() => taskRepository.start(task.id), 'Failed to start task.')}
                className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
              >
                Start
              </button>
            )}
            {canAct && !isReadOnly && (task.status === 'InProgress' || task.status === 'OnHold') && (
              <>
                <button
                  type="button"
                  disabled={isActing}
                  onClick={() =>
                    void runAction(
                      () => taskRepository.updateProgress(task.id, task.status === 'InProgress' ? 'OnHold' : 'InProgress'),
                      'Failed to update progress.',
                    )
                  }
                  className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {task.status === 'InProgress' ? 'Put on hold' : 'Resume'}
                </button>
                <button
                  type="button"
                  disabled={isActing}
                  onClick={() => void runAction(() => taskRepository.complete(task.id), 'Failed to complete task.')}
                  className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
                >
                  Complete
                </button>
              </>
            )}

            {canManage && !isReadOnly && (
              <>
                <button
                  type="button"
                  onClick={() => setIsReassigning((open) => !open)}
                  className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
                >
                  {isReassigning ? 'Cancel' : 'Reassign'}
                </button>
                <button
                  type="button"
                  onClick={() => setIsEditing((open) => !open)}
                  className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
                >
                  {isEditing ? 'Cancel' : 'Edit'}
                </button>
                <button
                  type="button"
                  disabled={isActing}
                  onClick={() => void runAction(() => taskRepository.cancel(task.id), 'Failed to cancel task.')}
                  className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  Cancel task
                </button>
              </>
            )}
          </div>
        </div>

        {isRejecting && (
          <form onSubmit={rejectForm.handleSubmit(onReject)} noValidate className="space-y-2 border-b border-hairline p-6">
            <label htmlFor="reject-reason" className={labelClass}>
              Reason <span className="text-ink-tertiary">(optional)</span>
            </label>
            <textarea id="reject-reason" rows={2} className={inputClass} {...rejectForm.register('reason')} />
            <button
              type="submit"
              disabled={rejectForm.formState.isSubmitting}
              className="rounded-md bg-danger px-3 py-2 text-sm font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
            >
              Confirm reject
            </button>
          </form>
        )}

        <div className="grid grid-cols-2 gap-6 p-6">
          <Field label="Description" value={task.description ?? '—'} />
          <Field label="Assignee" value={task.assignedEmployeeName ?? '—'} />
          <Field label="Assigned date" value={new Date(task.assignedDate).toLocaleDateString()} />
          <Field label="Due date" value={task.dueDate ? new Date(task.dueDate).toLocaleDateString() : '—'} />
          <Field label="Notes" value={task.notes ?? '—'} />
          <Field label="Completed" value={task.completedAtUtc ? new Date(task.completedAtUtc).toLocaleString() : '—'} />
          <div>
            <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">Client</p>
            {task.clientName ? (
              <>
                <p className="mt-0.5 text-sm text-ink">{task.clientName}</p>
                {task.clientAddress && <p className="text-sm text-ink-muted">{task.clientAddress}</p>}
                {mapsUrl && (
                  <a href={mapsUrl} target="_blank" rel="noreferrer" className="text-sm text-primary hover:text-primary-hover">
                    Open in Maps
                  </a>
                )}
              </>
            ) : (
              <p className="mt-0.5 text-sm text-ink">No client visit</p>
            )}
          </div>
        </div>

        {formError && (
          <p role="alert" className="px-6 pb-6 text-sm text-danger">
            {formError}
          </p>
        )}
      </div>

      {canAct && (
        <>
          <CommentsPanel taskId={task.id} readOnly={isReadOnly} />
          <AttachmentsPanel taskId={task.id} readOnly={isReadOnly} />
        </>
      )}

      {canManage && (
        <Modal isOpen={isEditing} onClose={() => setIsEditing(false)} title="Edit task">
          <form onSubmit={editForm.handleSubmit(onSaveEdit)} noValidate className="space-y-3">
            <div>
              <label htmlFor="edit-task-title" className={labelClass}>
                Title
              </label>
              <input id="edit-task-title" className={inputClass} {...editForm.register('title')} />
              {editForm.formState.errors.title && (
                <p className="mt-1 text-xs text-danger">{editForm.formState.errors.title.message}</p>
              )}
            </div>
            <div>
              <label htmlFor="edit-task-description" className={labelClass}>
                Description
              </label>
              <textarea id="edit-task-description" rows={2} className={inputClass} {...editForm.register('description')} />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-task-client" className={labelClass}>
                  Client
                </label>
                <select id="edit-task-client" className={inputClass} {...editForm.register('clientId')}>
                  <option value="">No client visit</option>
                  {clientResult?.data.map((client) => (
                    <option key={client.id} value={client.id}>
                      {client.clientName}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label htmlFor="edit-task-due-date" className={labelClass}>
                  Due date
                </label>
                <input id="edit-task-due-date" type="date" className={inputClass} {...editForm.register('dueDate')} />
              </div>
            </div>
            <div>
              <label htmlFor="edit-task-priority" className={labelClass}>
                Priority
              </label>
              <select id="edit-task-priority" className={inputClass} {...editForm.register('priority')}>
                {PRIORITY_OPTIONS.map((p) => (
                  <option key={p} value={p}>
                    {p}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="edit-task-notes" className={labelClass}>
                Notes
              </label>
              <textarea id="edit-task-notes" rows={2} className={inputClass} {...editForm.register('notes')} />
            </div>
            {formError && (
              <p role="alert" className="text-sm text-danger">
                {formError}
              </p>
            )}
            <button
              type="submit"
              disabled={editForm.formState.isSubmitting}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
            >
              {editForm.formState.isSubmitting ? 'Saving…' : 'Save changes'}
            </button>
          </form>
        </Modal>
      )}

      {canManage && (
        <Modal isOpen={isReassigning} onClose={() => setIsReassigning(false)} title="Reassign task">
          <form onSubmit={reassignForm.handleSubmit(onReassign)} noValidate className="space-y-3">
            <div>
              <label htmlFor="reassign-employee" className={labelClass}>
                New assignee
              </label>
              <select id="reassign-employee" className={inputClass} {...reassignForm.register('assignedEmployeeId')}>
                <option value="">Select employee…</option>
                {employeeResult?.data.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.fullName}
                  </option>
                ))}
              </select>
              {reassignForm.formState.errors.assignedEmployeeId && (
                <p className="mt-1 text-xs text-danger">{reassignForm.formState.errors.assignedEmployeeId.message}</p>
              )}
            </div>
            {formError && (
              <p role="alert" className="text-sm text-danger">
                {formError}
              </p>
            )}
            <button
              type="submit"
              disabled={reassignForm.formState.isSubmitting}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
            >
              {reassignForm.formState.isSubmitting ? 'Reassigning…' : 'Reassign'}
            </button>
          </form>
        </Modal>
      )}
    </div>
  )
}
