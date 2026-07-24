export interface EmployeeDocument {
  id: string
  employeeId: string
  documentType: string
  originalFileName: string
  contentType: string
  fileSizeBytes: number
  uploadedAtUtc: string
  expiresAtUtc: string | null
}

export interface DocumentListFilters {
  documentType?: string
  page?: number
  pageSize?: number
}

export interface UploadDocumentInput {
  employeeId: string
  documentType: string
  file: File
  expiresAtUtc?: string | null
}

export interface DocumentDownload {
  blob: Blob
  fileName: string
}

export interface DocumentRepository {
  list(employeeId: string, filters?: DocumentListFilters): Promise<EmployeeDocument[]>
  upload(input: UploadDocumentInput): Promise<{ id: string }>
  download(employeeId: string, documentId: string): Promise<DocumentDownload>
  remove(employeeId: string, documentId: string): Promise<void>
}
