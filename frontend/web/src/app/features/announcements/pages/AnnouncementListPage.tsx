import { useEffect, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useAuth } from '@/app/core/auth/useAuth'
import { AppError } from '@/app/shared/models/appError'
import { Modal } from '@/app/shared/components/Modal'
import { departmentRepository, type Department } from '@/app/features/departments/api'
import { useAnnouncements } from '../hooks/useAnnouncements'
import { announcementRepository } from '../api'
import { AnnouncementPriorityBadge } from '../components/AnnouncementPriorityBadge'
import {
  ANNOUNCEMENT_AUDIENCE_TYPES,
  ANNOUNCEMENT_PRIORITIES,
  ANNOUNCEMENT_TARGET_ROLES,
  announcementFormSchema,
  type AnnouncementFormValues,
} from '../types/announcementSchema'

const DEFAULT_VALUES: AnnouncementFormValues = {
  title: '',
  message: '',
  priority: 'Normal',
  audienceType: 'All',
  departmentId: '',
  targetRole: '',
  expiresAtUtc: '',
}

function CreateAnnouncementForm({ onCreated }: { onCreated: () => void }) {
  const [formError, setFormError] = useState<string | null>(null)
  const [departments, setDepartments] = useState<Department[]>([])
  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isSubmitting },
  } = useForm<AnnouncementFormValues>({
    resolver: zodResolver(announcementFormSchema),
    defaultValues: DEFAULT_VALUES,
  })
  const audienceType = useWatch({ control, name: 'audienceType' })

  useEffect(() => {
    let cancelled = false
    departmentRepository
      .list()
      .then((data) => {
        if (!cancelled) setDepartments(data)
      })
      .catch(() => {
        // Non-fatal: the department select simply stays empty if this fails.
      })
    return () => {
      cancelled = true
    }
  }, [])

  async function submit(values: AnnouncementFormValues) {
    setFormError(null)
    try {
      await announcementRepository.create({
        title: values.title.trim(),
        message: values.message.trim(),
        priority: values.priority,
        audienceType: values.audienceType,
        departmentId: values.audienceType === 'Department' ? values.departmentId || null : null,
        targetRole: values.audienceType === 'Role' ? values.targetRole || null : null,
        expiresAtUtc: values.expiresAtUtc ? new Date(values.expiresAtUtc).toISOString() : null,
      })
      onCreated()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to publish announcement.')
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="space-y-3">
      <div>
        <label htmlFor="ann-title" className="mb-1 block text-sm font-medium text-ink-muted">
          Title
        </label>
        <input
          id="ann-title"
          aria-invalid={Boolean(errors.title)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('title')}
        />
        {errors.title && <p className="mt-1 text-xs text-danger">{errors.title.message}</p>}
      </div>

      <div>
        <label htmlFor="ann-message" className="mb-1 block text-sm font-medium text-ink-muted">
          Message
        </label>
        <textarea
          id="ann-message"
          rows={4}
          aria-invalid={Boolean(errors.message)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('message')}
        />
        {errors.message && <p className="mt-1 text-xs text-danger">{errors.message.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="ann-priority" className="mb-1 block text-sm font-medium text-ink-muted">
            Priority
          </label>
          <select
            id="ann-priority"
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('priority')}
          >
            {ANNOUNCEMENT_PRIORITIES.map((priority) => (
              <option key={priority} value={priority}>
                {priority}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label htmlFor="ann-audience" className="mb-1 block text-sm font-medium text-ink-muted">
            Audience
          </label>
          <select
            id="ann-audience"
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('audienceType')}
          >
            {ANNOUNCEMENT_AUDIENCE_TYPES.map((type) => (
              <option key={type} value={type}>
                {type}
              </option>
            ))}
          </select>
        </div>
      </div>

      {audienceType === 'Department' && (
        <div>
          <label htmlFor="ann-department" className="mb-1 block text-sm font-medium text-ink-muted">
            Department
          </label>
          <select
            id="ann-department"
            aria-invalid={Boolean(errors.departmentId)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('departmentId')}
          >
            <option value="">Select a department…</option>
            {departments.map((department) => (
              <option key={department.id} value={department.id}>
                {department.name}
              </option>
            ))}
          </select>
          {errors.departmentId && <p className="mt-1 text-xs text-danger">{errors.departmentId.message}</p>}
        </div>
      )}

      {audienceType === 'Role' && (
        <div>
          <label htmlFor="ann-role" className="mb-1 block text-sm font-medium text-ink-muted">
            Role
          </label>
          <select
            id="ann-role"
            aria-invalid={Boolean(errors.targetRole)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('targetRole')}
          >
            <option value="">Select a role…</option>
            {ANNOUNCEMENT_TARGET_ROLES.map((role) => (
              <option key={role} value={role}>
                {role}
              </option>
            ))}
          </select>
          {errors.targetRole && <p className="mt-1 text-xs text-danger">{errors.targetRole.message}</p>}
        </div>
      )}

      <div>
        <label htmlFor="ann-expires" className="mb-1 block text-sm font-medium text-ink-muted">
          Expires on <span className="text-ink-tertiary">(optional)</span>
        </label>
        <input
          id="ann-expires"
          type="date"
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('expiresAtUtc')}
        />
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
        {isSubmitting ? 'Publishing…' : 'Publish announcement'}
      </button>
    </form>
  )
}

export function AnnouncementListPage() {
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'
  const [onlyUnread, setOnlyUnread] = useState(false)
  const { announcements, isLoading, error, refresh } = useAnnouncements(onlyUnread)
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [listError, setListError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)

  async function handleMarkRead(id: string) {
    setListError(null)
    setBusyId(id)
    try {
      await announcementRepository.markRead(id)
      refresh()
    } catch (err) {
      setListError(err instanceof AppError ? err.message : 'Failed to mark announcement as read.')
    } finally {
      setBusyId(null)
    }
  }

  async function handleRetract(id: string) {
    setListError(null)
    setBusyId(id)
    try {
      await announcementRepository.remove(id)
      refresh()
    } catch (err) {
      setListError(err instanceof AppError ? err.message : 'Failed to retract announcement.')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Announcements</h1>
          <p className="text-sm text-ink-subtle">{announcements.length} announcements</p>
        </div>
        {canManage && (
          <button
            type="button"
            onClick={() => setIsCreateOpen((open) => !open)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            {isCreateOpen ? 'Cancel' : 'New announcement'}
          </button>
        )}
      </div>

      {canManage && (
        <Modal isOpen={isCreateOpen} onClose={() => setIsCreateOpen(false)} title="New announcement">
          <CreateAnnouncementForm
            onCreated={() => {
              setIsCreateOpen(false)
              refresh()
            }}
          />
        </Modal>
      )}

      <label className="flex items-center gap-2 text-sm text-ink-muted">
        <input
          type="checkbox"
          checked={onlyUnread}
          onChange={(event) => setOnlyUnread(event.target.checked)}
          className="h-4 w-4 rounded border-hairline-strong bg-surface-2 text-primary focus:ring-primary-focus/50"
        />
        Show only unread
      </label>

      {(error || listError) && (
        <p role="alert" className="text-sm text-danger">
          {error ?? listError}
        </p>
      )}

      <div className="space-y-3">
        {isLoading &&
          Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="h-24 animate-pulse rounded-lg border border-hairline bg-surface-1" />
          ))}

        {!isLoading && announcements.length === 0 && (
          <div className="rounded-lg border border-hairline bg-surface-1 p-8 text-center text-sm text-ink-subtle">
            No announcements to show.
          </div>
        )}

        {!isLoading &&
          announcements.map((announcement) => (
            <div
              key={announcement.id}
              className={`rounded-lg border p-4 ${
                announcement.isReadByMe ? 'border-hairline bg-surface-1' : 'border-primary/40 bg-primary/5'
              }`}
            >
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="text-sm font-semibold text-ink">{announcement.title}</h2>
                    <AnnouncementPriorityBadge priority={announcement.priority} />
                    {!announcement.isReadByMe && (
                      <span className="rounded-full bg-primary/15 px-2 py-0.5 text-[10px] font-medium text-primary-hover ring-1 ring-inset ring-primary/30">
                        New
                      </span>
                    )}
                  </div>
                  <p className="mt-1.5 whitespace-pre-wrap text-sm text-ink-muted">{announcement.message}</p>
                  <p className="mt-2 text-xs text-ink-subtle">
                    {new Date(announcement.createdAtUtc).toLocaleString()}
                    {announcement.audienceType !== 'All' &&
                      ` · ${announcement.audienceType === 'Role' ? announcement.targetRole : 'Department-scoped'}`}
                  </p>
                </div>
                <div className="flex shrink-0 flex-col items-end gap-2">
                  {!announcement.isReadByMe && (
                    <button
                      type="button"
                      disabled={busyId === announcement.id}
                      onClick={() => void handleMarkRead(announcement.id)}
                      className="text-xs font-medium text-primary-hover hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      Mark as read
                    </button>
                  )}
                  {canManage && (
                    <button
                      type="button"
                      disabled={busyId === announcement.id}
                      onClick={() => void handleRetract(announcement.id)}
                      className="text-xs font-medium text-danger hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      Retract
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))}
      </div>
    </div>
  )
}
