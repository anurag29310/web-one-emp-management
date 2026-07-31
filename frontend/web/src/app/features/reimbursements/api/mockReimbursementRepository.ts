import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import type {
  Reimbursement,
  ReimbursementAttachment,
  ReimbursementInput,
  ReimbursementListFilters,
  ReimbursementRepository,
} from './reimbursementRepository'
import { mockReimbursementAttachments, mockReimbursements } from './mockData'

let reimbursements = [...mockReimbursements]
let attachments = [...mockReimbursementAttachments]

// The mock repository has no auth context (it's a plain object, not a hook), so — same as the
// real backend deriving RequestingUserId from the JWT — new claims are always attributed to a
// single fixed mock claimant rather than whichever role is "logged in".
const MOCK_CLAIMANT_ID = '10000000-0000-0000-0000-000000000001' // Ava Patel

const EDITABLE_STATUSES = ['Draft', 'ChangesRequested'] as const
const ATTACHABLE_STATUSES: Reimbursement['status'][] = ['Draft', 'Submitted', 'UnderReview', 'ChangesRequested']

function nextId(): string {
  return `00000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

function nextReimbursementNumber(): string {
  return `REI-${Date.now().toString(16).toUpperCase().slice(-8)}`
}

function findOrThrow(id: string): Reimbursement {
  const found = reimbursements.find((r) => r.id === id)
  if (!found) {
    throw new AppError(`Reimbursement ${id} was not found.`, 404, 'NOT_FOUND')
  }
  return found
}

function assertStatus(reimbursement: Reimbursement, expected: readonly Reimbursement['status'][], action: string): void {
  if (!expected.includes(reimbursement.status)) {
    throw new AppError(
      `${reimbursement.reimbursementNumber} must be ${expected.join(' or ')} to ${action} (currently ${reimbursement.status}).`,
      409,
      'INVALID_STATUS',
    )
  }
}

function computeAmount(input: ReimbursementInput): { amount: number; mileageRatePerKm: number | null } {
  if (input.distanceKm) {
    const rate = 0.55 // mirrors the mock value used for the seeded mileage claim
    return { amount: Math.round(input.distanceKm * rate * 100) / 100, mileageRatePerKm: rate }
  }
  return { amount: input.amount, mileageRatePerKm: null }
}

export const mockReimbursementRepository: ReimbursementRepository = {
  async list(filters: ReimbursementListFilters = {}): Promise<PagedResult<Reimbursement>> {
    await delay(300)
    const { page = 1, pageSize = 20, employeeId, status } = filters

    let filtered = [...reimbursements]
    if (employeeId) filtered = filtered.filter((r) => r.employeeId === employeeId)
    if (status) filtered = filtered.filter((r) => r.status === status)

    filtered = filtered.sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc))

    const start = (page - 1) * pageSize
    const pageItems = filtered.slice(start, start + pageSize)

    return {
      data: pageItems,
      page,
      pageSize,
      totalCount: filtered.length,
      totalPages: Math.max(1, Math.ceil(filtered.length / pageSize)),
      correlationId: 'mock-correlation-id',
    }
  },

  async getById(id: string): Promise<Reimbursement> {
    await delay(200)
    return findOrThrow(id)
  },

  async create(input: ReimbursementInput): Promise<Reimbursement> {
    await delay(300)
    const { amount, mileageRatePerKm } = computeAmount(input)
    const now = new Date().toISOString()
    const created: Reimbursement = {
      id: nextId(),
      reimbursementNumber: nextReimbursementNumber(),
      employeeId: MOCK_CLAIMANT_ID,
      employeeName: 'Ava Patel',
      expenseTitle: input.expenseTitle,
      expenseCategory: input.expenseCategory,
      expenseDate: input.expenseDate,
      amount,
      currency: input.currency ?? 'USD',
      description: input.description ?? null,
      notes: input.notes ?? null,
      distanceKm: input.distanceKm ?? null,
      mileageRatePerKm,
      status: 'Draft',
      submittedAtUtc: null,
      approvedAtUtc: null,
      approvedBy: null,
      reviewRemarks: null,
      payrollProcessed: false,
      payrollRunId: null,
      payrollDate: null,
      createdAtUtc: now,
      updatedAtUtc: null,
    }
    reimbursements = [created, ...reimbursements]
    return created
  },

  async update(id: string, input: ReimbursementInput): Promise<Reimbursement> {
    await delay(300)
    const existing = findOrThrow(id)
    assertStatus(existing, EDITABLE_STATUSES, 'edit')
    const { amount, mileageRatePerKm } = computeAmount(input)
    const updated: Reimbursement = {
      ...existing,
      expenseTitle: input.expenseTitle,
      expenseCategory: input.expenseCategory,
      expenseDate: input.expenseDate,
      amount,
      currency: input.currency ?? existing.currency,
      description: input.description ?? null,
      notes: input.notes ?? null,
      distanceKm: input.distanceKm ?? null,
      mileageRatePerKm,
      updatedAtUtc: new Date().toISOString(),
    }
    reimbursements = reimbursements.map((r) => (r.id === id ? updated : r))
    return updated
  },

  async remove(id: string): Promise<void> {
    await delay(200)
    const existing = findOrThrow(id)
    assertStatus(existing, ['Draft'], 'delete')
    reimbursements = reimbursements.filter((r) => r.id !== id)
  },

  async submit(id: string): Promise<void> {
    await delay(200)
    const existing = findOrThrow(id)
    assertStatus(existing, EDITABLE_STATUSES, 'submit')
    const now = new Date().toISOString()
    reimbursements = reimbursements.map((r) =>
      r.id === id ? { ...r, status: 'Submitted', submittedAtUtc: now, reviewRemarks: null, updatedAtUtc: now } : r,
    )
  },

  async startReview(id: string): Promise<void> {
    await delay(200)
    const existing = findOrThrow(id)
    assertStatus(existing, ['Submitted'], 'start review')
    reimbursements = reimbursements.map((r) =>
      r.id === id ? { ...r, status: 'UnderReview', updatedAtUtc: new Date().toISOString() } : r,
    )
  },

  async approve(id: string): Promise<void> {
    await delay(200)
    const existing = findOrThrow(id)
    assertStatus(existing, ['UnderReview'], 'approve')
    const now = new Date().toISOString()
    reimbursements = reimbursements.map((r) =>
      r.id === id
        ? { ...r, status: 'Approved', approvedAtUtc: now, approvedBy: '00000000-0000-0000-0000-000000000001', updatedAtUtc: now }
        : r,
    )
  },

  async reject(id: string, remarks: string): Promise<void> {
    await delay(200)
    const existing = findOrThrow(id)
    assertStatus(existing, ['UnderReview'], 'reject')
    reimbursements = reimbursements.map((r) =>
      r.id === id ? { ...r, status: 'Rejected', reviewRemarks: remarks, updatedAtUtc: new Date().toISOString() } : r,
    )
  },

  async requestChanges(id: string, remarks: string): Promise<void> {
    await delay(200)
    const existing = findOrThrow(id)
    assertStatus(existing, ['UnderReview'], 'request changes on')
    reimbursements = reimbursements.map((r) =>
      r.id === id ? { ...r, status: 'ChangesRequested', reviewRemarks: remarks, updatedAtUtc: new Date().toISOString() } : r,
    )
  },

  async getAttachments(id: string): Promise<ReimbursementAttachment[]> {
    await delay(250)
    return attachments.filter((a) => a.reimbursementId === id).sort((a, b) => b.uploadedAtUtc.localeCompare(a.uploadedAtUtc))
  },

  async uploadAttachment(id: string, file: File): Promise<{ id: string }> {
    await delay(300)
    const reimbursement = findOrThrow(id)
    assertStatus(reimbursement, ATTACHABLE_STATUSES, 'attach a document to')
    const created: ReimbursementAttachment = {
      id: nextId(),
      reimbursementId: id,
      originalFileName: file.name,
      contentType: file.type || 'application/octet-stream',
      fileSizeBytes: file.size,
      uploadedAtUtc: new Date().toISOString(),
      uploadedBy: null,
    }
    attachments = [created, ...attachments]
    return { id: created.id }
  },

  async downloadAttachment(attachmentId: string): Promise<{ blob: Blob; fileName: string }> {
    await delay(200)
    const attachment = attachments.find((a) => a.id === attachmentId)
    if (!attachment) {
      throw new AppError(`Attachment ${attachmentId} was not found.`, 404, 'NOT_FOUND')
    }
    return {
      blob: new Blob(['Mock attachment content.'], { type: attachment.contentType }),
      fileName: attachment.originalFileName,
    }
  },
}
