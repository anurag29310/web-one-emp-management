import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useCompany } from '../hooks/useCompany'
import { companyRepository } from '../api'
import { companyFormSchema, type CompanyFormValues } from '../types/companySchema'
import { CompanyFormFields } from '../components/CompanyFormFields'
import { CompanyStatusBadge } from '../components/CompanyStatusBadge'
import { ReasonPromptModal } from '../components/ReasonPromptModal'
import { AppError } from '@/app/shared/models/appError'
import { Modal } from '@/app/shared/components/Modal'

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">{label}</p>
      <p className="mt-0.5 text-sm text-ink">{value}</p>
    </div>
  )
}

export function CompanyDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { company, isLoading, error, refresh } = useCompany(id)

  const [isEditing, setIsEditing] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [statusMessage, setStatusMessage] = useState<string | null>(null)
  const [isBusy, setIsBusy] = useState(false)
  const [activePrompt, setActivePrompt] = useState<'suspend' | 'reject' | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CompanyFormValues>({ resolver: zodResolver(companyFormSchema) })

  useEffect(() => {
    if (company) {
      reset({
        name: company.name,
        timezone: company.timezone,
        currency: company.currency,
        logoUrl: company.logoUrl ?? '',
      })
    }
  }, [company, reset])

  async function onSave(values: CompanyFormValues) {
    if (!company) return
    setFormError(null)
    try {
      await companyRepository.update(company.id, {
        name: values.name.trim(),
        timezone: values.timezone.trim(),
        currency: values.currency.trim(),
        logoUrl: values.logoUrl?.trim() || undefined,
      })
      setIsEditing(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update company.')
    }
  }

  async function runAction(action: () => Promise<void>, failureMessage: string, successMessage?: string) {
    if (!company) return
    setIsBusy(true)
    setFormError(null)
    setStatusMessage(null)
    try {
      await action()
      if (successMessage) setStatusMessage(successMessage)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : failureMessage)
    } finally {
      setIsBusy(false)
    }
  }

  async function handleResetPassword(userId: string) {
    if (!company) return
    setIsBusy(true)
    setFormError(null)
    setStatusMessage(null)
    try {
      const token = await companyRepository.resetAdminPassword(company.id, userId)
      setStatusMessage(`Password reset token issued: ${token}`)
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to issue a password reset token.')
    } finally {
      setIsBusy(false)
    }
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!company) return null

  const canActivate = !company.isDeleted && (company.status === 'Suspended' || company.status === 'Inactive')
  const canSuspend = !company.isDeleted && (company.status === 'Active' || company.status === 'Trial')
  const isPendingApproval = !company.isDeleted && company.status === 'PendingApproval'

  return (
    <div className="space-y-4">
      <Link
        to="/platform/companies"
        className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover"
      >
        ← Back to companies
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-hairline p-6">
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">{company.name}</h1>
            <p className="text-sm text-ink-subtle">{company.employeeCount} employees</p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <CompanyStatusBadge status={company.status} />
            <button
              type="button"
              onClick={() => setIsEditing((editing) => !editing)}
              className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
            >
              {isEditing ? 'Cancel' : 'Edit'}
            </button>

            {company.isDeleted ? (
              <button
                type="button"
                disabled={isBusy}
                onClick={() => void runAction(() => companyRepository.restore(company.id), 'Failed to restore company.')}
                className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3 disabled:cursor-not-allowed disabled:opacity-60"
              >
                Restore
              </button>
            ) : (
              <>
                {isPendingApproval && (
                  <>
                    <button
                      type="button"
                      disabled={isBusy}
                      onClick={() =>
                        void runAction(() => companyRepository.approve(company.id), 'Failed to approve company.')
                      }
                      className="rounded-md bg-success/15 px-3 py-2 text-sm font-medium text-success transition hover:bg-success/25 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      Approve
                    </button>
                    <button
                      type="button"
                      disabled={isBusy}
                      onClick={() => setActivePrompt('reject')}
                      className="rounded-md bg-danger/15 px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/25 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      Reject
                    </button>
                  </>
                )}
                {canActivate && (
                  <button
                    type="button"
                    disabled={isBusy}
                    onClick={() =>
                      void runAction(() => companyRepository.activate(company.id), 'Failed to activate company.')
                    }
                    className="rounded-md bg-success/15 px-3 py-2 text-sm font-medium text-success transition hover:bg-success/25 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Activate
                  </button>
                )}
                {canSuspend && (
                  <button
                    type="button"
                    disabled={isBusy}
                    onClick={() => setActivePrompt('suspend')}
                    className="rounded-md bg-danger/15 px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/25 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Suspend
                  </button>
                )}
                <button
                  type="button"
                  disabled={isBusy}
                  onClick={() =>
                    void runAction(
                      () => companyRepository.forceLogout(company.id),
                      'Failed to force-logout company users.',
                      'Every user of this company has been logged out.',
                    )
                  }
                  className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  Force logout
                </button>
                <button
                  type="button"
                  disabled={isBusy}
                  onClick={() =>
                    void runAction(() => companyRepository.remove(company.id), 'Failed to delete company.')
                  }
                  className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  Delete
                </button>
              </>
            )}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-6 p-6">
          <Field label="Timezone" value={company.timezone} />
          <Field label="Currency" value={company.currency} />
          <Field label="Registered" value={new Date(company.registeredAtUtc).toLocaleString()} />
          <Field label="Approved" value={company.approvedAtUtc ? new Date(company.approvedAtUtc).toLocaleString() : '—'} />
          <Field
            label="Suspended"
            value={
              company.suspendedAtUtc
                ? `${new Date(company.suspendedAtUtc).toLocaleString()}${company.suspendedReason ? ` — ${company.suspendedReason}` : ''}`
                : '—'
            }
          />
          <Field
            label="Rejected"
            value={
              company.rejectedAtUtc
                ? `${new Date(company.rejectedAtUtc).toLocaleString()}${company.rejectedReason ? ` — ${company.rejectedReason}` : ''}`
                : '—'
            }
          />
        </div>

        {(formError || statusMessage) && (
          <div className="space-y-1 px-6 pb-6">
            {formError && (
              <p role="alert" className="text-sm text-danger">
                {formError}
              </p>
            )}
            {statusMessage && <p className="text-sm text-success">{statusMessage}</p>}
          </div>
        )}
      </div>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="border-b border-hairline p-5">
          <h2 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">Admins</h2>
        </div>
        {company.admins.length === 0 ? (
          <p className="p-5 text-sm text-ink-subtle">No admin users found for this company.</p>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
              <tr>
                <th className="px-4 py-3">Username</th>
                <th className="px-4 py-3">Email</th>
                <th className="px-4 py-3">Active</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-hairline">
              {company.admins.map((admin) => (
                <tr key={admin.userId}>
                  <td className="px-4 py-3 font-medium text-ink">{admin.userName}</td>
                  <td className="px-4 py-3 text-ink-muted">{admin.email}</td>
                  <td className="px-4 py-3 text-ink-muted">{admin.isActive ? 'Yes' : 'No'}</td>
                  <td className="px-4 py-3 text-right">
                    <button
                      type="button"
                      disabled={isBusy}
                      onClick={() => void handleResetPassword(admin.userId)}
                      className="text-xs font-medium text-ink-muted hover:text-ink disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      Send password reset
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <Modal isOpen={isEditing} onClose={() => setIsEditing(false)} title="Edit company">
        <form onSubmit={handleSubmit(onSave)} noValidate className="space-y-3">
          <CompanyFormFields register={register} errors={errors} idPrefix="edit-company" />
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
            {isSubmitting ? 'Saving…' : 'Save changes'}
          </button>
        </form>
      </Modal>

      <ReasonPromptModal
        isOpen={activePrompt === 'suspend'}
        title="Suspend company"
        confirmLabel="Suspend"
        isBusy={isBusy}
        onClose={() => setActivePrompt(null)}
        onConfirm={(reason) => {
          setActivePrompt(null)
          void runAction(() => companyRepository.suspend(company.id, reason), 'Failed to suspend company.')
        }}
      />
      <ReasonPromptModal
        isOpen={activePrompt === 'reject'}
        title="Reject company"
        confirmLabel="Reject"
        isBusy={isBusy}
        onClose={() => setActivePrompt(null)}
        onConfirm={(reason) => {
          setActivePrompt(null)
          void runAction(() => companyRepository.reject(company.id, reason), 'Failed to reject company.')
        }}
      />
    </div>
  )
}
