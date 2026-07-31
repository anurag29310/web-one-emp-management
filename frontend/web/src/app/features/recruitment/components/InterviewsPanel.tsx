import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useInterviews } from '../hooks/useInterviews'
import { recruitmentRepository, type Interview } from '../api'
import {
  interviewFeedbackFormSchema,
  rescheduleInterviewFormSchema,
  scheduleInterviewFormSchema,
  type InterviewFeedbackFormInput,
  type InterviewFeedbackFormValues,
  type RescheduleInterviewFormValues,
  type ScheduleInterviewFormValues,
} from '../types/recruitmentSchema'
import { InterviewStatusBadge } from './InterviewStatusBadge'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { AppError } from '@/app/shared/models/appError'
import { Modal } from '@/app/shared/components/Modal'

const inputClass =
  'w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'
const labelClass = 'mb-1 block text-sm font-medium text-ink-muted'
const MODES = ['Onsite', 'Phone', 'VideoCall'] as const
const OUTCOMES = ['Passed', 'Failed', 'OnHold'] as const

function toDatetimeLocal(iso: string): string {
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

export function InterviewsPanel({ candidateId, canSchedule }: { candidateId: string; canSchedule: boolean }) {
  const { interviews, isLoading, error, refresh } = useInterviews(candidateId)
  const { result: employeeResult } = useEmployees({ pageSize: 100 })
  const [isScheduling, setIsScheduling] = useState(false)
  const [reschedulingId, setReschedulingId] = useState<string | null>(null)
  const [feedbackId, setFeedbackId] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [panelError, setPanelError] = useState<string | null>(null)

  const scheduleForm = useForm<ScheduleInterviewFormValues>({
    resolver: zodResolver(scheduleInterviewFormSchema),
    defaultValues: { interviewerEmployeeId: '', round: '', mode: 'VideoCall', scheduledAtUtc: '', durationMinutes: '' },
  })
  const rescheduleForm = useForm<RescheduleInterviewFormValues>({
    resolver: zodResolver(rescheduleInterviewFormSchema),
    defaultValues: { scheduledAtUtc: '', durationMinutes: '' },
  })
  const feedbackForm = useForm<InterviewFeedbackFormInput, unknown, InterviewFeedbackFormValues>({
    resolver: zodResolver(interviewFeedbackFormSchema),
    defaultValues: { feedback: '', rating: 3, outcome: 'Passed' },
  })

  function employeeName(id: string): string {
    return employeeResult?.data.find((e) => e.id === id)?.fullName ?? id
  }

  async function onSchedule(values: ScheduleInterviewFormValues) {
    setPanelError(null)
    try {
      await recruitmentRepository.scheduleInterview(candidateId, {
        interviewerEmployeeId: values.interviewerEmployeeId,
        round: values.round.trim(),
        mode: values.mode,
        scheduledAtUtc: new Date(values.scheduledAtUtc).toISOString(),
        durationMinutes: values.durationMinutes ? Number(values.durationMinutes) : undefined,
      })
      scheduleForm.reset()
      setIsScheduling(false)
      refresh()
    } catch (err) {
      setPanelError(err instanceof AppError ? err.message : 'Failed to schedule interview.')
    }
  }

  async function onReschedule(values: RescheduleInterviewFormValues) {
    if (!reschedulingId) return
    setPanelError(null)
    try {
      await recruitmentRepository.rescheduleInterview(reschedulingId, {
        scheduledAtUtc: new Date(values.scheduledAtUtc).toISOString(),
        durationMinutes: values.durationMinutes ? Number(values.durationMinutes) : undefined,
      })
      setReschedulingId(null)
      refresh()
    } catch (err) {
      setPanelError(err instanceof AppError ? err.message : 'Failed to reschedule interview.')
    }
  }

  async function onSubmitFeedback(values: InterviewFeedbackFormValues) {
    if (!feedbackId) return
    setPanelError(null)
    try {
      await recruitmentRepository.submitInterviewFeedback(feedbackId, values)
      feedbackForm.reset()
      setFeedbackId(null)
      refresh()
    } catch (err) {
      setPanelError(err instanceof AppError ? err.message : 'Failed to submit feedback.')
    }
  }

  async function runRowAction(interview: Interview, action: () => Promise<void>, failureMessage: string) {
    setBusyId(interview.id)
    setPanelError(null)
    try {
      await action()
      refresh()
    } catch (err) {
      setPanelError(err instanceof AppError ? err.message : failureMessage)
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="rounded-lg border border-hairline bg-surface-1">
      <div className="flex items-center justify-between border-b border-hairline p-6">
        <h2 className="text-[18px] font-medium text-ink">Interviews</h2>
        {canSchedule && (
          <button
            type="button"
            onClick={() => setIsScheduling((open) => !open)}
            className="rounded-md bg-surface-2 px-3 py-2 text-sm font-medium text-ink transition hover:bg-surface-3"
          >
            {isScheduling ? 'Cancel' : 'Schedule interview'}
          </button>
        )}
      </div>

      {(error || panelError) && <p className="px-6 pt-4 text-sm text-danger">{error ?? panelError}</p>}

      <div className="p-6">
        {!isLoading && interviews.length === 0 && <p className="text-sm text-ink-subtle">No interviews scheduled yet.</p>}
        <ul className="space-y-3">
          {interviews.map((interview) => (
            <li key={interview.id} className="rounded-md border border-hairline p-3">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-ink">{interview.round}</p>
                  <p className="text-xs text-ink-subtle">
                    {employeeName(interview.interviewerEmployeeId)} · {interview.mode} ·{' '}
                    {new Date(interview.scheduledAtUtc).toLocaleString()}
                    {interview.durationMinutes ? ` · ${interview.durationMinutes} min` : ''}
                  </p>
                </div>
                <InterviewStatusBadge status={interview.status} />
              </div>

              {interview.status === 'Completed' && (
                <p className="mt-2 text-sm text-ink-muted">
                  <span className="font-medium text-ink">{interview.outcome}</span>
                  {interview.rating ? ` · Rating ${interview.rating}/5` : ''}
                  {interview.feedback ? ` — ${interview.feedback}` : ''}
                </p>
              )}

              {canSchedule && interview.status === 'Scheduled' && (
                <div className="mt-2 flex flex-wrap gap-3">
                  <button
                    type="button"
                    onClick={() => setReschedulingId((id) => (id === interview.id ? null : interview.id))}
                    className="text-xs font-medium text-primary-hover hover:underline"
                  >
                    Reschedule
                  </button>
                  <button
                    type="button"
                    onClick={() => setFeedbackId((id) => (id === interview.id ? null : interview.id))}
                    className="text-xs font-medium text-primary-hover hover:underline"
                  >
                    Submit feedback
                  </button>
                  <button
                    type="button"
                    disabled={busyId === interview.id}
                    onClick={() =>
                      void runRowAction(interview, () => recruitmentRepository.markInterviewNoShow(interview.id), 'Failed to mark no-show.')
                    }
                    className="text-xs font-medium text-warning hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    No-show
                  </button>
                  <button
                    type="button"
                    disabled={busyId === interview.id}
                    onClick={() =>
                      void runRowAction(interview, () => recruitmentRepository.cancelInterview(interview.id), 'Failed to cancel interview.')
                    }
                    className="text-xs font-medium text-danger hover:underline disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Cancel
                  </button>
                </div>
              )}

              {reschedulingId === interview.id && (
                <form onSubmit={rescheduleForm.handleSubmit(onReschedule)} noValidate className="mt-3 space-y-2 border-t border-hairline pt-3">
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label htmlFor={`reschedule-time-${interview.id}`} className={labelClass}>
                        New date/time
                      </label>
                      <input
                        id={`reschedule-time-${interview.id}`}
                        type="datetime-local"
                        defaultValue={toDatetimeLocal(interview.scheduledAtUtc)}
                        className={inputClass}
                        {...rescheduleForm.register('scheduledAtUtc')}
                      />
                    </div>
                    <div>
                      <label htmlFor={`reschedule-duration-${interview.id}`} className={labelClass}>
                        Duration (min)
                      </label>
                      <input
                        id={`reschedule-duration-${interview.id}`}
                        type="number"
                        min="1"
                        defaultValue={interview.durationMinutes ?? ''}
                        className={inputClass}
                        {...rescheduleForm.register('durationMinutes')}
                      />
                    </div>
                  </div>
                  <button
                    type="submit"
                    disabled={rescheduleForm.formState.isSubmitting}
                    className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Save new time
                  </button>
                </form>
              )}

              {feedbackId === interview.id && (
                <form onSubmit={feedbackForm.handleSubmit(onSubmitFeedback)} noValidate className="mt-3 space-y-2 border-t border-hairline pt-3">
                  <div>
                    <label htmlFor={`feedback-text-${interview.id}`} className={labelClass}>
                      Feedback
                    </label>
                    <textarea id={`feedback-text-${interview.id}`} rows={2} className={inputClass} {...feedbackForm.register('feedback')} />
                    {feedbackForm.formState.errors.feedback && (
                      <p className="mt-1 text-xs text-danger">{feedbackForm.formState.errors.feedback.message}</p>
                    )}
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label htmlFor={`feedback-rating-${interview.id}`} className={labelClass}>
                        Rating (1-5)
                      </label>
                      <input
                        id={`feedback-rating-${interview.id}`}
                        type="number"
                        min="1"
                        max="5"
                        className={inputClass}
                        {...feedbackForm.register('rating')}
                      />
                    </div>
                    <div>
                      <label htmlFor={`feedback-outcome-${interview.id}`} className={labelClass}>
                        Outcome
                      </label>
                      <select id={`feedback-outcome-${interview.id}`} className={inputClass} {...feedbackForm.register('outcome')}>
                        {OUTCOMES.map((o) => (
                          <option key={o} value={o}>
                            {o}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>
                  <button
                    type="submit"
                    disabled={feedbackForm.formState.isSubmitting}
                    className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Submit feedback
                  </button>
                </form>
              )}
            </li>
          ))}
        </ul>
      </div>

      <Modal isOpen={isScheduling} onClose={() => setIsScheduling(false)} title="Schedule interview">
        <form onSubmit={scheduleForm.handleSubmit(onSchedule)} noValidate className="space-y-3">
          <div>
            <label htmlFor="schedule-interviewer" className={labelClass}>
              Interviewer
            </label>
            <select id="schedule-interviewer" className={inputClass} {...scheduleForm.register('interviewerEmployeeId')}>
              <option value="">Select employee…</option>
              {employeeResult?.data.map((employee) => (
                <option key={employee.id} value={employee.id}>
                  {employee.fullName}
                </option>
              ))}
            </select>
            {scheduleForm.formState.errors.interviewerEmployeeId && (
              <p className="mt-1 text-xs text-danger">{scheduleForm.formState.errors.interviewerEmployeeId.message}</p>
            )}
          </div>
          <div>
            <label htmlFor="schedule-round" className={labelClass}>
              Round
            </label>
            <input id="schedule-round" placeholder="Technical Round 1" className={inputClass} {...scheduleForm.register('round')} />
            {scheduleForm.formState.errors.round && <p className="mt-1 text-xs text-danger">{scheduleForm.formState.errors.round.message}</p>}
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="schedule-mode" className={labelClass}>
                Mode
              </label>
              <select id="schedule-mode" className={inputClass} {...scheduleForm.register('mode')}>
                {MODES.map((m) => (
                  <option key={m} value={m}>
                    {m}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="schedule-duration" className={labelClass}>
                Duration (min)
              </label>
              <input id="schedule-duration" type="number" min="1" className={inputClass} {...scheduleForm.register('durationMinutes')} />
            </div>
          </div>
          <div>
            <label htmlFor="schedule-time" className={labelClass}>
              Date/time
            </label>
            <input id="schedule-time" type="datetime-local" className={inputClass} {...scheduleForm.register('scheduledAtUtc')} />
            {scheduleForm.formState.errors.scheduledAtUtc && (
              <p className="mt-1 text-xs text-danger">{scheduleForm.formState.errors.scheduledAtUtc.message}</p>
            )}
          </div>
          {panelError && (
            <p role="alert" className="text-sm text-danger">
              {panelError}
            </p>
          )}
          <button
            type="submit"
            disabled={scheduleForm.formState.isSubmitting}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
          >
            {scheduleForm.formState.isSubmitting ? 'Scheduling…' : 'Schedule'}
          </button>
        </form>
      </Modal>
    </div>
  )
}
