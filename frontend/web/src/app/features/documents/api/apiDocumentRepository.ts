import { httpClient } from '@/app/core/api/httpClient'
import type {
  DocumentDownload,
  DocumentListFilters,
  DocumentRepository,
  EmployeeDocument,
  UploadDocumentInput,
} from './documentRepository'

// EmployeeDocumentsController.cs returns raw JSON bodies (a bare array from List, a bare Guid
// from Upload) rather than the app's usual `{ data, message, correlationId }` envelope — do not
// route these calls through `unwrap()`.

// Backend uses ASP.NET's File() helper, which sets a standard
// `attachment; filename="name.pdf"` (or filename*=UTF-8''name.pdf) header.
function extractFileName(contentDisposition: string | undefined, fallback: string): string {
  if (!contentDisposition) return fallback
  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition)
  if (utf8Match) return decodeURIComponent(utf8Match[1])
  const quotedMatch = /filename="?([^";]+)"?/i.exec(contentDisposition)
  return quotedMatch ? quotedMatch[1] : fallback
}

export const apiDocumentRepository: DocumentRepository = {
  async list(employeeId: string, filters: DocumentListFilters = {}): Promise<EmployeeDocument[]> {
    const response = await httpClient.get<EmployeeDocument[]>(`/employees/${employeeId}/documents`, {
      params: filters,
    })
    return response.data
  },

  async upload({ employeeId, documentType, file, expiresAtUtc }: UploadDocumentInput): Promise<{ id: string }> {
    const form = new FormData()
    form.append('file', file)
    form.append('documentType', documentType)
    if (expiresAtUtc) form.append('expiresAtUtc', expiresAtUtc)

    // Clear the shared httpClient instance's default `Content-Type: application/json` header for
    // this request — leaving it in place would make axios JSON-serialize the FormData body
    // instead of sending a real multipart/form-data request with a boundary.
    const response = await httpClient.post<string>(`/employees/${employeeId}/documents`, form, {
      headers: { 'Content-Type': undefined },
    })
    return { id: response.data }
  },

  async download(employeeId: string, documentId: string): Promise<DocumentDownload> {
    // responseType 'blob' is required — the download endpoint returns a raw file body, not the
    // usual JSON envelope.
    const response = await httpClient.get<Blob>(`/employees/${employeeId}/documents/${documentId}/download`, {
      responseType: 'blob',
    })
    return {
      blob: response.data,
      fileName: extractFileName(response.headers['content-disposition'], 'document'),
    }
  },

  async remove(employeeId: string, documentId: string): Promise<void> {
    await httpClient.delete(`/employees/${employeeId}/documents/${documentId}`)
  },
}
