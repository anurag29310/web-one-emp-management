import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useAttendanceRecords } from '../hooks/useAttendanceRecords'
import { attendanceRepository, type AttendanceRecord, type AttendanceStatus } from '../api'
import {
  attendanceStatusOptions,
  createAttendanceRecordSchema,
  updateAttendanceRecordSchema,
  type CreateAttendanceRecordFormValues,
  type UpdateAttendanceRecordFormValues,
} from '../types/attendanceSchemas'
import { Pagination } from '../components/Pagination'
import { useShifts } from '@/app/features/shifts/hooks/useShifts'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { StatusBadge } from '@/app/shared/components/StatusBadge'
import { Modal } from '@/app/shared/components/Modal'

function toDateTimeLocal(iso: string | null): string {
  if (!iso) return ''
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

function toIso(dateTimeLocal: string): string | undefined {
  return dateTimeLocal ? new Date(dateTimeLocal).toISOString() : undefined
}

function CreateRecordForm({
  employees,
  shifts,
  onClose,
  onCreated,
}: {
  employees: { id: string; fullName: string }[]
  shifts: { id: string; name: string }[]
  onClose: () => void
  onCreated: () => void
}) {
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<CreateAttendanceRecordFormValues>({
    resolver: zodResolver(createAttendanceRecordSchema),
    defaultValues: {
      employeeId: '',
      shiftId: '',
      attendanceDate: new Date().toISOString().slice(0, 10),
      checkInAt: '',
      checkOutAt: '',
      status: 'Present',
      notes: '',
    },
  })

  async function submit(values: CreateAttendanceRecordFormValues) {
    setFormError(null)
    try {
      await attendanceRepository.create({
        employeeId: values.employeeId,
        shiftId: values.shiftId || undefined,
        attendanceDate: `${values.attendanceDate}T00:00:00.000Z`,
        checkInAtUtc: toIso(values.checkInAt ?? ''),
        checkOutAtUtc: toIso(values.checkOutAt ?? ''),
        status: values.status,
        notes: values.notes?.trim() || undefined,
      })
      onCreated()
      onClose()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to create attendance record.')
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="space-y-3">
      <div>
        <label htmlFor="rec-employee" className="mb-1 block text-sm font-medium text-ink-muted">
          Employee
        </label>
        <select
          id="rec-employee"
          aria-invalid={Boolean(errors.employeeId)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('employeeId')}
        >
          <option value="">Select an employee…</option>
          {employees.map((e) => (
            <option key={e.id} value={e.id}>
              {e.fullName}
            </option>
          ))}
        </select>
        {errors.employeeId && <p className="mt-1 text-xs text-danger">{errors.employeeId.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="rec-date" className="mb-1 block text-sm font-medium text-ink-muted">
            Date
          </label>
          <input
            id="rec-date"
            type="date"
            aria-invalid={Boolean(errors.attendanceDate)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('attendanceDate')}
          />
          {errors.attendanceDate && <p className="mt-1 text-xs text-danger">{errors.attendanceDate.message}</p>}
        </div>
        <div>
          <label htmlFor="rec-shift" className="mb-1 block text-sm font-medium text-ink-muted">
            Shift (optional)
          </label>
          <select
            id="rec-shift"
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('shiftId')}
          >
            <option value="">No shift</option>
            {shifts.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="rec-checkin" className="mb-1 block text-sm font-medium text-ink-muted">
            Check-in (optional)
          </label>
          <input
            id="rec-checkin"
            type="datetime-local"
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('checkInAt')}
          />
        </div>
        <div>
          <label htmlFor="rec-checkout" className="mb-1 block text-sm font-medium text-ink-muted">
            Check-out (optional)
          </label>
          <input
            id="rec-checkout"
            type="datetime-local"
            aria-invalid={Boolean(errors.checkOutAt)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('checkOutAt')}
          />
          {errors.checkOutAt && <p className="mt-1 text-xs text-danger">{errors.checkOutAt.message}</p>}
        </div>
      </div>

      <div>
        <label htmlFor="rec-status" className="mb-1 block text-sm font-medium text-ink-muted">
          Status
        </label>
        <select
          id="rec-status"
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('status')}
        >
          {attendanceStatusOptions.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
      </div>

      <div>
        <label htmlFor="rec-notes" className="mb-1 block text-sm font-medium text-ink-muted">
          Notes
        </label>
        <textarea
          id="rec-notes"
          rows={2}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('notes')}
        />
        {errors.notes && <p className="mt-1 text-xs text-danger">{errors.notes.message}</p>}
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
        {isSubmitting ? 'Saving…' : 'Create record'}
      </button>
    </form>
  )
}

function EditRecordForm({
  record,
  shifts,
  onClose,
  onSaved,
}: {
  record: AttendanceRecord
  shifts: { id: string; name: string }[]
  onClose: () => void
  onSaved: () => void
}) {
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<UpdateAttendanceRecordFormValues>({
    resolver: zodResolver(updateAttendanceRecordSchema),
    defaultValues: {
      shiftId: record.shiftId ?? '',
      checkInAt: toDateTimeLocal(record.checkInAtUtc),
      checkOutAt: toDateTimeLocal(record.checkOutAtUtc),
      status: record.status,
      notes: record.notes ?? '',
    },
  })

  async function submit(values: UpdateAttendanceRecordFormValues) {
    setFormError(null)
    try {
      await attendanceRepository.update({
        id: record.id,
        shiftId: values.shiftId || undefined,
        checkInAtUtc: toIso(values.checkInAt ?? ''),
        checkOutAtUtc: toIso(values.checkOutAt ?? ''),
        status: values.status,
        notes: values.notes?.trim() || undefined,
      })
      onSaved()
      onClose()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to update attendance record.')
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="space-y-3">
      <p className="text-sm text-ink-subtle">{new Date(record.attendanceDate).toLocaleDateString()}</p>

      <div>
        <label htmlFor="edit-rec-shift" className="mb-1 block text-sm font-medium text-ink-muted">
          Shift
        </label>
        <select
          id="edit-rec-shift"
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('shiftId')}
        >
          <option value="">No shift</option>
          {shifts.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name}
            </option>
          ))}
        </select>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="edit-rec-checkin" className="mb-1 block text-sm font-medium text-ink-muted">
            Check-in
          </label>
          <input
            id="edit-rec-checkin"
            type="datetime-local"
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('checkInAt')}
          />
        </div>
        <div>
          <label htmlFor="edit-rec-checkout" className="mb-1 block text-sm font-medium text-ink-muted">
            Check-out
          </label>
          <input
            id="edit-rec-checkout"
            type="datetime-local"
            aria-invalid={Boolean(errors.checkOutAt)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('checkOutAt')}
          />
          {errors.checkOutAt && <p className="mt-1 text-xs text-danger">{errors.checkOutAt.message}</p>}
        </div>
      </div>

      <div>
        <label htmlFor="edit-rec-status" className="mb-1 block text-sm font-medium text-ink-muted">
          Status
        </label>
        <select
          id="edit-rec-status"
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('status')}
        >
          {attendanceStatusOptions.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
      </div>

      <div>
        <label htmlFor="edit-rec-notes" className="mb-1 block text-sm font-medium text-ink-muted">
          Notes
        </label>
        <textarea
          id="edit-rec-notes"
          rows={2}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('notes')}
        />
        {errors.notes && <p className="mt-1 text-xs text-danger">{errors.notes.message}</p>}
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
  )
}

export function AttendanceRecordsPage() {
  const { user } = useAuth()
  const canManage = user?.role === 'Admin' || user?.role === 'HR'

  const [page, setPage] = useState(1)
  const [employeeId, setEmployeeId] = useState('')
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')
  const [status, setStatus] = useState<AttendanceStatus | ''>('')

  const { result: employeeResult } = useEmployees({ pageSize: 100 })
  const { shifts } = useShifts()

  const { result, isLoading, error, refresh } = useAttendanceRecords({
    page,
    pageSize: 20,
    employeeId: employeeId || undefined,
    dateFrom: dateFrom ? `${dateFrom}T00:00:00.000Z` : undefined,
    dateTo: dateTo ? `${dateTo}T23:59:59.999Z` : undefined,
    status: status || undefined,
  })

  const employeesById = useMemo(() => new Map((employeeResult?.data ?? []).map((e) => [e.id, e])), [employeeResult])

  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [editingRecord, setEditingRecord] = useState<AttendanceRecord | null>(null)
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)
  const [rowError, setRowError] = useState<string | null>(null)

  async function handleDelete(record: AttendanceRecord) {
    setPendingDeleteId(record.id)
    setRowError(null)
    try {
      await attendanceRepository.remove(record.id)
      refresh()
    } catch (err) {
      setRowError(err instanceof AppError ? err.message : 'Failed to delete attendance record.')
    } finally {
      setPendingDeleteId(null)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Attendance Records</h1>
          <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
        </div>
        {canManage && (
          <button
            type="button"
            onClick={() => setIsCreateOpen(true)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            New record
          </button>
        )}
      </div>

      <div className="flex flex-wrap gap-3 rounded-lg border border-hairline bg-surface-1 p-4">
        <select
          value={employeeId}
          onChange={(e) => {
            setEmployeeId(e.target.value)
            setPage(1)
          }}
          className="rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        >
          <option value="">All employees</option>
          {employeeResult?.data.map((e) => (
            <option key={e.id} value={e.id}>
              {e.fullName}
            </option>
          ))}
        </select>
        <input
          type="date"
          value={dateFrom}
          onChange={(e) => {
            setDateFrom(e.target.value)
            setPage(1)
          }}
          className="rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        />
        <input
          type="date"
          value={dateTo}
          onChange={(e) => {
            setDateTo(e.target.value)
            setPage(1)
          }}
          className="rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        />
        <select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as AttendanceStatus | '')
            setPage(1)
          }}
          className="rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        >
          <option value="">All statuses</option>
          {attendanceStatusOptions.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
      </div>

      {(error || rowError) && (
        <p role="alert" className="text-sm text-danger">
          {error ?? rowError}
        </p>
      )}

      {canManage && (
        <Modal isOpen={isCreateOpen} onClose={() => setIsCreateOpen(false)} title="New attendance record">
          <CreateRecordForm
            employees={employeeResult?.data ?? []}
            shifts={shifts}
            onClose={() => setIsCreateOpen(false)}
            onCreated={refresh}
          />
        </Modal>
      )}

      {canManage && (
        <Modal isOpen={editingRecord !== null} onClose={() => setEditingRecord(null)} title="Edit attendance record">
          {editingRecord && (
            <EditRecordForm record={editingRecord} shifts={shifts} onClose={() => setEditingRecord(null)} onSaved={refresh} />
          )}
        </Modal>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Employee</th>
              <th className="px-4 py-3">Date</th>
              <th className="px-4 py-3">Check-in</th>
              <th className="px-4 py-3">Check-out</th>
              <th className="px-4 py-3">Status</th>
              {canManage && <th className="px-4 py-3" />}
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 4 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={canManage ? 6 : 5}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && result?.data.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={canManage ? 6 : 5}>
                  No attendance records match your filters.
                </td>
              </tr>
            )}

            {!isLoading &&
              result?.data.map((record) => (
                <tr key={record.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3 font-medium text-ink">
                    {employeesById.get(record.employeeId)?.fullName ?? record.employeeId}
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{new Date(record.attendanceDate).toLocaleDateString()}</td>
                  <td className="px-4 py-3 text-ink-muted">
                    {record.checkInAtUtc ? new Date(record.checkInAtUtc).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' }) : '—'}
                  </td>
                  <td className="px-4 py-3 text-ink-muted">
                    {record.checkOutAtUtc ? new Date(record.checkOutAtUtc).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' }) : '—'}
                  </td>
                  <td className="px-4 py-3">
                    <StatusBadge status={record.status} />
                  </td>
                  {canManage && (
                    <td className="px-4 py-3 text-right">
                      <div className="flex justify-end gap-3">
                        <button
                          type="button"
                          onClick={() => setEditingRecord(record)}
                          className="text-xs font-medium text-ink-muted hover:text-ink"
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          disabled={pendingDeleteId === record.id}
                          onClick={() => void handleDelete(record)}
                          className="text-xs font-medium text-danger hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                        >
                          {pendingDeleteId === record.id ? 'Deleting…' : 'Delete'}
                        </button>
                      </div>
                    </td>
                  )}
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      {result && <Pagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} onPageChange={setPage} />}
    </div>
  )
}
