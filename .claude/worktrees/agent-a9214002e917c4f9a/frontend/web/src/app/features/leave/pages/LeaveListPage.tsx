import { useState, type FormEvent } from 'react'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { useLeaveRequests } from '../hooks/useLeaveRequests'
import { leaveRepository, type LeaveStatus } from '../api'
import { KNOWN_LEAVE_TYPES, leaveTypeName } from '../api/leaveTypes'
import { AppError } from '@/app/shared/models/appError'
import { appConfig } from '@/app/core/config/env'
import { Modal } from '@/app/shared/components/Modal'

const STATUS_STYLES: Record<LeaveStatus, string> = {
  Pending: 'bg-warning/15 text-warning ring-warning/30',
  Approved: 'bg-success/15 text-success ring-success/30',
  Rejected: 'bg-danger/15 text-danger ring-danger/30',
  Cancelled: 'bg-surface-2 text-ink-subtle ring-hairline-strong',
}

function LeaveStatusBadge({ status }: { status: LeaveStatus }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${STATUS_STYLES[status]}`}
    >
      {status}
    </span>
  )
}

function ApplyLeaveForm({ onApplied }: { onApplied: () => void }) {
  const { result: employeesResult } = useEmployees({ pageSize: 100 })
  const [employeeId, setEmployeeId] = useState('')
  const [leaveTypeId, setLeaveTypeId] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [reason, setReason] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError(null)
    if (!employeeId || !leaveTypeId || !startDate || !endDate) {
      setFormError('Employee, leave type, and dates are required.')
      return
    }
    const totalDays =
      Math.round((new Date(endDate).getTime() - new Date(startDate).getTime()) / 86_400_000) + 1
    if (totalDays < 1) {
      setFormError('End date must be on or after the start date.')
      return
    }

    setIsSaving(true)
    try {
      await leaveRepository.apply({ employeeId, leaveTypeId, startDate, endDate, totalDays, reason })
      setEmployeeId('')
      setLeaveTypeId('')
      setStartDate('')
      setEndDate('')
      setReason('')
      onApplied()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to submit leave request.')
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-3">
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="mb-1 block text-sm font-medium text-ink-muted">Employee</label>
          <select
            value={employeeId}
            onChange={(e) => setEmployeeId(e.target.value)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          >
            <option value="">Select employee…</option>
            {employeesResult?.data.map((employee) => (
              <option key={employee.id} value={employee.id}>
                {employee.fullName}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-ink-muted">Leave type ID</label>
          <input
            value={leaveTypeId}
            onChange={(e) => setLeaveTypeId(e.target.value)}
            placeholder="Paste a leave type guid…"
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none placeholder:text-ink-tertiary focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          />
        </div>
      </div>

      {appConfig.dataSource === 'mock' && (
        <p className="text-xs text-ink-subtle">
          Mock leave type IDs:{' '}
          {KNOWN_LEAVE_TYPES.map((t) => (
            <button
              key={t.id}
              type="button"
              onClick={() => setLeaveTypeId(t.id)}
              className="mr-2 font-mono text-primary-hover hover:underline"
            >
              {t.name}
            </button>
          ))}
        </p>
      )}

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="mb-1 block text-sm font-medium text-ink-muted">Start date</label>
          <input
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-ink-muted">End date</label>
          <input
            type="date"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
            className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
          />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium text-ink-muted">Reason</label>
        <textarea
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          rows={2}
          className="w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50"
        />
      </div>

      {formError && <p className="text-sm text-danger">{formError}</p>}

      <button
        type="submit"
        disabled={isSaving}
        className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:opacity-60"
      >
        {isSaving ? 'Submitting…' : 'Apply for leave'}
      </button>
    </form>
  )
}

export function LeaveListPage() {
  const { result, isLoading, error, refresh } = useLeaveRequests({ pageSize: 50 })
  const { result: employeesResult } = useEmployees({ pageSize: 100 })
  const [isFormOpen, setIsFormOpen] = useState(false)

  function employeeName(employeeId: string): string {
    return employeesResult?.data.find((e) => e.id === employeeId)?.fullName ?? employeeId
  }

  async function handleDecision(id: string, decision: 'approve' | 'reject') {
    if (decision === 'approve') {
      await leaveRepository.approve(id)
    } else {
      await leaveRepository.reject(id)
    }
    refresh()
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Leave Requests</h1>
          <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
        </div>
        <button
          type="button"
          onClick={() => setIsFormOpen((open) => !open)}
          className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
        >
          {isFormOpen ? 'Cancel' : 'Apply for leave'}
        </button>
      </div>

      <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="Apply for leave">
        <ApplyLeaveForm
          onApplied={() => {
            setIsFormOpen(false)
            refresh()
          }}
        />
      </Modal>

      {error && <p className="text-sm text-danger">{error}</p>}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Employee</th>
              <th className="px-4 py-3">Type</th>
              <th className="px-4 py-3">Dates</th>
              <th className="px-4 py-3">Days</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody className="divide-y divide-hairline">
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <tr key={i}>
                  <td className="px-4 py-3" colSpan={6}>
                    <div className="h-5 animate-pulse rounded bg-surface-2" />
                  </td>
                </tr>
              ))}

            {!isLoading && result?.data.length === 0 && (
              <tr>
                <td className="px-4 py-8 text-center text-ink-subtle" colSpan={6}>
                  No leave requests yet.
                </td>
              </tr>
            )}

            {!isLoading &&
              result?.data.map((request) => (
                <tr key={request.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3 font-medium text-ink">{employeeName(request.employeeId)}</td>
                  <td className="px-4 py-3 text-ink-muted">{leaveTypeName(request.leaveTypeId)}</td>
                  <td className="px-4 py-3 text-ink-muted">
                    {request.startDate} → {request.endDate}
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{request.totalDays}</td>
                  <td className="px-4 py-3">
                    <LeaveStatusBadge status={request.status} />
                  </td>
                  <td className="px-4 py-3 text-right">
                    {request.status === 'Pending' && (
                      <div className="flex justify-end gap-3">
                        <button
                          type="button"
                          onClick={() => void handleDecision(request.id, 'approve')}
                          className="text-xs font-medium text-success hover:underline"
                        >
                          Approve
                        </button>
                        <button
                          type="button"
                          onClick={() => void handleDecision(request.id, 'reject')}
                          className="text-xs font-medium text-danger hover:underline"
                        >
                          Reject
                        </button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
