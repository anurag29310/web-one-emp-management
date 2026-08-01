import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { ApiSuccessEnvelope, PagedResult } from '@/app/shared/models/apiEnvelope'
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

// Backend uses ASP.NET's File() helper for attachment downloads, which sets a standard
// `attachment; filename="name.pdf"` (or filename*=UTF-8''name.pdf) header.
function extractFileName(contentDisposition: string | undefined, fallback: string): string {
  if (!contentDisposition) return fallback
  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition)
  if (utf8Match) return decodeURIComponent(utf8Match[1])
  const quotedMatch = /filename="?([^";]+)"?/i.exec(contentDisposition)
  return quotedMatch ? quotedMatch[1] : fallback
}

/**
 * Shape of EMS.Application.Common.DTOs.PagedResult<T> as it actually serializes: the backend
 * wraps it a second time inside ApiResponse<T>, so a list endpoint's JSON body is
 * `{ data: { data: [...], page, pageSize, totalCount, totalPages }, message, correlationId }`
 * — the pagination fields live one level deeper than the flat `{ data, page, pageSize, ... }`
 * shape documented in api-specification.md §2.3. Confirmed against TaskController.GetAll,
 * which returns `ApiResponse<PagedResult<TaskItemDto>>.Success(result)` — same pattern already
 * used in attendance/audit-logs/performance repositories.
 */
interface BackendPagedResult<T> {
  data: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

function unwrapPaged<T>(response: { data: ApiSuccessEnvelope<BackendPagedResult<T>> }): PagedResult<T> {
  const envelope = response.data
  const paged = envelope.data
  return {
    data: paged.data,
    page: paged.page,
    pageSize: paged.pageSize,
    totalCount: paged.totalCount,
    totalPages: paged.totalPages,
    correlationId: envelope.correlationId,
  }
}

export const apiTaskRepository: TaskRepository = {
  async list(filters?: TaskListFilters): Promise<PagedResult<TaskItem>> {
    const response = await httpClient.get<ApiSuccessEnvelope<BackendPagedResult<TaskItem>>>('/tasks', {
      params: filters,
    })
    return unwrapPaged(response)
  },

  async getById(id: string): Promise<TaskItem> {
    const response = await httpClient.get<{ data: TaskItem }>(`/tasks/${id}`)
    return unwrap(response)
  },

  async create(input: CreateTaskInput): Promise<TaskItem> {
    const response = await httpClient.post<{ data: TaskItem }>('/tasks', input)
    return unwrap(response)
  },

  async update(id: string, input: UpdateTaskInput): Promise<TaskItem> {
    const response = await httpClient.put<{ data: TaskItem }>(`/tasks/${id}`, { id, ...input })
    return unwrap(response)
  },

  async reassign(id: string, assignedEmployeeId: string): Promise<TaskItem> {
    const response = await httpClient.post<{ data: TaskItem }>(`/tasks/${id}/reassign`, { assignedEmployeeId })
    return unwrap(response)
  },

  async cancel(id: string): Promise<void> {
    await httpClient.post(`/tasks/${id}/cancel`)
  },

  async accept(id: string): Promise<void> {
    await httpClient.post(`/tasks/${id}/accept`)
  },

  async reject(id: string, reason?: string): Promise<void> {
    await httpClient.post(`/tasks/${id}/reject`, { reason })
  },

  async start(id: string): Promise<void> {
    await httpClient.post(`/tasks/${id}/start`)
  },

  async updateProgress(id: string, status: Extract<TaskStatus, 'InProgress' | 'OnHold'>): Promise<void> {
    await httpClient.post(`/tasks/${id}/progress`, { status })
  },

  async complete(id: string): Promise<void> {
    await httpClient.post(`/tasks/${id}/complete`)
  },

  async getComments(id: string): Promise<TaskComment[]> {
    const response = await httpClient.get<{ data: TaskComment[] }>(`/tasks/${id}/comments`)
    return unwrap(response)
  },

  async addComment(id: string, comment: string): Promise<TaskComment> {
    const response = await httpClient.post<{ data: TaskComment }>(`/tasks/${id}/comments`, { comment })
    return unwrap(response)
  },

  async getAttachments(id: string): Promise<TaskAttachment[]> {
    const response = await httpClient.get<{ data: TaskAttachment[] }>(`/tasks/${id}/attachments`)
    return unwrap(response)
  },

  async uploadAttachment(id: string, file: File): Promise<{ id: string }> {
    const form = new FormData()
    form.append('file', file)

    // Clear the shared httpClient instance's default `Content-Type: application/json` header for
    // this request — leaving it in place would make axios JSON-serialize the FormData body
    // instead of sending a real multipart/form-data request with a boundary.
    const response = await httpClient.post<{ data: string }>(`/tasks/${id}/attachments`, form, {
      headers: { 'Content-Type': undefined },
    })
    return { id: unwrap(response) }
  },

  async downloadAttachment(attachmentId: string): Promise<TaskAttachmentDownload> {
    const response = await httpClient.get<Blob>(`/tasks/attachments/${attachmentId}/download`, {
      responseType: 'blob',
    })
    return {
      blob: response.data,
      fileName: extractFileName(response.headers['content-disposition'], 'attachment'),
    }
  },
}
