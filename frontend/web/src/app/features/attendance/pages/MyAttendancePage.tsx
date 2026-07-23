import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useAttendanceRecords } from '../hooks/useAttendanceRecords'
import { useOwnEmployeeId } from '../hooks/useOwnEmployeeId'
import { attendanceRepository, type AttendanceRecord } from '../api'
import { correctionRequestSchema, type CorrectionRequestFormValues } from '../types/attendanceSchemas'
import { startOfDayIso, endOfDayIso, toDateOnly } from '../utils/date'
import { AttendanceCalendar } from '../components/AttendanceCalendar'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { StatusBadge } from '@/app/shared/components/StatusBadge'
import { Avatar } from '@/app/shared/components/Avatar'
import { Modal } from '@/app/shared/components/Modal'

function formatTime(iso: string | null): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
}

function CorrectionForm({ record, onClose, onSubmitted }: { record: AttendanceRecord; onClose: () => void; onSubmitted: () => void }) {
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<CorrectionRequestFormValues>({
    resolver: zodResolver(correctionRequestSchema),
    defaultValues: { requestedCheckInAt: '', requestedCheckOutAt: '', reason: '' },
  })

  async function submit(values: CorrectionRequestFormValues) {
    setFormError(null)
    try {
      await attendanceRepository.requestCorrection({
        attendanceRecordId: record.id,
        requestedCheckInAtUtc: values.requestedCheckInAt ? new Date(values.requestedCheckInAt).toISOString() : undefined,
        requestedCheckOutAtUtc: values.requestedCheckOutAt ? new Date(values.requestedCheckOutAt).toISOString() : undefined,
        reason: values.reason.trim(),
      })
      onSubmitted()
      onClose()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to submit correction request.')
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="space-y-3">
      <p className="text-sm text-ink-subtle">
        Requesting a correction for {new Date(record.attendanceDate).toLocaleDateString()}.
      </p>
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="corr-checkin" className="mb-1 block text-sm font-medium text-ink-muted">
            Corrected check-in
          </label>
          <input
            id="corr-checkin"
            type="datetime-local"
            aria-invalid={Boolean(errors.requestedCheckInAt)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('requestedCheckInAt')}
          />
        </div>
        <div>
          <label htmlFor="corr-checkout" className="mb-1 block text-sm font-medium text-ink-muted">
            Corrected check-out
          </label>
          <input
            id="corr-checkout"
            type="datetime-local"
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
            {...register('requestedCheckOutAt')}
          />
        </div>
      </div>
      {errors.requestedCheckInAt && <p className="text-xs text-danger">{errors.requestedCheckInAt.message}</p>}
      <div>
        <label htmlFor="corr-reason" className="mb-1 block text-sm font-medium text-ink-muted">
          Reason
        </label>
        <textarea
          id="corr-reason"
          rows={3}
          aria-invalid={Boolean(errors.reason)}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          {...register('reason')}
        />
        {errors.reason && <p className="mt-1 text-xs text-danger">{errors.reason.message}</p>}
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
        {isSubmitting ? 'Submitting…' : 'Submit correction request'}
      </button>
    </form>
  )
}

export function MyAttendancePage() {
  const { user } = useAuth()
  const isPrivileged = user?.role === 'Admin' || user?.role === 'HR'
  const { resolve: resolveOwnEmployeeId } = useOwnEmployeeId()

  const [employeeSearch, setEmployeeSearch] = useState('')
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string | null>(null)
  const { result: employeeResult } = useEmployees({ search: employeeSearch || undefined, pageSize: 10 })

  const [monthDate, setMonthDate] = useState(() => {
    const now = new Date()
    return new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 1))
  })
  const [selectedDate, setSelectedDate] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [isActing, setIsActing] = useState(false)
  const [needsManualId, setNeedsManualId] = useState(false)
  const [manualEmployeeId, setManualEmployeeId] = useState('')
  const [correctionRecord, setCorrectionRecord] = useState<AttendanceRecord | null>(null)

  const activeEmployeeId = isPrivileged ? selectedEmployeeId : null

  const monthStart = new Date(Date.UTC(monthDate.getUTCFullYear(), monthDate.getUTCMonth(), 1))
  const monthEnd = new Date(Date.UTC(monthDate.getUTCFullYear(), monthDate.getUTCMonth() + 1, 0))

  const {
    result: monthResult,
    isLoading: isMonthLoading,
    error: monthError,
    refresh: refreshMonth,
  } = useAttendanceRecords({
    employeeId: activeEmployeeId ?? undefined,
    dateFrom: startOfDayIso(monthStart),
    dateTo: endOfDayIso(monthEnd),
    pageSize: 100,
  })

  // Skip fetching "my" personal data for a privileged user until they've picked who they're acting as.
  const canQuery = !isPrivileged || Boolean(selectedEmployeeId)
  const records = useMemo(() => (canQuery ? monthResult?.data ?? [] : []), [canQuery, monthResult])

  const today = new Date()
  const todayKey = useMemo(() => toDateOnly(new Date()), [])
  const todayRecord = useMemo(() => records.find((r) => r.attendanceDate.slice(0, 10) === todayKey) ?? null, [records, todayKey])
  const selectedRecord = useMemo(
    () => (selectedDate ? records.find((r) => r.attendanceDate.slice(0, 10) === selectedDate) ?? null : null),
    [records, selectedDate],
  )

  const selectedEmployee = employeeResult?.data.find((e) => e.id === selectedEmployeeId)

  async function handleCheckIn() {
    setActionError(null)
    setIsActing(true)
    try {
      let employeeId = activeEmployeeId
      if (!employeeId) {
        employeeId = manualEmployeeId.trim() || (await resolveOwnEmployeeId())
      }
      if (!employeeId) {
        setNeedsManualId(true)
        setActionError('We could not automatically detect your employee record. Enter your Employee ID below to continue.')
        return
      }
      await attendanceRepository.checkIn({ employeeId, checkInAtUtc: new Date().toISOString() })
      refreshMonth()
    } catch (err) {
      setActionError(err instanceof AppError ? err.message : 'Failed to check in.')
    } finally {
      setIsActing(false)
    }
  }

  async function handleCheckOut() {
    if (!todayRecord) return
    setActionError(null)
    setIsActing(true)
    try {
      await attendanceRepository.checkOut({ employeeId: todayRecord.employeeId, checkOutAtUtc: new Date().toISOString() })
      refreshMonth()
    } catch (err) {
      setActionError(err instanceof AppError ? err.message : 'Failed to check out.')
    } finally {
      setIsActing(false)
    }
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">My Attendance</h1>
        <p className="text-sm text-ink-subtle">Check in, check out, and review your attendance history.</p>
      </div>

      {isPrivileged && (
        <div className="rounded-lg border border-hairline bg-surface-1 p-4">
          <label htmlFor="attendance-employee-search" className="mb-1 block text-sm font-medium text-ink-muted">
            Record attendance for
          </label>
          <input
            id="attendance-employee-search"
            type="search"
            value={employeeSearch}
            onChange={(e) => setEmployeeSearch(e.target.value)}
            placeholder="Search employee by name or code…"
            className="w-full max-w-sm rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          />
          {employeeSearch && (
            <ul className="mt-2 max-w-sm divide-y divide-hairline rounded-md border border-hairline">
              {employeeResult?.data.map((employee) => (
                <li key={employee.id}>
                  <button
                    type="button"
                    onClick={() => {
                      setSelectedEmployeeId(employee.id)
                      setEmployeeSearch('')
                    }}
                    className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm text-ink transition hover:bg-surface-2"
                  >
                    <Avatar name={employee.fullName} size="sm" />
                    {employee.fullName}
                  </button>
                </li>
              ))}
            </ul>
          )}
          {selectedEmployee && !employeeSearch && (
            <div className="mt-2 flex items-center gap-2 text-sm text-ink">
              <Avatar name={selectedEmployee.fullName} size="sm" />
              {selectedEmployee.fullName}
              <button
                type="button"
                onClick={() => setSelectedEmployeeId(null)}
                className="text-xs text-ink-subtle hover:text-danger"
              >
                Clear
              </button>
            </div>
          )}
        </div>
      )}

      {(!isPrivileged || selectedEmployeeId) && (
        <div className="rounded-lg border border-hairline bg-surface-1 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">Today</p>
              <p className="text-sm text-ink-muted">{today.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' })}</p>
            </div>
            {todayRecord && <StatusBadge status={todayRecord.status} />}
          </div>

          <div className="mt-4 grid grid-cols-2 gap-6">
            <div>
              <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">Check-in</p>
              <p className="mt-0.5 text-lg font-medium text-ink">{formatTime(todayRecord?.checkInAtUtc ?? null)}</p>
            </div>
            <div>
              <p className="text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">Check-out</p>
              <p className="mt-0.5 text-lg font-medium text-ink">{formatTime(todayRecord?.checkOutAtUtc ?? null)}</p>
            </div>
          </div>

          {needsManualId && (
            <div className="mt-4">
              <label htmlFor="manual-employee-id" className="mb-1 block text-sm font-medium text-ink-muted">
                Employee ID
              </label>
              <input
                id="manual-employee-id"
                value={manualEmployeeId}
                onChange={(e) => setManualEmployeeId(e.target.value)}
                placeholder="00000000-0000-0000-0000-000000000000"
                className="w-full max-w-sm rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
              />
            </div>
          )}

          {actionError && (
            <p role="alert" className="mt-3 text-sm text-danger">
              {actionError}
            </p>
          )}

          <div className="mt-5 flex gap-2">
            {!todayRecord?.checkInAtUtc && (
              <button
                type="button"
                disabled={isActing}
                onClick={() => void handleCheckIn()}
                className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isActing ? 'Checking in…' : 'Check in'}
              </button>
            )}
            {todayRecord?.checkInAtUtc && !todayRecord.checkOutAtUtc && (
              <button
                type="button"
                disabled={isActing}
                onClick={() => void handleCheckOut()}
                className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isActing ? 'Checking out…' : 'Check out'}
              </button>
            )}
            {todayRecord?.checkInAtUtc && todayRecord.checkOutAtUtc && (
              <p className="text-sm text-success">Attendance recorded for today.</p>
            )}
          </div>
        </div>
      )}

      {(!isPrivileged || selectedEmployeeId) && (
        <div className="grid grid-cols-3 gap-4">
          <div className="col-span-2">
            <AttendanceCalendar
              monthDate={monthDate}
              records={records}
              selectedDate={selectedDate}
              onSelectDate={setSelectedDate}
              onMonthChange={(date) => {
                setMonthDate(date)
                setSelectedDate(null)
              }}
            />
          </div>
          <div className="col-span-1 rounded-lg border border-hairline bg-surface-1 p-4">
            {monthError && <p className="text-sm text-danger">{monthError}</p>}
            {isMonthLoading && <p className="text-sm text-ink-subtle">Loading…</p>}
            {!isMonthLoading && !selectedRecord && (
              <p className="text-sm text-ink-subtle">Select a highlighted day to see details.</p>
            )}
            {selectedRecord && (
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <p className="text-sm font-medium text-ink">
                    {new Date(selectedRecord.attendanceDate).toLocaleDateString(undefined, { dateStyle: 'medium' })}
                  </p>
                  <StatusBadge status={selectedRecord.status} />
                </div>
                <p className="text-sm text-ink-muted">Check-in: {formatTime(selectedRecord.checkInAtUtc)}</p>
                <p className="text-sm text-ink-muted">Check-out: {formatTime(selectedRecord.checkOutAtUtc)}</p>
                {selectedRecord.notes && <p className="text-sm text-ink-subtle">{selectedRecord.notes}</p>}
                {!isPrivileged && (
                  <button
                    type="button"
                    onClick={() => setCorrectionRecord(selectedRecord)}
                    className="w-full rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
                  >
                    Request correction
                  </button>
                )}
              </div>
            )}
          </div>
        </div>
      )}

      <Modal isOpen={correctionRecord !== null} onClose={() => setCorrectionRecord(null)} title="Request attendance correction">
        {correctionRecord && (
          <CorrectionForm record={correctionRecord} onClose={() => setCorrectionRecord(null)} onSubmitted={refreshMonth} />
        )}
      </Modal>
    </div>
  )
}
