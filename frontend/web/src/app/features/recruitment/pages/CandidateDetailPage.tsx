import { useEffect, useState, type ChangeEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useCandidate } from '../hooks/useCandidate'
import { useCandidateAttachments } from '../hooks/useCandidateAttachments'
import { useOffers } from '../hooks/useOffers'
import { recruitmentRepository, type CandidateAttachment } from '../api'
import {
  convertToEmployeeFormSchema,
  editCandidateFormSchema,
  reasonFormSchema,
  type ConvertToEmployeeFormValues,
  type EditCandidateFormValues,
  type ReasonFormValues,
} from '../types/recruitmentSchema'
import { CandidateStatusBadge } from '../components/CandidateStatusBadge'
import { InterviewsPanel } from '../components/InterviewsPanel'
import { OffersPanel } from '../components/OffersPanel'
import { ChecklistPanel } from '../components/ChecklistPanel'
import { useDesignations } from '@/app/features/designations/hooks/useDesignations'
import { useDepartments } from '@/app/features/departments/hooks/useDepartments'
import { useOfficeLocations } from '@/app/features/office-locations/hooks/useOfficeLocations'
import { useTeams } from '@/app/features/teams/hooks/useTeams'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

const inputClass =
  'w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'
const labelClass = 'mb-1 block text-sm font-medium text-ink-muted'

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

type TerminalAction = 'reject' | 'withdraw'

export function CandidateDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { candidate, isLoading, error, refresh } = useCandidate(id)
  const { attachments, isLoading: attachmentsLoading, error: attachmentsError, refresh: refreshAttachments } =
    useCandidateAttachments(id)
  const { offers } = useOffers(id)
  const { designations } = useDesignations()
  const { departments } = useDepartments()
  const { officeLocations } = useOfficeLocations()
  const { teams } = useTeams()
  const { result: employeeResult } = useEmployees({ pageSize: 100 })
  const { user } = useAuth()
  const navigate = useNavigate()

  const canManage = user?.role === 'Admin' || user?.role === 'HR'

  const [isEditing, setIsEditing] = useState(false)
  const [terminalAction, setTerminalAction] = useState<TerminalAction | null>(null)
  const [isConverting, setIsConverting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [isActing, setIsActing] = useState(false)
  const [isUploading, setIsUploading] = useState(false)

  const editForm = useForm<EditCandidateFormValues>({ resolver: zodResolver(editCandidateFormSchema) })
  const resetEditForm = editForm.reset
  const reasonForm = useForm<ReasonFormValues>({ resolver: zodResolver(reasonFormSchema), defaultValues: { reason: '' } })
  const convertForm = useForm<ConvertToEmployeeFormValues>({
    resolver: zodResolver(convertToEmployeeFormSchema),
    defaultValues: { employeeCode: '', officeLocationId: '', teamId: '', managerId: '', joinDate: '' },
  })

  useEffect(() => {
    if (candidate) {
      resetEditForm({
        firstName: candidate.firstName,
        lastName: candidate.lastName,
        email: candidate.email,
        phoneNumber: candidate.phoneNumber ?? '',
        designationId: candidate.designationId,
        departmentId: candidate.departmentId ?? '',
        source: candidate.source ?? '',
        notes: candidate.notes ?? '',
      })
    }
  }, [candidate, resetEditForm])

  async function onSaveEdit(values: EditCandidateFormValues) {
    if (!candidate) return
    setFormError(null)
    try {
      await recruitmentRepository.updateCandidate(candidate.id, {
        firstName: values.firstName.trim(),
        lastName: values.lastName.trim(),
        email: values.email.trim(),
        phoneNumber: values.phoneNumber?.trim() || undefined,
        designationId: values.designationId,
        departmentId: values.departmentId || undefined,
        source: values.source?.trim() || undefined,
        notes: values.notes?.trim() || undefined,
      })
      setIsEditing(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update candidate.')
    }
  }

  async function onTerminalAction(values: ReasonFormValues) {
    if (!candidate || !terminalAction) return
    setFormError(null)
    try {
      if (terminalAction === 'reject') {
        await recruitmentRepository.rejectCandidate(candidate.id, values.reason?.trim() || undefined)
      } else {
        await recruitmentRepository.withdrawCandidate(candidate.id, values.reason?.trim() || undefined)
      }
      setTerminalAction(null)
      reasonForm.reset()
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to record the decision.')
    }
  }

  async function onConvert(values: ConvertToEmployeeFormValues) {
    if (!candidate) return
    setFormError(null)
    try {
      await recruitmentRepository.convertToEmployee(candidate.id, {
        employeeCode: values.employeeCode.trim(),
        officeLocationId: values.officeLocationId,
        teamId: values.teamId || undefined,
        managerId: values.managerId || undefined,
        joinDate: values.joinDate || undefined,
      })
      setIsConverting(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to convert candidate to employee.')
    }
  }

  async function handleUpload(event: ChangeEvent<HTMLInputElement>) {
    if (!candidate) return
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    setFormError(null)
    setIsUploading(true)
    try {
      await recruitmentRepository.uploadCandidateAttachment(candidate.id, file)
      refreshAttachments()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to upload attachment.')
    } finally {
      setIsUploading(false)
    }
  }

  async function handleDownload(attachment: CandidateAttachment) {
    setFormError(null)
    try {
      const { blob, fileName } = await recruitmentRepository.downloadCandidateAttachment(attachment.id)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = fileName
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to download attachment.')
    }
  }

  async function handleDelete() {
    if (!candidate) return
    setIsActing(true)
    setFormError(null)
    try {
      await recruitmentRepository.deleteCandidate(candidate.id)
      navigate('/candidates')
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete candidate.')
      setIsActing(false)
    }
  }

  if (!canManage) {
    return (
      <div className="rounded-lg border border-hairline bg-surface-1 p-6">
        <p className="text-sm text-danger">You don't have permission to view this candidate.</p>
      </div>
    )
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!candidate) return null

  const isTerminal = candidate.status === 'Rejected' || candidate.status === 'Withdrawn' || candidate.status === 'Hired'
  const hasAcceptedOffer = offers.some((o) => o.status === 'Accepted')
  const canConvert = candidate.status !== 'Hired' && hasAcceptedOffer

  return (
    <div className="space-y-4">
      <Link to="/candidates" className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover">
        ← Back to candidates
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">
              {candidate.firstName} {candidate.lastName}
            </h1>
            <p className="font-mono text-sm text-ink-subtle">{candidate.candidateNumber}</p>
          </div>
          <div className="flex flex-wrap items-center justify-end gap-2">
            <CandidateStatusBadge status={candidate.status} />
            {canConvert && (
              <button
                type="button"
                onClick={() => setIsConverting((open) => !open)}
                className="rounded-md bg-success px-3 py-2 text-sm font-medium text-white transition hover:opacity-90"
              >
                {isConverting ? 'Cancel' : 'Convert to employee'}
              </button>
            )}
            {!isTerminal && (
              <>
                <button
                  type="button"
                  onClick={() => setIsEditing((open) => !open)}
                  className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
                >
                  {isEditing ? 'Cancel' : 'Edit'}
                </button>
                <button
                  type="button"
                  onClick={() => setTerminalAction((a) => (a === 'withdraw' ? null : 'withdraw'))}
                  className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
                >
                  Withdraw
                </button>
                <button
                  type="button"
                  onClick={() => setTerminalAction((a) => (a === 'reject' ? null : 'reject'))}
                  className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10"
                >
                  Reject
                </button>
              </>
            )}
            <button
              type="button"
              disabled={isActing}
              onClick={() => void handleDelete()}
              className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10 disabled:cursor-not-allowed disabled:opacity-60"
            >
              Delete
            </button>
          </div>
        </div>

        {terminalAction && (
          <form onSubmit={reasonForm.handleSubmit(onTerminalAction)} noValidate className="space-y-2 border-b border-hairline p-6">
            <label htmlFor="terminal-reason" className={labelClass}>
              {terminalAction === 'reject' ? 'Reason for rejection' : 'Reason for withdrawal'}{' '}
              <span className="text-ink-tertiary">(optional)</span>
            </label>
            <textarea id="terminal-reason" rows={2} className={inputClass} {...reasonForm.register('reason')} />
            <button
              type="submit"
              disabled={reasonForm.formState.isSubmitting}
              className={`rounded-md px-3 py-2 text-sm font-medium text-white transition disabled:cursor-not-allowed disabled:opacity-60 ${
                terminalAction === 'reject' ? 'bg-danger hover:opacity-90' : 'bg-ink hover:opacity-90'
              }`}
            >
              {terminalAction === 'reject' ? 'Confirm reject' : 'Confirm withdraw'}
            </button>
          </form>
        )}

        <div className="grid grid-cols-2 gap-6 p-6">
          <Field label="Email" value={candidate.email} />
          <Field label="Phone" value={candidate.phoneNumber ?? '—'} />
          <Field label="Designation" value={candidate.designationName ?? '—'} />
          <Field label="Department" value={candidate.departmentName ?? '—'} />
          <Field label="Source" value={candidate.source ?? '—'} />
          <Field label="Applied date" value={new Date(candidate.appliedDate).toLocaleDateString()} />
          <Field label="Notes" value={candidate.notes ?? '—'} />
        </div>

        {formError && (
          <p role="alert" className="px-6 pb-6 text-sm text-danger">
            {formError}
          </p>
        )}
      </div>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <h2 className="text-[18px] font-medium text-ink">Resume &amp; attachments</h2>
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
        </div>
        <div className="p-6">
          {attachmentsError && <p className="mb-2 text-sm text-danger">{attachmentsError}</p>}
          {!attachmentsLoading && attachments.length === 0 && <p className="text-sm text-ink-subtle">No attachments uploaded yet.</p>}
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
                  onClick={() => void handleDownload(attachment)}
                  className="text-xs font-medium text-primary-hover hover:underline"
                >
                  Download
                </button>
              </li>
            ))}
          </ul>
        </div>
      </div>

      <InterviewsPanel candidateId={candidate.id} canSchedule={!isTerminal} />
      <OffersPanel candidateId={candidate.id} canManage={!isTerminal} />
      <ChecklistPanel candidateId={candidate.id} canManage={true} />

      {!isTerminal && (
        <Modal isOpen={isEditing} onClose={() => setIsEditing(false)} title="Edit candidate">
          <form onSubmit={editForm.handleSubmit(onSaveEdit)} noValidate className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-first" className={labelClass}>
                  First name
                </label>
                <input id="edit-first" className={inputClass} {...editForm.register('firstName')} />
                {editForm.formState.errors.firstName && (
                  <p className="mt-1 text-xs text-danger">{editForm.formState.errors.firstName.message}</p>
                )}
              </div>
              <div>
                <label htmlFor="edit-last" className={labelClass}>
                  Last name
                </label>
                <input id="edit-last" className={inputClass} {...editForm.register('lastName')} />
                {editForm.formState.errors.lastName && (
                  <p className="mt-1 text-xs text-danger">{editForm.formState.errors.lastName.message}</p>
                )}
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-email" className={labelClass}>
                  Email
                </label>
                <input id="edit-email" type="email" className={inputClass} {...editForm.register('email')} />
                {editForm.formState.errors.email && <p className="mt-1 text-xs text-danger">{editForm.formState.errors.email.message}</p>}
              </div>
              <div>
                <label htmlFor="edit-phone" className={labelClass}>
                  Phone
                </label>
                <input id="edit-phone" className={inputClass} {...editForm.register('phoneNumber')} />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-designation" className={labelClass}>
                  Designation
                </label>
                <select id="edit-designation" className={inputClass} {...editForm.register('designationId')}>
                  <option value="">Select designation…</option>
                  {designations.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.name}
                    </option>
                  ))}
                </select>
                {editForm.formState.errors.designationId && (
                  <p className="mt-1 text-xs text-danger">{editForm.formState.errors.designationId.message}</p>
                )}
              </div>
              <div>
                <label htmlFor="edit-department" className={labelClass}>
                  Department
                </label>
                <select id="edit-department" className={inputClass} {...editForm.register('departmentId')}>
                  <option value="">Unspecified</option>
                  {departments.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <div>
              <label htmlFor="edit-source" className={labelClass}>
                Source
              </label>
              <input id="edit-source" className={inputClass} {...editForm.register('source')} />
            </div>
            <div>
              <label htmlFor="edit-notes" className={labelClass}>
                Notes
              </label>
              <textarea id="edit-notes" rows={2} className={inputClass} {...editForm.register('notes')} />
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

      {canConvert && (
        <Modal isOpen={isConverting} onClose={() => setIsConverting(false)} title="Convert to employee">
          <form onSubmit={convertForm.handleSubmit(onConvert)} noValidate className="space-y-3">
            <div>
              <label htmlFor="convert-code" className={labelClass}>
                Employee code
              </label>
              <input id="convert-code" placeholder="EMP-1042" className={inputClass} {...convertForm.register('employeeCode')} />
              {convertForm.formState.errors.employeeCode && (
                <p className="mt-1 text-xs text-danger">{convertForm.formState.errors.employeeCode.message}</p>
              )}
            </div>
            <div>
              <label htmlFor="convert-office" className={labelClass}>
                Office location
              </label>
              <select id="convert-office" className={inputClass} {...convertForm.register('officeLocationId')}>
                <option value="">Select office location…</option>
                {officeLocations.map((o) => (
                  <option key={o.id} value={o.id}>
                    {o.name}
                  </option>
                ))}
              </select>
              {convertForm.formState.errors.officeLocationId && (
                <p className="mt-1 text-xs text-danger">{convertForm.formState.errors.officeLocationId.message}</p>
              )}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="convert-team" className={labelClass}>
                  Team <span className="text-ink-tertiary">(optional)</span>
                </label>
                <select id="convert-team" className={inputClass} {...convertForm.register('teamId')}>
                  <option value="">Unassigned</option>
                  {teams.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.name}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label htmlFor="convert-manager" className={labelClass}>
                  Manager <span className="text-ink-tertiary">(optional)</span>
                </label>
                <select id="convert-manager" className={inputClass} {...convertForm.register('managerId')}>
                  <option value="">Unassigned</option>
                  {employeeResult?.data.map((e) => (
                    <option key={e.id} value={e.id}>
                      {e.fullName}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <div>
              <label htmlFor="convert-join-date" className={labelClass}>
                Join date override <span className="text-ink-tertiary">(optional, defaults to the offer's joining date)</span>
              </label>
              <input id="convert-join-date" type="date" className={inputClass} {...convertForm.register('joinDate')} />
            </div>
            {formError && (
              <p role="alert" className="text-sm text-danger">
                {formError}
              </p>
            )}
            <button
              type="submit"
              disabled={convertForm.formState.isSubmitting}
              className="rounded-md bg-success px-3 py-2 text-sm font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {convertForm.formState.isSubmitting ? 'Converting…' : 'Convert to employee'}
            </button>
          </form>
        </Modal>
      )}
    </div>
  )
}
