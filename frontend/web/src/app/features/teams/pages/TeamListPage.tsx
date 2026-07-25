import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTeams } from '../hooks/useTeams'
import { teamRepository, type Team } from '../api'
import { teamFormSchema, type TeamFormValues } from '../types/teamSchema'
import { useDepartments } from '@/app/features/departments/hooks/useDepartments'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

export function TeamListPage() {
  const { teams, isLoading, error, refresh } = useTeams()
  const { departments } = useDepartments()
  const { result: employeeResult } = useEmployees({ pageSize: 100 })
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'

  const [isFormOpen, setIsFormOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<TeamFormValues>({
    resolver: zodResolver(teamFormSchema),
    defaultValues: { departmentId: '', name: '', code: '', leadEmployeeId: '' },
  })

  async function onCreate(values: TeamFormValues) {
    setFormError(null)
    try {
      await teamRepository.create({
        departmentId: values.departmentId,
        name: values.name.trim(),
        code: values.code.trim(),
        leadEmployeeId: values.leadEmployeeId || undefined,
      })
      reset()
      setIsFormOpen(false)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to create team.')
    }
  }

  async function handleDelete(team: Team) {
    setPendingDeleteId(team.id)
    try {
      await teamRepository.remove(team.id)
      refresh()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to delete team.')
    } finally {
      setPendingDeleteId(null)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Teams</h1>
          <p className="text-sm text-ink-subtle">{teams.length} teams</p>
        </div>
        {canManage && (
          <button
            type="button"
            onClick={() => setIsFormOpen((open) => !open)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            {isFormOpen ? 'Cancel' : 'New team'}
          </button>
        )}
      </div>

      {canManage && (
        <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="New team">
          <form onSubmit={handleSubmit(onCreate)} noValidate className="space-y-3">
            <div>
              <label htmlFor="team-department" className="mb-1 block text-sm font-medium text-ink-muted">
                Department
              </label>
              <select
                id="team-department"
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
                <label htmlFor="team-name" className="mb-1 block text-sm font-medium text-ink-muted">
                  Name
                </label>
                <input
                  id="team-name"
                  aria-invalid={Boolean(errors.name)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...register('name')}
                />
                {errors.name && <p className="mt-1 text-xs text-danger">{errors.name.message}</p>}
              </div>
              <div>
                <label htmlFor="team-code" className="mb-1 block text-sm font-medium text-ink-muted">
                  Code
                </label>
                <input
                  id="team-code"
                  aria-invalid={Boolean(errors.code)}
                  className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
                  {...register('code')}
                />
                {errors.code && <p className="mt-1 text-xs text-danger">{errors.code.message}</p>}
              </div>
            </div>
            <div>
              <label htmlFor="team-lead" className="mb-1 block text-sm font-medium text-ink-muted">
                Team lead
              </label>
              <select
                id="team-lead"
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
              {isSubmitting ? 'Saving…' : 'Create team'}
            </button>
          </form>
        </Modal>
      )}

      {error && (
        <p role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}
      {!isFormOpen && formError && (
        <p role="alert" className="text-sm text-danger">
          {formError}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Name</th>
              <th className="px-4 py-3">Code</th>
              <th className="px-4 py-3">Department</th>
              <th className="px-4 py-3">Lead</th>
              {canManage && <th className="px-4 py-3" />}
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={canManage ? 5 : 4}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && teams.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={canManage ? 5 : 4}>
                  No teams yet.
                </td>
              </tr>
            )}

            {!isLoading &&
              teams.map((team) => {
                const lead = employeeResult?.data.find((e) => e.id === team.leadEmployeeId)
                return (
                  <tr key={team.id} className="transition hover:bg-surface-2">
                    <td className="px-4 py-3">
                      <Link to={`/teams/${team.id}`} className="font-medium text-ink hover:text-primary-hover">
                        {team.name}
                      </Link>
                    </td>
                    <td className="px-4 py-3 font-mono text-ink-subtle">{team.code}</td>
                    <td className="px-4 py-3 text-ink-muted">{team.departmentName ?? '—'}</td>
                    <td className="px-4 py-3 text-ink-muted">{lead?.fullName ?? '—'}</td>
                    {canManage && (
                      <td className="px-4 py-3 text-right">
                        <button
                          type="button"
                          disabled={pendingDeleteId === team.id}
                          onClick={() => void handleDelete(team)}
                          className="text-xs font-medium text-danger hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                        >
                          {pendingDeleteId === team.id ? 'Deleting…' : 'Delete'}
                        </button>
                      </td>
                    )}
                  </tr>
                )
              })}
          </tbody>
        </table>
      </div>
    </div>
  )
}
