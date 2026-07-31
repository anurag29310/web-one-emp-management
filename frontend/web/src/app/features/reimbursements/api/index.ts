import { selectRepository } from '@/app/core/config/selectRepository'
import { mockReimbursementRepository } from './mockReimbursementRepository'
import { apiReimbursementRepository } from './apiReimbursementRepository'
import type { ReimbursementRepository } from './reimbursementRepository'

export const reimbursementRepository: ReimbursementRepository = selectRepository({
  mock: mockReimbursementRepository,
  api: apiReimbursementRepository,
})

export type {
  Reimbursement,
  ReimbursementAttachment,
  ReimbursementAttachmentDownload,
  ReimbursementInput,
  ReimbursementListFilters,
  ReimbursementRepository,
  ReimbursementStatus,
} from './reimbursementRepository'
