import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTasks } from '../hooks/useTasks'
import { taskRepository, type TaskPriority, type TaskStatus } from '../api'
import { createTaskFormSchema, type CreateTaskFormValues } from '../types/taskSchema'
import { TaskStatusBadge } from '../components/TaskStatusBadge'
import { TaskPriorityBadge } from '../components/TaskPriorityBadge'
import { useEmployees } from '@/app/features/employees/hooks/useEmployees'
import { useClients } from '@/app/features/clients/hooks/useClients'
import { AppError } from '@/app/shared/models/appError'
import { useAuth } from '@/app/core/auth/useAuth'
import { Modal } from '@/app/shared/components/Modal'

const STATUS_OPTIONS: TaskStatus[] = ['Assigned', 'Accepted', 'Rejected', 'InProgress', 'OnHold', 'Completed', 'Cancelled']
const PRIORITY_OPTIONS: TaskPriority[] = ['Low', 'Medium', 'High', 'Critical']

const inputClass =
  'w-full rounded-md border border-hairline-strong bg-surface-2 px-3 py-2 text-sm text-ink outline-none focus:border-primary-focus focus:ring-2 focus:ring-primary-focus/50'
const labelClass = 'mb-1 block text-sm font-medium text-ink-muted'

function CreateTaskForm({ onCreated }: { onCreated: () => void }) {
  const { result: employeeResult } = useEmployees({ pageSize: 100 })
  const { result: clientResult } = useClients({ pageSize: 100, isActive: true })
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateTaskFormValues>({
    resolver: zodResolver(createTaskFormSchema),
    defaultValues: {
      title: '',
      description: '',
      clientId: '',
      assignedEmployeeId: '',
      dueDate: '',
      priority: 'Medium',
      notes: '',
    },
  })

  async function onSubmit(values: CreateTaskFormValues) {
    setFormError(null)
    try {
      await taskRepository.create({
        title: values.title.trim(),
        description: values.description?.trim() || undefined,
        clientId: values.clientId || undefined,
        assignedEmployeeId: values.assignedEmployeeId,
        dueDate: values.dueDate || undefined,
        priority: values.priority,
        notes: values.notes?.trim() || undefined,
      })
      reset()
      onCreated()
    } catch (err) {
      setFormError(err instanceof AppError ? err.message : 'Failed to create task.')
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-3">
      <div>
        <label htmlFor="task-title" className={labelClass}>
          Title
        </label>
        <input id="task-title" aria-invalid={Boolean(errors.title)} className={inputClass} {...register('title')} />
        {errors.title && <p className="mt-1 text-xs text-danger">{errors.title.message}</p>}
      </div>

      <div>
        <label htmlFor="task-description" className={labelClass}>
          Description
        </label>
        <textarea id="task-description" rows={2} className={inputClass} {...register('description')} />
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="task-assignee" className={labelClass}>
            Assignee
          </label>
          <select id="task-assignee" aria-invalid={Boolean(errors.assignedEmployeeId)} className={inputClass} {...register('assignedEmployeeId')}>
            <option value="">Select employee…</option>
            {employeeResult?.data.map((employee) => (
              <option key={employee.id} value={employee.id}>
                {employee.fullName}
              </option>
            ))}
          </select>
          {errors.assignedEmployeeId && <p className="mt-1 text-xs text-danger">{errors.assignedEmployeeId.message}</p>}
        </div>
        <div>
          <label htmlFor="task-client" className={labelClass}>
            Client <span className="text-ink-tertiary">(optional)</span>
          </label>
          <select id="task-client" className={inputClass} {...register('clientId')}>
            <option value="">No client visit</option>
            {clientResult?.data.map((client) => (
              <option key={client.id} value={client.id}>
                {client.clientName}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label htmlFor="task-due-date" className={labelClass}>
            Due date <span className="text-ink-tertiary">(optional)</span>
          </label>
          <input id="task-due-date" type="date" className={inputClass} {...register('dueDate')} />
        </div>
        <div>
          <label htmlFor="task-priority" className={labelClass}>
            Priority
          </label>
          <select id="task-priority" className={inputClass} {...register('priority')}>
            {PRIORITY_OPTIONS.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div>
        <label htmlFor="task-notes" className={labelClass}>
          Notes
        </label>
        <textarea id="task-notes" rows={2} className={inputClass} {...register('notes')} />
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
        {isSubmitting ? 'Creating…' : 'Create task'}
      </button>
    </form>
  )
}

export function TaskListPage() {
  const [status, setStatus] = useState<TaskStatus | ''>('')
  const [priority, setPriority] = useState<TaskPriority | ''>('')
  const { user } = useAuth()
  const canManage = user?.role === 'Admin'
  const { result, isLoading, error, refresh } = useTasks({
    pageSize: 50,
    status: status || undefined,
    priority: priority || undefined,
  })
  const [isFormOpen, setIsFormOpen] = useState(false)

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[28px] font-semibold leading-[1.2] tracking-[-0.6px] text-ink">Tasks</h1>
          <p className="text-sm text-ink-subtle">{result ? `${result.totalCount} total` : ' '}</p>
        </div>
        {canManage && (
          <button
            type="button"
            onClick={() => setIsFormOpen((open) => !open)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-white transition hover:bg-primary-hover"
          >
            {isFormOpen ? 'Cancel' : 'New task'}
          </button>
        )}
      </div>

      {canManage && (
        <Modal isOpen={isFormOpen} onClose={() => setIsFormOpen(false)} title="New task">
          <CreateTaskForm
            onCreated={() => {
              setIsFormOpen(false)
              refresh()
            }}
          />
        </Modal>
      )}

      <div className="flex items-center gap-3">
        <select
          aria-label="Filter by status"
          value={status}
          onChange={(e) => setStatus(e.target.value as TaskStatus | '')}
          className={`w-48 ${inputClass}`}
        >
          <option value="">All statuses</option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
        <select
          aria-label="Filter by priority"
          value={priority}
          onChange={(e) => setPriority(e.target.value as TaskPriority | '')}
          className={`w-48 ${inputClass}`}
        >
          <option value="">All priorities</option>
          {PRIORITY_OPTIONS.map((p) => (
            <option key={p} value={p}>
              {p}
            </option>
          ))}
        </select>
      </div>

      {!canManage && <p className="text-sm text-ink-subtle">Showing tasks assigned to you.</p>}

      {error && (
        <p role="alert" className="text-sm text-danger">
          {error}
        </p>
      )}

      <div className="overflow-hidden rounded-lg border border-hairline bg-surface-1">
        <table className="w-full text-sm">
          <thead className="bg-surface-2 text-left text-[13px] font-medium uppercase tracking-[0.4px] text-ink-subtle">
            <tr>
              <th className="px-4 py-3">Task</th>
              <th className="px-4 py-3">Client</th>
              <th className="px-4 py-3">Assignee</th>
              <th className="px-4 py-3">Due</th>
              <th className="px-4 py-3">Priority</th>
              <th className="px-4 py-3">Status</th>
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
                  No tasks found.
                </td>
              </tr>
            )}

            {!isLoading &&
              result?.data.map((task) => (
                <tr key={task.id} className="transition hover:bg-surface-2">
                  <td className="px-4 py-3">
                    <Link to={`/tasks/${task.id}`} className="font-medium text-ink hover:text-primary-hover">
                      {task.title}
                    </Link>
                    <p className="font-mono text-xs text-ink-subtle">{task.taskNumber}</p>
                  </td>
                  <td className="px-4 py-3 text-ink-muted">{task.clientName ?? '—'}</td>
                  <td className="px-4 py-3 text-ink-muted">{task.assignedEmployeeName ?? '—'}</td>
                  <td className="px-4 py-3 text-ink-muted">
                    {task.dueDate ? new Date(task.dueDate).toLocaleDateString() : '—'}
                  </td>
                  <td className="px-4 py-3">
                    <TaskPriorityBadge priority={task.priority} />
                  </td>
                  <td className="px-4 py-3">
                    <TaskStatusBadge status={task.status} />
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
