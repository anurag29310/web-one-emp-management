import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { mockEmployees } from '@/app/features/employees/api/mockData'
import { mockClients } from '@/app/features/clients/api/mockData'
import type {
  CreateTaskInput,
  TaskAttachment,
  TaskAttachmentDownload,
  TaskComment,
  TaskItem,
  TaskListFilters,
  TaskRepository,
  TaskStatus,
  UpdateTaskInput,
} from './taskRepository'
import { mockTaskAttachments, mockTaskComments, mockTasks } from './mockData'

let tasks = [...mockTasks]
let comments = [...mockTaskComments]
let attachments = [...mockTaskAttachments]

const READ_ONLY_STATUSES: TaskStatus[] = ['Completed', 'Cancelled']

function nextId(): string {
  return `00000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

function nextTaskNumber(): string {
  return `TSK-${Date.now().toString(16).toUpperCase().slice(-8)}`
}

function resolveEmployeeName(employeeId: string): string | null {
  return mockEmployees.find((e) => e.id === employeeId)?.fullName ?? null
}

// Mirrors TaskItemDto.FromEntity's denormalization of the linked Client onto the task response.
function resolveClientFields(clientId: string | undefined) {
  const client = clientId ? mockClients.find((c) => c.id === clientId) : undefined
  return {
    clientName: client?.clientName ?? null,
    clientAddress: client
      ? [client.addressLine1, client.addressLine2, client.city, client.state, client.country].filter(Boolean).join(', ')
      : null,
    clientLatitude: client?.latitude ?? null,
    clientLongitude: client?.longitude ?? null,
  }
}

function findTaskOrThrow(id: string): TaskItem {
  const task = tasks.find((t) => t.id === id)
  if (!task) {
    throw new AppError(`Task ${id} was not found.`, 404, 'NOT_FOUND')
  }
  return task
}

function assertMutable(task: TaskItem): void {
  if (READ_ONLY_STATUSES.includes(task.status)) {
    throw new AppError(`Task ${task.taskNumber} is ${task.status.toLowerCase()} and is read-only.`, 409, 'TASK_READONLY')
  }
}

function assertStatus(task: TaskItem, expected: TaskStatus[]): void {
  if (!expected.includes(task.status)) {
    throw new AppError(
      `Task ${task.taskNumber} must be ${expected.join(' or ')} for this action (currently ${task.status}).`,
      409,
      'INVALID_STATUS',
    )
  }
}

export const mockTaskRepository: TaskRepository = {
  async list(filters: TaskListFilters = {}): Promise<PagedResult<TaskItem>> {
    await delay(300)
    const { page = 1, pageSize = 20, assignedEmployeeId, clientId, status, priority } = filters

    let filtered = [...tasks]
    if (assignedEmployeeId) filtered = filtered.filter((t) => t.assignedEmployeeId === assignedEmployeeId)
    if (clientId) filtered = filtered.filter((t) => t.clientId === clientId)
    if (status) filtered = filtered.filter((t) => t.status === status)
    if (priority) filtered = filtered.filter((t) => t.priority === priority)

    filtered = filtered.sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc))

    const start = (page - 1) * pageSize
    const pageItems = filtered.slice(start, start + pageSize)

    return {
      data: pageItems,
      page,
      pageSize,
      totalCount: filtered.length,
      totalPages: Math.max(1, Math.ceil(filtered.length / pageSize)),
      correlationId: 'mock-correlation-id',
    }
  },

  async getById(id: string): Promise<TaskItem> {
    await delay(200)
    return findTaskOrThrow(id)
  },

  async create(input: CreateTaskInput): Promise<TaskItem> {
    await delay(300)
    const now = new Date().toISOString()
    const task: TaskItem = {
      id: nextId(),
      taskNumber: nextTaskNumber(),
      title: input.title,
      description: input.description ?? null,
      clientId: input.clientId ?? null,
      ...resolveClientFields(input.clientId),
      assignedEmployeeId: input.assignedEmployeeId,
      assignedEmployeeName: resolveEmployeeName(input.assignedEmployeeId),
      assignedByUserId: '00000000-0000-0000-0000-000000000001',
      assignedDate: now,
      dueDate: input.dueDate ?? null,
      priority: input.priority,
      status: 'Assigned',
      notes: input.notes ?? null,
      completedAtUtc: null,
      createdAtUtc: now,
      updatedAtUtc: null,
    }
    tasks = [task, ...tasks]
    return task
  },

  async update(id: string, input: UpdateTaskInput): Promise<TaskItem> {
    await delay(300)
    const existing = findTaskOrThrow(id)
    assertMutable(existing)
    const updated: TaskItem = {
      ...existing,
      title: input.title,
      description: input.description ?? null,
      clientId: input.clientId ?? null,
      ...resolveClientFields(input.clientId),
      dueDate: input.dueDate ?? null,
      priority: input.priority,
      notes: input.notes ?? null,
      updatedAtUtc: new Date().toISOString(),
    }
    tasks = tasks.map((t) => (t.id === id ? updated : t))
    return updated
  },

  async reassign(id: string, assignedEmployeeId: string): Promise<TaskItem> {
    await delay(300)
    const existing = findTaskOrThrow(id)
    assertMutable(existing)
    const updated: TaskItem = {
      ...existing,
      assignedEmployeeId,
      assignedEmployeeName: resolveEmployeeName(assignedEmployeeId),
      status: 'Assigned',
      updatedAtUtc: new Date().toISOString(),
    }
    tasks = tasks.map((t) => (t.id === id ? updated : t))
    return updated
  },

  async cancel(id: string): Promise<void> {
    await delay(200)
    const existing = findTaskOrThrow(id)
    if (existing.status === 'Completed') {
      throw new AppError(`Task ${existing.taskNumber} is already completed and cannot be cancelled.`, 409, 'TASK_READONLY')
    }
    tasks = tasks.map((t) => (t.id === id ? { ...t, status: 'Cancelled', updatedAtUtc: new Date().toISOString() } : t))
  },

  async accept(id: string): Promise<void> {
    await delay(200)
    const existing = findTaskOrThrow(id)
    assertStatus(existing, ['Assigned'])
    tasks = tasks.map((t) => (t.id === id ? { ...t, status: 'Accepted', updatedAtUtc: new Date().toISOString() } : t))
  },

  async reject(id: string, reason?: string): Promise<void> {
    await delay(200)
    const existing = findTaskOrThrow(id)
    assertStatus(existing, ['Assigned'])
    tasks = tasks.map((t) =>
      t.id === id
        ? {
            ...t,
            status: 'Rejected',
            notes: reason ? [t.notes, `Rejected: ${reason}`].filter(Boolean).join(' | ') : t.notes,
            updatedAtUtc: new Date().toISOString(),
          }
        : t,
    )
  },

  async start(id: string): Promise<void> {
    await delay(200)
    const existing = findTaskOrThrow(id)
    assertStatus(existing, ['Accepted'])
    tasks = tasks.map((t) => (t.id === id ? { ...t, status: 'InProgress', updatedAtUtc: new Date().toISOString() } : t))
  },

  async updateProgress(id: string, status: Extract<TaskStatus, 'InProgress' | 'OnHold'>): Promise<void> {
    await delay(200)
    const existing = findTaskOrThrow(id)
    assertStatus(existing, ['InProgress', 'OnHold'])
    tasks = tasks.map((t) => (t.id === id ? { ...t, status, updatedAtUtc: new Date().toISOString() } : t))
  },

  async complete(id: string): Promise<void> {
    await delay(200)
    const existing = findTaskOrThrow(id)
    assertStatus(existing, ['InProgress', 'OnHold'])
    const now = new Date().toISOString()
    tasks = tasks.map((t) => (t.id === id ? { ...t, status: 'Completed', completedAtUtc: now, updatedAtUtc: now } : t))
  },

  async getComments(id: string): Promise<TaskComment[]> {
    await delay(250)
    return comments.filter((c) => c.taskId === id).sort((a, b) => a.createdAtUtc.localeCompare(b.createdAtUtc))
  },

  async addComment(id: string, comment: string): Promise<TaskComment> {
    await delay(250)
    const task = findTaskOrThrow(id)
    assertMutable(task)
    const created: TaskComment = {
      id: nextId(),
      taskId: id,
      authorUserId: '00000000-0000-0000-0000-000000000001',
      comment,
      createdAtUtc: new Date().toISOString(),
    }
    comments = [...comments, created]
    return created
  },

  async getAttachments(id: string): Promise<TaskAttachment[]> {
    await delay(250)
    return attachments.filter((a) => a.taskId === id).sort((a, b) => b.uploadedAtUtc.localeCompare(a.uploadedAtUtc))
  },

  async uploadAttachment(id: string, file: File): Promise<{ id: string }> {
    await delay(300)
    const task = findTaskOrThrow(id)
    assertMutable(task)
    const created: TaskAttachment = {
      id: nextId(),
      taskId: id,
      originalFileName: file.name,
      contentType: file.type || 'application/octet-stream',
      fileSizeBytes: file.size,
      uploadedAtUtc: new Date().toISOString(),
      uploadedBy: null,
    }
    attachments = [created, ...attachments]
    return { id: created.id }
  },

  async downloadAttachment(attachmentId: string): Promise<TaskAttachmentDownload> {
    await delay(200)
    const attachment = attachments.find((a) => a.id === attachmentId)
    if (!attachment) {
      throw new AppError(`Attachment ${attachmentId} was not found.`, 404, 'NOT_FOUND')
    }
    return {
      blob: new Blob(['Mock attachment content.'], { type: attachment.contentType }),
      fileName: attachment.originalFileName,
    }
  },
}
