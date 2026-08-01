import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { ApiSuccessEnvelope, PagedResult } from '@/app/shared/models/apiEnvelope'
import type {
  Candidate,
  CandidateAttachment,
  CandidateListFilters,
  ChecklistItem,
  ConvertToEmployeeInput,
  CreateCandidateInput,
  CreateOfferInput,
  FileDownload,
  Interview,
  Offer,
  RecruitmentRepository,
  RescheduleInterviewInput,
  ScheduleInterviewInput,
  SubmitInterviewFeedbackInput,
  UpdateCandidateInput,
  AddChecklistItemInput,
} from './recruitmentRepository'

// Backend uses ASP.NET's File() helper for attachment/offer-letter downloads, which sets a
// standard `attachment; filename="name.pdf"` (or filename*=UTF-8''name.pdf) header.
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
 * CandidatesController.GetAll, which returns `ApiResponse<PagedResult<CandidateDto>>.Success(result)`
 * — same pattern already used in attendance/audit-logs/performance repositories.
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

export const apiRecruitmentRepository: RecruitmentRepository = {
  async listCandidates(filters?: CandidateListFilters): Promise<PagedResult<Candidate>> {
    const response = await httpClient.get<ApiSuccessEnvelope<BackendPagedResult<Candidate>>>('/candidates', {
      params: filters,
    })
    return unwrapPaged(response)
  },

  async getCandidateById(id: string): Promise<Candidate> {
    const response = await httpClient.get<{ data: Candidate }>(`/candidates/${id}`)
    return unwrap(response)
  },

  async createCandidate(input: CreateCandidateInput): Promise<{ id: string }> {
    const response = await httpClient.post<{ data: { id: string } }>('/candidates', input)
    return unwrap(response)
  },

  async updateCandidate(id: string, input: UpdateCandidateInput): Promise<void> {
    await httpClient.put(`/candidates/${id}`, { id, ...input })
  },

  async deleteCandidate(id: string): Promise<void> {
    await httpClient.delete(`/candidates/${id}`)
  },

  async restoreCandidate(id: string): Promise<void> {
    await httpClient.post(`/candidates/${id}/restore`)
  },

  async rejectCandidate(id: string, reason?: string): Promise<void> {
    await httpClient.post(`/candidates/${id}/reject`, { reason })
  },

  async withdrawCandidate(id: string, reason?: string): Promise<void> {
    await httpClient.post(`/candidates/${id}/withdraw`, { reason })
  },

  async getCandidateAttachments(candidateId: string): Promise<CandidateAttachment[]> {
    const response = await httpClient.get<{ data: CandidateAttachment[] }>(`/candidates/${candidateId}/attachments`)
    return unwrap(response)
  },

  async uploadCandidateAttachment(candidateId: string, file: File): Promise<{ id: string }> {
    const form = new FormData()
    form.append('file', file)
    const response = await httpClient.post<{ data: string }>(`/candidates/${candidateId}/attachments`, form, {
      headers: { 'Content-Type': undefined },
    })
    return { id: unwrap(response) }
  },

  async downloadCandidateAttachment(attachmentId: string): Promise<FileDownload> {
    const response = await httpClient.get<Blob>(`/candidates/attachments/${attachmentId}/download`, {
      responseType: 'blob',
    })
    return { blob: response.data, fileName: extractFileName(response.headers['content-disposition'], 'attachment') }
  },

  async getInterviews(candidateId: string): Promise<Interview[]> {
    const response = await httpClient.get<{ data: Interview[] }>(`/candidates/${candidateId}/interviews`)
    return unwrap(response)
  },

  async scheduleInterview(candidateId: string, input: ScheduleInterviewInput): Promise<{ id: string }> {
    const response = await httpClient.post<{ data: string }>(`/candidates/${candidateId}/interviews`, input)
    return { id: unwrap(response) }
  },

  async rescheduleInterview(id: string, input: RescheduleInterviewInput): Promise<void> {
    await httpClient.post(`/interviews/${id}/reschedule`, input)
  },

  async cancelInterview(id: string): Promise<void> {
    await httpClient.post(`/interviews/${id}/cancel`)
  },

  async markInterviewNoShow(id: string): Promise<void> {
    await httpClient.post(`/interviews/${id}/no-show`)
  },

  async submitInterviewFeedback(id: string, input: SubmitInterviewFeedbackInput): Promise<void> {
    await httpClient.post(`/interviews/${id}/feedback`, input)
  },

  async getOffers(candidateId: string): Promise<Offer[]> {
    const response = await httpClient.get<{ data: Offer[] }>(`/candidates/${candidateId}/offers`)
    return unwrap(response)
  },

  async createOffer(candidateId: string, input: CreateOfferInput): Promise<{ id: string }> {
    const response = await httpClient.post<{ data: string }>(`/candidates/${candidateId}/offers`, input)
    return { id: unwrap(response) }
  },

  async sendOffer(id: string): Promise<void> {
    await httpClient.post(`/offers/${id}/send`)
  },

  async acceptOffer(id: string): Promise<void> {
    await httpClient.post(`/offers/${id}/accept`)
  },

  async rejectOffer(id: string, reason?: string): Promise<void> {
    await httpClient.post(`/offers/${id}/reject`, { reason })
  },

  async withdrawOffer(id: string): Promise<void> {
    await httpClient.post(`/offers/${id}/withdraw`)
  },

  async downloadOffer(id: string): Promise<FileDownload> {
    const response = await httpClient.get<Blob>(`/offers/${id}/download`, { responseType: 'blob' })
    return { blob: response.data, fileName: extractFileName(response.headers['content-disposition'], 'offer-letter.pdf') }
  },

  async getChecklist(candidateId: string): Promise<ChecklistItem[]> {
    const response = await httpClient.get<{ data: ChecklistItem[] }>(`/candidates/${candidateId}/checklist`)
    return unwrap(response)
  },

  async addChecklistItem(candidateId: string, input: AddChecklistItemInput): Promise<{ id: string }> {
    const response = await httpClient.post<{ data: string }>(`/candidates/${candidateId}/checklist`, input)
    return { id: unwrap(response) }
  },

  async completeChecklistItem(itemId: string, notes?: string): Promise<void> {
    await httpClient.post(`/checklist/${itemId}/complete`, { reason: notes })
  },

  async convertToEmployee(candidateId: string, input: ConvertToEmployeeInput): Promise<{ id: string }> {
    const response = await httpClient.post<{ data: string }>(`/candidates/${candidateId}/convert-to-employee`, input)
    return { id: unwrap(response) }
  },
}
