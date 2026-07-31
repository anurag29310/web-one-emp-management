import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
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

export const apiReimbursementRepository: ReimbursementRepository = {
  async list(filters?: ReimbursementListFilters): Promise<PagedResult<Reimbursement>> {
    const response = await httpClient.get<PagedResult<Reimbursement>>('/reimbursements', { params: filters })
    return response.data
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
