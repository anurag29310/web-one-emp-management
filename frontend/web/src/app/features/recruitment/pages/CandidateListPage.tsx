import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useCandidates } from '../hooks/useCandidates'
import { recruitmentRepository, type CandidateStatus } from '../api'
import { createCandidateFormSchema, type CreateCandidateFormValues } from '../types/recruitmentSchema'
import { CandidateStatusBadge } from '../components/CandidateStatusBadge'
import { useDesignations } from '@/app/features/designations/hooks/useDesignations'
import { useDepartments } from '@/app/features/departments/hooks/useDepartments'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

const STATUS_OPTIONS: CandidateStatus[] = ['Applied', 'Screening', 'Interviewing', 'Offered', 'Hired', 'Rejected', 'Withdrawn']

const inputClass =
  'w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'
const labelClass = 'mb-1 block text-sm font-medium text-ink-muted'

function RegisterCandidateForm({ onCreated }: { onCreated: (id: string) => void }) {
  const { designations } = useDesignations()
  const { departments } = useDepartments()
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateCandidateFormValues>({
    resolver: zodResolver(createCandidateFormSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      phoneNumber: '',
      designationId: '',
      departmentId: '',
      source: '',
      appliedDate: new Date().toISOString().slice(0, 10),
      notes: '',
    },
  })

  async function onSubmit(values: CreateCandidateFormValues) {
    setFormError(null)
    try {
      const { id } = await recruitmentRepository.createCandidate({
        firstName: values.firstName.trim(),
        lastName: values.lastName.trim(),
        email: values.email.trim(),
        phoneNumber: values.phoneNumber?.trim() || undefined,
        designationId: values.designationId,
        departmentId: values.departmentId || undefined,
        source: values.source?.trim() || undefined,
        appliedDate: values.appliedDate,
        notes: values.notes?.trim() || undefined,
      })
      reset()
      onCreated(id)
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to register candidate.')
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-3">
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="cand-first" className={labelClass}>
            First name
          </label>
          <input id="cand-first" aria-invalid={Boolean(errors.firstName)} className={inputClass} {...register('firstName')} />
          {errors.firstName && <p className="mt-1 text-xs text-danger">{errors.firstName.message}</p>}
        </div>
        <div>
          <label htmlFor="cand-last" className={labelClass}>
            Last name
          </label>
          <input id="cand-last" aria-invalid={Boolean(errors.lastName)} className={inputClass} {...register('lastName')} />
          {errors.lastName && <p className="mt-1 text-xs text-danger">{errors.lastName.message}</p>}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="cand-email" className={labelClass}>
            Email
          </label>
          <input id="cand-email" type="email" aria-invalid={Boolean(errors.email)} className={inputClass} {...register('email')} />
          {errors.email && <p className="mt-1 text-xs text-danger">{errors.email.message}</p>}
        </div>
        <div>
          <label htmlFor="cand-phone" className={labelClass}>
            Phone <span className="text-ink-tertiary">(optional)</span>
          </label>
          <input id="cand-phone" className={inputClass} {...register('phoneNumber')} />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="cand-designation" className={labelClass}>
            Designation applied for
          </label>
          <select id="cand-designation" aria-invalid={Boolean(errors.designationId)} className={inputClass} {...register('designationId')}>
            <option value="">Select designation…</option>
            {designations.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name}
              </option>
            ))}
          </select>
          {errors.designationId && <p className="mt-1 text-xs text-danger">{errors.designationId.message}</p>}
        </div>
        <div>
          <label htmlFor="cand-department" className={labelClass}>
            Department <span className="text-ink-tertiary">(optional)</span>
          </label>
          <select id="cand-department" className={inputClass} {...register('departmentId')}>
            <option value="">Unspecified</option>
            {departments.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="cand-source" className={labelClass}>
            Source <span className="text-ink-tertiary">(optional)</span>
          </label>
          <input id="cand-source" placeholder="LinkedIn, Referral, Job Board…" className={inputClass} {...register('source')} />
        </div>
        <div>
          <label htmlFor="cand-applied" className={labelClass}>
            Applied date
          </label>
          <input
            id="cand-applied"
            type="date"
            aria-invalid={Boolean(errors.appliedDate)}
            className={inputClass}
            {...register('appliedDate')}
          />
          {errors.appliedDate && <p className="mt-1 text-xs text-danger">{errors.appliedDate.message}</p>}
        </div>
      </div>

      <div>
        <label htmlFor="cand-notes" className={labelClass}>
          Notes
        </label>
        <textarea id="cand-notes" rows={2} className={inputClass} {...register('notes')} />
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
        {isSubmitting ? 'Registering…' : 'Register candidate'}
      </button>
    </form>
  )
}

export function CandidateListPage() {
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<CandidateStatus | ''>('')
  const { result, isLoading, error } = useCandidates({
    pageSize: 50,
    search: search || undefined,
    status: status || undefined,
  })
  const [isFormOpen, setIsFormOpen] = useState(false)
  const navigate = useNavigate()

  if (!canManage) {
    return (
      <div className="rounded-lg border border-hairline bg-surface-1 p-6">
        <p className="text-sm text-danger">You don't have permission to view candidates.</p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Candidates</h1>
          <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
        </div>
        <button
          type="button"
          onClick={() => setIsFormOpen((open) => !open)}
          className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
        >
          {isFormOpen ? 'Cancel' : 'Register candidate'}
        </button>
      </div>

      <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="Register candidate">
        <RegisterCandidateForm
          onCreated={(id) => {
            setIsFormOpen(false)
            navigate(`/candidates/${id}`)
          }}
        />
      </Modal>

      <div className="flex items-center gap-3">
        <input
          type="search"
          aria-label="Search candidates"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search name or email…"
          className="w-72 rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        />
        <select
          aria-label="Filter by status"
          value={status}
          onChange={(e) => setStatus(e.target.value as CandidateStatus | '')}
          className="w-48 rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        >
          <option value="">All statuses</option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
      </div>

      {error && (
        <p role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Candidate</th>
              <th className="px-4 py-3">Designation</th>
              <th className="px-4 py-3">Source</th>
              <th className="px-4 py-3">Applied</th>
              <th className="px-4 py-3">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={5}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && result?.data.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={5}>
                  No candidates found.
                </td>
              </tr>
            )}

            {!isLoading &&
              result?.data.map((candidate) => (
                <tr key={candidate.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link to={`/candidates/${candidate.id}`} className="font-medium text-ink hover:text-primary-hover">
                      {candidate.firstName} {candidate.lastName}
                    </Link>
                    <p className="font-mono text-xs text-ink-subtle">{candidate.candidateNumber}</p>
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{candidate.designationName ?? '—'}</td>
                  <td className="px-4 py-3 text-ink-muted">{candidate.source ?? '—'}</td>
                  <td className="px-4 py-3 text-ink-muted">{new Date(candidate.appliedDate).toLocaleDateString()}</td>
                  <td className="px-4 py-3">
                    <CandidateStatusBadge status={candidate.status} />
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
