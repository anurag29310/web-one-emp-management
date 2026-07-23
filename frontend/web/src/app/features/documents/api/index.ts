import { selectRepository } from '@/app/core/config/selectRepository'
import { mockDocumentRepository } from './mockDocumentRepository'
import { apiDocumentRepository } from './apiDocumentRepository'
import type { DocumentRepository } from './documentRepository'

export const documentRepository: DocumentRepository = selectRepository({
  mock: mockDocumentRepository,
  api: apiDocumentRepository,
})

export type {
  DocumentDownload,
  DocumentListFilters,
  DocumentRepository,
  EmployeeDocument,
  UploadDocumentInput,
} from './documentRepository'
