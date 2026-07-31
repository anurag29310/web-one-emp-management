import type { PagedResult } from '@/app/shared/models/apiEnvelope'

export type TaskStatus = 'Assigned' | 'Accepted' | 'Rejected' | 'InProgress' | 'OnHold' | 'Completed' | 'Cancelled'
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical'

export interface TaskItem {
  id: string
  taskNumber: string
  title: string
  description: string | null
  clientId: string | null
  clientName: string | null
  clientAddress: string | null
  clientLatitude: number | null
  clientLongitude: number | null
  assignedEmployeeId: string
  assignedEmployeeName: string | null
  assignedByUserId: string
  assignedDate: string
  dueDate: string | null
  priority: TaskPriority
  status: TaskStatus
  notes: string | null
  completedAtUtc: string | null
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface TaskListFilters {
  page?: number
  pageSize?: number
  assignedEmployeeId?: string
  clientId?: string
  status?: TaskStatus
  priority?: TaskPriority
}

export interface CreateTaskInput {
  title: string
  description?: string
  clientId?: string
  assignedEmployeeId: string
  dueDate?: string
  priority: TaskPriority
  notes?: string
}

export interface UpdateTaskInput {
  title: string
  description?: string
  clientId?: string
  dueDate?: string
  priority: TaskPriority
  notes?: string
}

export interface TaskComment {
  id: string
  taskId: string
  authorUserId: string
  comment: string
  createdAtUtc: string
}

export interface TaskAttachment {
  id: string
  taskId: string
  originalFileName: string
  contentType: string
  fileSizeBytes: number
  uploadedAtUtc: string
  uploadedBy: string | null
}

export interface TaskAttachmentDownload {
  blob: Blob
  fileName: string
}

export interface TaskRepository {
  list(filters?: TaskListFilters): Promise<PagedResult<TaskItem>>
  getById(id: string): Promise<TaskItem>
  create(input: CreateTaskInput): Promise<TaskItem>
  update(id: string, input: UpdateTaskInput): Promise<TaskItem>
  reassign(id: string, assignedEmployeeId: string): Promise<TaskItem>
  cancel(id: string): Promise<void>
  accept(id: string): Promise<void>
  reject(id: string, reason?: string): Promise<void>
  start(id: string): Promise<void>
  updateProgress(id: string, status: Extract<TaskStatus, 'InProgress' | 'OnHold'>): Promise<void>
  complete(id: string): Promise<void>
  getComments(id: string): Promise<TaskComment[]>
  addComment(id: string, comment: string): Promise<TaskComment>
  getAttachments(id: string): Promise<TaskAttachment[]>
  uploadAttachment(id: string, file: File): Promise<{ id: string }>
  downloadAttachment(attachmentId: string): Promise<TaskAttachmentDownload>
}
