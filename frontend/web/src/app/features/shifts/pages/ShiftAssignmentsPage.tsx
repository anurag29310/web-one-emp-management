import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useShifts } from '../hooks/useShifts'
import { useEmployeeShifts } from '../hooks/useEmployeeShifts'
import { shiftRepository, type EmployeeShift } from '../api'
import { shiftAssignmentFormSchema, type ShiftAssignmentFormValues } from '../types/shiftSchema'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'
import { Avatar } from '@/app/shared/components/Avatar'

const EMPTY_ASSIGNMENT_FORM: ShiftAssignmentFormValues = {
  shiftId: '',
  effectiveFrom: new Date().toISOString().slice(0, 10),
  effectiveTo: '',
}

function AssignmentForm({
  defaultValues,
  submitLabel,
  onSubmit,
  shifts,
}: {
  defaultValues: ShiftAssignmentFormValues
  submitLabel: string
  onSubmit: (values: ShiftAssignmentFormValues) => Promise<void>
  shifts: { id: string; name: string }[]
}) {
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ShiftAssignmentFormValues>({ resolver: zodResolver(shiftAssignmentFormSchema), defaultValues })

  async function submit(values: ShiftAssignmentFormValues) {
    setFormError(null)
    try {
      await onSubmit(values)
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to save shift assignment.')
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="space-y-3">
      <div>
        <label htmlFor="assign-shift" className="mb-1 block text-sm font-medium text-ink-muted">
          Shift
        </label>
        <select
          id="assign-shift"
          aria-invalid={Boolean(errors.shiftId)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('shiftId')}
        >
          <option value="">Select a shift…</option>
          {shifts.map((shift) => (
            <option key={shift.id} value={shift.id}>
              {shift.name}
            </option>
          ))}
        </select>
        {errors.shiftId && <p className="mt-1 text-xs text-danger">{errors.shiftId.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="assign-from" className="mb-1 block text-sm font-medium text-ink-muted">
            Effective from
          </label>
          <input
            id="assign-from"
            type="date"
            aria-invalid={Boolean(errors.effectiveFrom)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('effectiveFrom')}
          />
          {errors.effectiveFrom && <p className="mt-1 text-xs text-danger">{errors.effectiveFrom.message}</p>}
        </div>
        <div>
          <label htmlFor="assign-to" className="mb-1 block text-sm font-medium text-ink-muted">
            Effective to (optional)
          </label>
          <input
            id="assign-to"
            type="date"
            aria-invalid={Boolean(errors.effectiveTo)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('effectiveTo')}
          />
          {errors.effectiveTo && <p className="mt-1 text-xs text-danger">{errors.effectiveTo.message}</p>}
        </div>
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
        {isSubmitting ? 'Saving…' : submitLabel}
      </button>
    </form>
  )
}

export function ShiftAssignmentsPage() {
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'

  const [search, setSearch] = useState('')
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string | null>(null)
  const { result: employeeResult } = useEmployees({ search: search || undefined, pageSize: 20 })
  const { shifts } = useShifts()
  const { assignments, isLoading, error, refresh } = useEmployeeShifts(selectedEmployeeId ?? undefined)

  const [isAssignOpen, setIsAssignOpen] = useState(false)
  const [editingAssignment, setEditingAssignment] = useState<EmployeeShift | null>(null)
  const [pendingEndId, setPendingEndId] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const shiftsById = useMemo(() => new Map(shifts.map((s) => [s.id, s])), [shifts])
  const selectedEmployee = employeeResult?.data.find((e) => e.id === selectedEmployeeId)

  const sortedAssignments = useMemo(
    () => [...assignments].sort((a, b) => b.effectiveFrom.localeCompare(a.effectiveFrom)),
    [assignments],
  )

  async function handleAssign(values: ShiftAssignmentFormValues) {
    if (!selectedEmployeeId) return
    await shiftRepository.assignEmployeeShift({
      employeeId: selectedEmployeeId,
      shiftId: values.shiftId,
      effectiveFrom: `${values.effectiveFrom}T00:00:00.000Z`,
      effectiveTo: values.effectiveTo ? `${values.effectiveTo}T00:00:00.000Z` : undefined,
    })
    setIsAssignOpen(false)
    refresh()
  }

  async function handleEditAssignment(values: ShiftAssignmentFormValues) {
    if (!selectedEmployeeId || !editingAssignment) return
    await shiftRepository.updateEmployeeShift({
      employeeId: selectedEmployeeId,
      assignmentId: editingAssignment.id,
      shiftId: values.shiftId,
      effectiveFrom: `${values.effectiveFrom}T00:00:00.000Z`,
      effectiveTo: values.effectiveTo ? `${values.effectiveTo}T00:00:00.000Z` : undefined,
    })
    setEditingAssignment(null)
    refresh()
  }

  async function handleEnd(assignment: EmployeeShift) {
    if (!selectedEmployeeId) return
    setPendingEndId(assignment.id)
    setActionError(null)
    try {
      await shiftRepository.endEmployeeShift(selectedEmployeeId, assignment.id)
      refresh()
    } catch (err) {
      setActionError(err instanceof AppError ? err.message : 'Failed to end shift assignment.')
    } finally {
      setPendingEndId(null)
    }
  }

  if (!canManage) {
    return (
      <div className="space-y-4">
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Shift assignments</h1>
        <p className="text-sm text-ink-subtle">
          You don't have permission to assign shifts. Contact an Admin or HR user.
        </p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Shift assignments</h1>
          <p className="text-sm text-ink-subtle">Assign employees to shifts, or edit an existing assignment.</p>
        </div>
        <Link to="/shifts" className="text-sm text-ink-subtle hover:text-primary-hover">
          ← Back to shifts
        </Link>
      </div>

      <div className="grid grid-cols-3 gap-4">
        <div className="col-span-1 space-y-2 rounded-lg border border-hairline bg-surface-1 p-4">
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search employees…"
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          />
          <ul className="max-h-96 space-y-1 overflow-y-auto">
            {employeeResult?.data.map((employee) => (
              <li key={employee.id}>
                <button
                  type="button"
                  onClick={() => setSelectedEmployeeId(employee.id)}
                  className={`flex w-full items-center gap-2 rounded-md px-2 py-2 text-left text-sm transition ${
                    selectedEmployeeId === employee.id ? 'bg-surface-2 text-ink' : 'text-ink-subtle hover:bg-surface-2 hover:text-ink'
                  }`}
                >
                  <Avatar name={employee.fullName} size="sm" />
                  <span className="min-w-0 flex-1 truncate">{employee.fullName}</span>
                </button>
              </li>
            ))}
            {employeeResult && employeeResult.data.length === 0 && (
              <li className="px-2 py-4 text-center text-sm text-ink-subtle">No employees match your search.</li>
            )}
          </ul>
        </div>

        <div className="col-span-2 space-y-4">
          {!selectedEmployeeId && (
            <div className="rounded-lg border border-hairline bg-surface-1 p-8 text-center text-sm text-ink-subtle">
              Select an employee to view or assign their shift.
            </div>
          )}

          {selectedEmployeeId && (
            <div className="rounded-lg border border-hairline bg-surface-1">
              <div className="flex items-center justify-between border-b border-hairline p-4">
                <div className="flex items-center gap-3">
                  <Avatar name={selectedEmployee?.fullName ?? '?'} />
                  <div>
                    <p className="text-sm font-medium text-ink">{selectedEmployee?.fullName ?? 'Employee'}</p>
                    <p className="text-xs text-ink-subtle">{selectedEmployee?.employeeCode}</p>
                  </div>
                </div>
                <button
                  type="button"
                  onClick={() => setIsAssignOpen(true)}
                  className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
                >
                  Assign shift
                </button>
              </div>

              {(error || actionError) && (
                <p role="alert" className="px-4 pt-3 text-sm text-danger">
                  {error ?? actionError}
                </p>
              )}

              <div className="divide-y divide-hairline">
                {isLoading && <div className="p-4 text-sm text-ink-subtle">Loading assignments…</div>}
                {!isLoading && sortedAssignments.length === 0 && (
                  <div className="p-4 text-sm text-ink-subtle">No shift assignments yet.</div>
                )}
                {!isLoading &&
                  sortedAssignments.map((assignment) => {
                    const shift = shiftsById.get(assignment.shiftId)
                    const isActive = !assignment.effectiveTo || new Date(assignment.effectiveTo) >= new Date()
                    return (
                      <div key={assignment.id} className="flex items-center justify-between p-4">
                        <div>
                          <p className="text-sm font-medium text-ink">{shift?.name ?? 'Unknown shift'}</p>
                          <p className="text-xs text-ink-subtle">
                            {new Date(assignment.effectiveFrom).toLocaleDateString()} –{' '}
                            {assignment.effectiveTo ? new Date(assignment.effectiveTo).toLocaleDateString() : 'ongoing'}
                          </p>
                        </div>
                        <div className="flex items-center gap-3">
                          {isActive && (
                            <span className="inline-flex items-center rounded-full bg-success/15 px-2.5 py-0.5 text-xs font-medium text-success ring-1 ring-inset ring-success/30">
                              Active
                            </span>
                          )}
                          <button
                            type="button"
                            onClick={() => setEditingAssignment(assignment)}
                            className="text-xs font-medium text-ink-muted hover:text-ink"
                          >
                            Edit
                          </button>
                          <button
                            type="button"
                            disabled={pendingEndId === assignment.id}
                            onClick={() => void handleEnd(assignment)}
                            className="text-xs font-medium text-danger hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                          >
                            {pendingEndId === assignment.id ? 'Ending…' : 'End'}
                          </button>
                        </div>
                      </div>
                    )
                  })}
              </div>
            </div>
          )}
        </div>
      </div>

      <Modal isOpen={isAssignOpen} onClose={() => setIsAssignOpen(false)} title="Assign shift">
        <AssignmentForm defaultValues={EMPTY_ASSIGNMENT_FORM} submitLabel="Assign shift" onSubmit={handleAssign} shifts={shifts} />
      </Modal>

      <Modal isOpen={editingAssignment !== null} onClose={() => setEditingAssignment(null)} title="Edit shift assignment">
        {editingAssignment && (
          <AssignmentForm
            defaultValues={{
              shiftId: editingAssignment.shiftId,
              effectiveFrom: editingAssignment.effectiveFrom.slice(0, 10),
              effectiveTo: editingAssignment.effectiveTo?.slice(0, 10) ?? '',
            }}
            submitLabel="Save changes"
            onSubmit={handleEditAssignment}
            shifts={shifts}
          />
        )}
      </Modal>
    </div>
  )
}
