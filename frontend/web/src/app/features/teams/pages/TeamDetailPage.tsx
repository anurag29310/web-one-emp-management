import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTeam } from '../hooks/useTeam'
import { useTeamEmployees } from '../hooks/useTeamEmployees'
import { teamRepository } from '../api'
import { teamFormSchema, type TeamFormValues } from '../types/teamSchema'
import { useDepartments } from '@/app/features/departments/hooks/useDepartments'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">{label}</p>
      <p className="mt-0.5 text-sm text-ink">{value}</p>
    </div>
  )
}

export function TeamDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { team, isLoading, error, refresh } = useTeam(id)
  const { employees: members, isLoading: isLoadingMembers } = useTeamEmployees(id)
  const { departments } = useDepartments()
  const { result: employeeResult } = useEmployees({ pageSize: 100 })
  const { user } = useAuth()
  const navigate = useNavigate()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'

  const [isEditing, setIsEditing] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<TeamFormValues>({ resolver: zodResolver(teamFormSchema) })

  useEffect(() => {
    if (team) {
      reset({
        departmentId: team.departmentId,
        name: team.name,
        code: team.code,
        leadEmployeeId: team.leadEmployeeId ?? '',
      })
    }
  }, [team, reset])

  async function onSave(values: TeamFormValues) {
    if (!team) return
    setFormError(null)
    try {
      await teamRepository.update({
        id: team.id,
        departmentId: values.departmentId,
        name: values.name.trim(),
        code: values.code.trim(),
        leadEmployeeId: values.leadEmployeeId || undefined,
      })
      setIsEditing(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update team.')
    }
  }

  async function handleDelete() {
    if (!team) return
    setIsDeleting(true)
    try {
      await teamRepository.remove(team.id)
      navigate('/teams')
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete team.')
      setIsDeleting(false)
    }
  }

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-lg border border-hairline bg-surface-1" />
  }
  if (error) return <p className="text-sm text-danger">{error}</p>
  if (!team) return null

  const lead = employeeResult?.data.find((e) => e.id === team.leadEmployeeId)

  return (
    <div className="space-y-4">
      <Link to="/teams" className="inline-flex items-center gap-1 text-sm text-ink-subtle hover:text-primary-hover">
        ← Back to teams
      </Link>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="flex items-center justify-between border-b border-hairline p-6">
          <div>
            <h1 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">{team.name}</h1>
            <p className="text-sm text-ink-subtle">{team.code}</p>
          </div>
          {canManage && (
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => setIsEditing((editing) => !editing)}
                className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
              >
                {isEditing ? 'Cancel' : 'Edit'}
              </button>
              <button
                type="button"
                disabled={isDeleting}
                onClick={() => void handleDelete()}
                className="rounded-md px-3 py-2 text-sm font-medium text-danger transition hover:bg-danger/10 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isDeleting ? 'Deleting…' : 'Delete'}
              </button>
            </div>
          )}
        </div>

        <div className="grid grid-cols-2 gap-6 p-6">
          <Field label="Department" value={team.departmentName ?? '—'} />
          <Field label="Team lead" value={lead?.fullName ?? '—'} />
          <Field label="Created" value={new Date(team.createdAtUtc).toLocaleDateString()} />
          <Field
            label="Last updated"
            value={team.updatedAtUtc ? new Date(team.updatedAtUtc).toLocaleDateString() : '—'}
          />
        </div>
        {!isEditing && formError && (
          <p role="alert" className="px-6 pb-6 text-sm text-danger">
            {formError}
          </p>
        )}
      </div>

      <div className="rounded-lg border border-hairline bg-surface-1">
        <div className="border-b border-hairline p-6">
          <h2 className="text-[22px] font-medium leading-[1.25] tracking-[-0.4px] text-ink">Team members</h2>
          <p className="text-sm text-ink-subtle">{members.length} members</p>
        </div>
        <div className="divide-y divide-hairline">
          {isLoadingMembers && <div className="h-16 animate-pulse bg-surface-2" />}
          {!isLoadingMembers && members.length === 0 && (
            <p className="px-6 py-8 text-center text-sm text-ink-subtle">No members in this team yet.</p>
          )}
          {!isLoadingMembers &&
            members.map((member) => (
              <Link
                key={member.id}
                to={`/employees/${member.id}`}
                className="flex items-center justify-between px-6 py-3 transition hover:bg-surface-2"
              >
                <div>
                  <p className="text-sm font-medium text-ink">{member.fullName}</p>
                  <p className="text-xs text-ink-subtle">{member.designationName ?? 'No designation'}</p>
                </div>
                <span className="text-xs text-ink-subtle">{member.employeeCode}</span>
              </Link>
            ))}
        </div>
      </div>

      {canManage && (
        <Modal isOpen={isEditing} onClose={() => setIsEditing(false)} title="Edit team">
          <form onSubmit={handleSubmit(onSave)} noValidate className="space-y-3">
            <div>
              <label htmlFor="edit-team-department" className="mb-1 block text-sm font-medium text-ink-muted">
                Department
              </label>
              <select
                id="edit-team-department"
                aria-invalid={Boolean(errors.departmentId)}
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...register('departmentId')}
              >
                <option value="">Select a department</option>
                {departments.map((department) => (
                  <option key={department.id} value={department.id}>
                    {department.name}
                  </option>
                ))}
              </select>
              {errors.departmentId && (
                <p className="mt-1 text-xs text-danger">{errors.departmentId.message}</p>
              )}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="edit-team-name" className="mb-1 block text-sm font-medium text-ink-muted">
                  Name
                </label>
                <input
                  id="edit-team-name"
                  aria-invalid={Boolean(errors.name)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...register('name')}
                />
                {errors.name && <p className="mt-1 text-xs text-danger">{errors.name.message}</p>}
              </div>
              <div>
                <label htmlFor="edit-team-code" className="mb-1 block text-sm font-medium text-ink-muted">
                  Code
                </label>
                <input
                  id="edit-team-code"
                  aria-invalid={Boolean(errors.code)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...register('code')}
                />
                {errors.code && <p className="mt-1 text-xs text-danger">{errors.code.message}</p>}
              </div>
            </div>
            <div>
              <label htmlFor="edit-team-lead" className="mb-1 block text-sm font-medium text-ink-muted">
                Team lead
              </label>
              <select
                id="edit-team-lead"
                className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                {...register('leadEmployeeId')}
              >
                <option value="">No lead assigned</option>
                {employeeResult?.data.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.fullName}
                  </option>
                ))}
              </select>
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
              {isSubmitting ? 'Saving…' : 'Save changes'}
            </button>
          </form>
        </Modal>
      )}
    </div>
  )
}
