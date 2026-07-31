import type { PagedResult } from '@/app/shared/models/apiEnvelope'

export type ReimbursementStatus =
  | 'Draft'
  | 'Submitted'
  | 'UnderReview'
  | 'Approved'
  | 'Rejected'
  | 'ChangesRequested'
  | 'Paid'

export interface Reimbursement {
  id: string
  reimbursementNumber: string
  employeeId: string
  employeeName: string | null
  expenseTitle: string
  expenseCategory: string
  expenseDate: string
  amount: number
  currency: string
  description: string | null
  notes: string | null
  distanceKm: number | null
  mileageRatePerKm: number | null
  status: ReimbursementStatus
  submittedAtUtc: string | null
  approvedAtUtc: string | null
  approvedBy: string | null
  reviewRemarks: string | null
  payrollProcessed: boolean
  payrollRunId: string | null
  payrollDate: string | null
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface ReimbursementListFilters {
  page?: number
  pageSize?: number
  employeeId?: string
  status?: ReimbursementStatus
}

export interface ReimbursementInput {
  expenseTitle: string
  expenseCategory: string
  expenseDate: string
  amount: number
  currency?: string
  description?: string
  notes?: string
  distanceKm?: number
}

export interface ReimbursementAttachment {
  id: string
  reimbursementId: string
  originalFileName: string
  contentType: string
  fileSizeBytes: number
  uploadedAtUtc: string
  uploadedBy: string | null
}

export interface ReimbursementAttachmentDownload {
  blob: Blob
  fileName: string
}

export interface ReimbursementRepository {
  list(filters?: ReimbursementListFilters): Promise<PagedResult<Reimbursement>>
  getById(id: string): Promise<Reimbursement>
  create(input: ReimbursementInput): Promise<Reimbursement>
  update(id: string, input: ReimbursementInput): Promise<Reimbursement>
  remove(id: string): Promise<void>
  submit(id: string): Promise<void>
  startReview(id: string): Promise<void>
  approve(id: string): Promise<void>
  reject(id: string, remarks: string): Promise<void>
  requestChanges(id: string, remarks: string): Promise<void>
  getAttachments(id: string): Promise<ReimbursementAttachment[]>
  uploadAttachment(id: string, file: File): Promise<{ id: string }>
  downloadAttachment(attachmentId: string): Promise<ReimbursementAttachmentDownload>
}
