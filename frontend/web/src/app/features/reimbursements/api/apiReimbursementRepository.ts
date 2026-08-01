import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { ApiSuccessEnvelope, PagedResult } from '@/app/shared/models/apiEnvelope'
import type {
  Reimbursement,
  ReimbursementAttachment,
  ReimbursementAttachmentDownload,
  ReimbursementInput,
  ReimbursementListFilters,
  ReimbursementRepository,
} from './reimbursementRepository'

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
 * shape documented in api-specification.md §2.3. Confirmed against
 * ReimbursementController.GetAll, which returns
 * `ApiResponse<PagedResult<ReimbursementDto>>.Success(result)` — same pattern already used in
 * attendance/audit-logs/performance repositories.
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

export const apiReimbursementRepository: ReimbursementRepository = {
  async list(filters?: ReimbursementListFilters): Promise<PagedResult<Reimbursement>> {
    const response = await httpClient.get<ApiSuccessEnvelope<BackendPagedResult<Reimbursement>>>('/reimbursements', {
      params: filters,
    })
    return unwrapPaged(response)
  },

  async getById(id: string): Promise<Reimbursement> {
    const response = await httpClient.get<{ data: Reimbursement }>(`/reimbursements/${id}`)
    return unwrap(response)
  },

  async create(input: ReimbursementInput): Promise<Reimbursement> {
    const response = await httpClient.post<{ data: Reimbursement }>('/reimbursements', input)
    return unwrap(response)
  },

  async update(id: string, input: ReimbursementInput): Promise<Reimbursement> {
    const response = await httpClient.put<{ data: Reimbursement }>(`/reimbursements/${id}`, { id, ...input })
    return unwrap(response)
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/reimbursements/${id}`)
  },

  async submit(id: string): Promise<void> {
    await httpClient.post(`/reimbursements/${id}/submit`)
  },

  async startReview(id: string): Promise<void> {
    await httpClient.post(`/reimbursements/${id}/start-review`)
  },

  async approve(id: string): Promise<void> {
    await httpClient.post(`/reimbursements/${id}/approve`)
  },

  async reject(id: string, remarks: string): Promise<void> {
    await httpClient.post(`/reimbursements/${id}/reject`, { remarks })
  },

  async requestChanges(id: string, remarks: string): Promise<void> {
    await httpClient.post(`/reimbursements/${id}/request-changes`, { remarks })
  },

  async getAttachments(id: string): Promise<ReimbursementAttachment[]> {
    const response = await httpClient.get<{ data: ReimbursementAttachment[] }>(`/reimbursements/${id}/attachments`)
    return unwrap(response)
  },

  async uploadAttachment(id: string, file: File): Promise<{ id: string }> {
    const form = new FormData()
    form.append('file', file)

    // Clear the shared httpClient instance's default `Content-Type: application/json` header for
    // this request — leaving it in place would make axios JSON-serialize the FormData body
    // instead of sending a real multipart/form-data request with a boundary.
    const response = await httpClient.post<{ data: string }>(`/reimbursements/${id}/attachments`, form, {
      headers: { 'Content-Type': undefined },
    })
    return { id: unwrap(response) }
  },

  async downloadAttachment(attachmentId: string): Promise<ReimbursementAttachmentDownload> {
    const response = await httpClient.get<Blob>(`/reimbursements/attachments/${attachmentId}/download`, {
      responseType: 'blob',
    })
    return {
      blob: response.data,
      fileName: extractFileName(response.headers['content-disposition'], 'attachment'),
    }
  },
}
