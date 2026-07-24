import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import { ALLOWED_DOCUMENT_CONTENT_TYPES, MAX_DOCUMENT_SIZE_BYTES } from '../types/documentSchema'
import type {
  DocumentDownload,
  DocumentListFilters,
  DocumentRepository,
  EmployeeDocument,
  UploadDocumentInput,
} from './documentRepository'
import { mockDocuments } from './mockData'

const documents = [...mockDocuments]

function nextId(): string {
  return `30000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

export const mockDocumentRepository: DocumentRepository = {
  async list(employeeId: string, filters: DocumentListFilters = {}): Promise<EmployeeDocument[]> {
    await delay(300)
    const { documentType, page = 1, pageSize = 20 } = filters

    let filtered = documents.filter((d) => d.employeeId === employeeId)
    if (documentType) {
      filtered = filtered.filter((d) => d.documentType === documentType)
    }
    filtered = [...filtered].sort(
      (a, b) => new Date(b.uploadedAtUtc).getTime() - new Date(a.uploadedAtUtc).getTime(),
    )

    const start = (page - 1) * pageSize
    return filtered.slice(start, start + pageSize)
  },

  async upload({ employeeId, documentType, file, expiresAtUtc }: UploadDocumentInput): Promise<{ id: string }> {
    await delay(400)

    // Mirrors UploadDocumentCommandHandler.cs's validation so mock mode behaves the same as the
    // real API for oversized/unsupported uploads.
    if (file.size === 0) {
      throw new AppError('File content is empty.', 400, 'VALIDATION_ERROR')
    }
    if (file.size > MAX_DOCUMENT_SIZE_BYTES) {
      throw new AppError('File exceeds maximum allowed size.', 400, 'VALIDATION_ERROR')
    }
    if (!(ALLOWED_DOCUMENT_CONTENT_TYPES as readonly string[]).includes(file.type)) {
      throw new AppError('Unsupported file type.', 400, 'VALIDATION_ERROR')
    }

    const document: EmployeeDocument = {
      id: nextId(),
      employeeId,
      documentType,
      originalFileName: file.name,
      contentType: file.type,
      fileSizeBytes: file.size,
      uploadedAtUtc: new Date().toISOString(),
      expiresAtUtc: expiresAtUtc || null,
    }
    documents.unshift(document)
    return { id: document.id }
  },

  async download(employeeId: string, documentId: string): Promise<DocumentDownload> {
    await delay(300)
    const document = documents.find((d) => d.id === documentId && d.employeeId === employeeId)
    if (!document) {
      throw new AppError('Document was not found.', 404, 'NOT_FOUND')
    }
    const blob = new Blob([`Mock file content for ${document.originalFileName}`], {
      type: document.contentType,
    })
    return { blob, fileName: document.originalFileName }
  },

  async remove(employeeId: string, documentId: string): Promise<void> {
    await delay(250)
    const index = documents.findIndex((d) => d.id === documentId && d.employeeId === employeeId)
    if (index === -1) {
      throw new AppError('Document was not found.', 404, 'NOT_FOUND')
    }
    documents.splice(index, 1)
  },
}
