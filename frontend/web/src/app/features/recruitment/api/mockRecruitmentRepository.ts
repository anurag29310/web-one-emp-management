import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import { mockDesignations } from '@/app/features/designations/api/mockData'
import { mockDepartments } from '@/app/features/departments/api/mockData'
import { mockEmployees } from '@/app/features/employees/api/mockData'
import type {
  AddChecklistItemInput,
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
} from './recruitmentRepository'
import { mockChecklistItems, mockInterviews, mockOffers, mockCandidates } from './mockData'

let candidates = [...mockCandidates]
let interviews = [...mockInterviews]
let offers = [...mockOffers]
let checklistItems = [...mockChecklistItems]

interface MockCandidateAttachment extends CandidateAttachment {
  candidateId: string
}
const attachments: MockCandidateAttachment[] = []

const TERMINAL_STATUSES: Candidate['status'][] = ['Rejected', 'Withdrawn', 'Hired']
const DEFAULT_CHECKLIST_ITEMS = [
  'Offer Letter Signed',
  'ID Proof Submitted',
  'Bank Details Collected',
  'Laptop/Asset Allocated',
  'Induction Completed',
]

function nextId(): string {
  return `00000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

function nextNumber(prefix: string): string {
  return `${prefix}-${Date.now().toString(16).toUpperCase().slice(-8)}`
}

function findCandidateOrThrow(id: string): Candidate {
  const found = candidates.find((c) => c.id === id)
  if (!found) throw new AppError(`Candidate ${id} was not found.`, 404, 'NOT_FOUND')
  return found
}

function assertNotTerminal(candidate: Candidate): void {
  if (TERMINAL_STATUSES.includes(candidate.status)) {
    throw new AppError(`${candidate.candidateNumber} is ${candidate.status} and cannot be modified further.`, 409, 'CANDIDATE_TERMINAL')
  }
}

function resolveDesignationName(id: string): string | null {
  return mockDesignations.find((d) => d.id === id)?.name ?? null
}
function resolveDepartmentName(id: string | undefined): string | null {
  return id ? mockDepartments.find((d) => d.id === id)?.name ?? null : null
}
function resolveEmployeeName(id: string): string | null {
  return mockEmployees.find((e) => e.id === id)?.fullName ?? null
}

function findInterviewOrThrow(id: string): Interview {
  const found = interviews.find((i) => i.id === id)
  if (!found) throw new AppError(`Interview ${id} was not found.`, 404, 'NOT_FOUND')
  return found
}
function findOfferOrThrow(id: string): Offer {
  const found = offers.find((o) => o.id === id)
  if (!found) throw new AppError(`Offer ${id} was not found.`, 404, 'NOT_FOUND')
  return found
}
function findChecklistItemOrThrow(id: string): ChecklistItem {
  const found = checklistItems.find((i) => i.id === id)
  if (!found) throw new AppError(`Checklist item ${id} was not found.`, 404, 'NOT_FOUND')
  return found
}

export const mockRecruitmentRepository: RecruitmentRepository = {
  async listCandidates(filters: CandidateListFilters = {}): Promise<PagedResult<Candidate>> {
    await delay(300)
    const { page = 1, pageSize = 20, status, designationId, search } = filters

    let filtered = candidates.filter((c) => !c.isDeleted)
    if (status) filtered = filtered.filter((c) => c.status === status)
    if (designationId) filtered = filtered.filter((c) => c.designationId === designationId)
    if (search) {
      const term = search.toLowerCase()
      filtered = filtered.filter(
        (c) =>
          c.firstName.toLowerCase().includes(term) ||
          c.lastName.toLowerCase().includes(term) ||
          c.email.toLowerCase().includes(term),
      )
    }

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

  async getCandidateById(id: string): Promise<Candidate> {
    await delay(200)
    return findCandidateOrThrow(id)
  },

  async createCandidate(input: CreateCandidateInput): Promise<{ id: string }> {
    await delay(300)
    const now = new Date().toISOString()
    const created: Candidate = {
      id: nextId(),
      candidateNumber: nextNumber('CAN'),
      firstName: input.firstName,
      lastName: input.lastName,
      email: input.email,
      phoneNumber: input.phoneNumber ?? null,
      designationId: input.designationId,
      designationName: resolveDesignationName(input.designationId),
      departmentId: input.departmentId ?? null,
      departmentName: resolveDepartmentName(input.departmentId),
      source: input.source ?? null,
      appliedDate: input.appliedDate,
      status: 'Applied',
      notes: input.notes ?? null,
      convertedEmployeeId: null,
      isDeleted: false,
      createdAtUtc: now,
      updatedAtUtc: null,
    }
    candidates = [created, ...candidates]
    return { id: created.id }
  },

  async updateCandidate(id: string, input: UpdateCandidateInput): Promise<void> {
    await delay(300)
    const existing = findCandidateOrThrow(id)
    assertNotTerminal(existing)
    candidates = candidates.map((c) =>
      c.id === id
        ? {
            ...c,
            firstName: input.firstName,
            lastName: input.lastName,
            email: input.email,
            phoneNumber: input.phoneNumber ?? null,
            designationId: input.designationId,
            designationName: resolveDesignationName(input.designationId),
            departmentId: input.departmentId ?? null,
            departmentName: resolveDepartmentName(input.departmentId),
            source: input.source ?? null,
            notes: input.notes ?? null,
            updatedAtUtc: new Date().toISOString(),
          }
        : c,
    )
  },

  async deleteCandidate(id: string): Promise<void> {
    await delay(200)
    findCandidateOrThrow(id)
    candidates = candidates.map((c) => (c.id === id ? { ...c, isDeleted: true } : c))
  },

  async restoreCandidate(id: string): Promise<void> {
    await delay(200)
    findCandidateOrThrow(id)
    candidates = candidates.map((c) => (c.id === id ? { ...c, isDeleted: false } : c))
  },

  async rejectCandidate(id: string, reason?: string): Promise<void> {
    await delay(200)
    const existing = findCandidateOrThrow(id)
    assertNotTerminal(existing)
    candidates = candidates.map((c) =>
      c.id === id
        ? {
            ...c,
            status: 'Rejected',
            notes: reason ? [c.notes, `Rejected: ${reason}`].filter(Boolean).join(' | ') : c.notes,
            updatedAtUtc: new Date().toISOString(),
          }
        : c,
    )
  },

  async withdrawCandidate(id: string, reason?: string): Promise<void> {
    await delay(200)
    const existing = findCandidateOrThrow(id)
    assertNotTerminal(existing)
    candidates = candidates.map((c) =>
      c.id === id
        ? {
            ...c,
            status: 'Withdrawn',
            notes: reason ? [c.notes, `Withdrawn: ${reason}`].filter(Boolean).join(' | ') : c.notes,
            updatedAtUtc: new Date().toISOString(),
          }
        : c,
    )
  },

  async getCandidateAttachments(candidateId: string): Promise<CandidateAttachment[]> {
    await delay(250)
    return attachments.filter((a) => a.candidateId === candidateId)
  },

  async uploadCandidateAttachment(candidateId: string, file: File): Promise<{ id: string }> {
    await delay(300)
    findCandidateOrThrow(candidateId)
    const created: MockCandidateAttachment = {
      id: nextId(),
      candidateId,
      originalFileName: file.name,
      contentType: file.type || 'application/octet-stream',
      fileSizeBytes: file.size,
      uploadedAtUtc: new Date().toISOString(),
    }
    attachments.push(created)
    return { id: created.id }
  },

  async downloadCandidateAttachment(attachmentId: string): Promise<FileDownload> {
    await delay(200)
    const attachment = attachments.find((a) => a.id === attachmentId)
    if (!attachment) throw new AppError(`Attachment ${attachmentId} was not found.`, 404, 'NOT_FOUND')
    return { blob: new Blob(['Mock attachment content.'], { type: attachment.contentType }), fileName: attachment.originalFileName }
  },

  async getInterviews(candidateId: string): Promise<Interview[]> {
    await delay(250)
    return interviews.filter((i) => i.candidateId === candidateId).sort((a, b) => b.scheduledAtUtc.localeCompare(a.scheduledAtUtc))
  },

  async scheduleInterview(candidateId: string, input: ScheduleInterviewInput): Promise<{ id: string }> {
    await delay(300)
    const candidate = findCandidateOrThrow(candidateId)
    assertNotTerminal(candidate)
    const created: Interview = {
      id: nextId(),
      candidateId,
      interviewerEmployeeId: input.interviewerEmployeeId,
      interviewerName: resolveEmployeeName(input.interviewerEmployeeId),
      round: input.round,
      mode: input.mode,
      scheduledAtUtc: input.scheduledAtUtc,
      durationMinutes: input.durationMinutes ?? null,
      status: 'Scheduled',
      feedback: null,
      rating: null,
      outcome: 'Pending',
      createdAtUtc: new Date().toISOString(),
    }
    interviews = [created, ...interviews]
    if (candidate.status === 'Applied' || candidate.status === 'Screening') {
      candidates = candidates.map((c) => (c.id === candidateId ? { ...c, status: 'Interviewing', updatedAtUtc: created.createdAtUtc } : c))
    }
    return { id: created.id }
  },

  async rescheduleInterview(id: string, input: RescheduleInterviewInput): Promise<void> {
    await delay(200)
    const existing = findInterviewOrThrow(id)
    if (existing.status !== 'Scheduled') {
      throw new AppError('Only a Scheduled interview can be rescheduled.', 409, 'INVALID_STATUS')
    }
    interviews = interviews.map((i) =>
      i.id === id ? { ...i, scheduledAtUtc: input.scheduledAtUtc, durationMinutes: input.durationMinutes ?? i.durationMinutes } : i,
    )
  },

  async cancelInterview(id: string): Promise<void> {
    await delay(200)
    const existing = findInterviewOrThrow(id)
    if (existing.status !== 'Scheduled') {
      throw new AppError('Only a Scheduled interview can be cancelled.', 409, 'INVALID_STATUS')
    }
    interviews = interviews.map((i) => (i.id === id ? { ...i, status: 'Cancelled' } : i))
  },

  async markInterviewNoShow(id: string): Promise<void> {
    await delay(200)
    const existing = findInterviewOrThrow(id)
    if (existing.status !== 'Scheduled') {
      throw new AppError('Only a Scheduled interview can be marked as a no-show.', 409, 'INVALID_STATUS')
    }
    interviews = interviews.map((i) => (i.id === id ? { ...i, status: 'NoShow' } : i))
  },

  async submitInterviewFeedback(id: string, input: SubmitInterviewFeedbackInput): Promise<void> {
    await delay(300)
    const existing = findInterviewOrThrow(id)
    if (existing.status !== 'Scheduled') {
      throw new AppError('Feedback can only be submitted for a Scheduled interview.', 409, 'INVALID_STATUS')
    }
    interviews = interviews.map((i) =>
      i.id === id
        ? { ...i, status: 'Completed', feedback: input.feedback, rating: input.rating, outcome: input.outcome }
        : i,
    )
  },

  async getOffers(candidateId: string): Promise<Offer[]> {
    await delay(250)
    return offers.filter((o) => o.candidateId === candidateId).sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc))
  },

  async createOffer(candidateId: string, input: CreateOfferInput): Promise<{ id: string }> {
    await delay(300)
    const candidate = findCandidateOrThrow(candidateId)
    assertNotTerminal(candidate)
    const created: Offer = {
      id: nextId(),
      offerNumber: nextNumber('OFR'),
      candidateId,
      designationId: input.designationId,
      designationName: resolveDesignationName(input.designationId),
      departmentId: input.departmentId ?? null,
      departmentName: resolveDepartmentName(input.departmentId),
      offeredSalary: input.offeredSalary,
      joiningDate: input.joiningDate,
      status: 'Draft',
      issuedAtUtc: null,
      respondedAtUtc: null,
      expiresAtUtc: input.expiresAtUtc ?? null,
      notes: input.notes ?? null,
      hasDocument: false,
      createdAtUtc: new Date().toISOString(),
    }
    offers = [created, ...offers]
    return { id: created.id }
  },

  async sendOffer(id: string): Promise<void> {
    await delay(300)
    const existing = findOfferOrThrow(id)
    if (existing.status !== 'Draft') {
      throw new AppError('Only a Draft offer can be sent.', 409, 'INVALID_STATUS')
    }
    const now = new Date().toISOString()
    offers = offers.map((o) => (o.id === id ? { ...o, status: 'Sent', issuedAtUtc: now, hasDocument: true } : o))
    candidates = candidates.map((c) => (c.id === existing.candidateId ? { ...c, status: 'Offered', updatedAtUtc: now } : c))
  },

  async acceptOffer(id: string): Promise<void> {
    await delay(300)
    const existing = findOfferOrThrow(id)
    if (existing.status !== 'Sent') {
      throw new AppError('Only a Sent offer can be accepted.', 409, 'INVALID_STATUS')
    }
    const now = new Date().toISOString()
    offers = offers.map((o) => (o.id === id ? { ...o, status: 'Accepted', respondedAtUtc: now } : o))

    const alreadySeeded = checklistItems.some((item) => item.candidateId === existing.candidateId)
    if (!alreadySeeded) {
      const seeded = DEFAULT_CHECKLIST_ITEMS.map((itemName) => ({
        id: nextId(),
        candidateId: existing.candidateId,
        itemName,
        isCompleted: false,
        completedAtUtc: null,
        notes: null,
        createdAtUtc: now,
      }))
      checklistItems = [...checklistItems, ...seeded]
    }
  },

  async rejectOffer(id: string, reason?: string): Promise<void> {
    await delay(300)
    const existing = findOfferOrThrow(id)
    if (existing.status !== 'Sent') {
      throw new AppError('Only a Sent offer can be rejected.', 409, 'INVALID_STATUS')
    }
    const now = new Date().toISOString()
    offers = offers.map((o) =>
      o.id === id ? { ...o, status: 'Rejected', respondedAtUtc: now, notes: reason ? [o.notes, reason].filter(Boolean).join(' | ') : o.notes } : o,
    )
  },

  async withdrawOffer(id: string): Promise<void> {
    await delay(200)
    const existing = findOfferOrThrow(id)
    if (existing.status !== 'Draft' && existing.status !== 'Sent') {
      throw new AppError('Only a Draft or Sent offer can be withdrawn.', 409, 'INVALID_STATUS')
    }
    offers = offers.map((o) => (o.id === id ? { ...o, status: 'Withdrawn' } : o))
  },

  async downloadOffer(id: string): Promise<FileDownload> {
    await delay(200)
    const existing = findOfferOrThrow(id)
    if (!existing.hasDocument) {
      throw new AppError('This offer has no letter to download yet.', 404, 'NOT_FOUND')
    }
    return { blob: new Blob(['Mock offer letter content.'], { type: 'application/pdf' }), fileName: `${existing.offerNumber}.pdf` }
  },

  async getChecklist(candidateId: string): Promise<ChecklistItem[]> {
    await delay(250)
    return checklistItems.filter((i) => i.candidateId === candidateId).sort((a, b) => a.createdAtUtc.localeCompare(b.createdAtUtc))
  },

  async addChecklistItem(candidateId: string, input: AddChecklistItemInput): Promise<{ id: string }> {
    await delay(300)
    findCandidateOrThrow(candidateId)
    const created: ChecklistItem = {
      id: nextId(),
      candidateId,
      itemName: input.itemName,
      isCompleted: false,
      completedAtUtc: null,
      notes: input.notes ?? null,
      createdAtUtc: new Date().toISOString(),
    }
    checklistItems = [...checklistItems, created]
    return { id: created.id }
  },

  async completeChecklistItem(itemId: string, notes?: string): Promise<void> {
    await delay(200)
    findChecklistItemOrThrow(itemId)
    const now = new Date().toISOString()
    checklistItems = checklistItems.map((i) =>
      i.id === itemId ? { ...i, isCompleted: true, completedAtUtc: now, notes: notes ?? i.notes } : i,
    )
  },

  async convertToEmployee(candidateId: string, input: ConvertToEmployeeInput): Promise<{ id: string }> {
    await delay(300)
    const candidate = findCandidateOrThrow(candidateId)
    if (candidate.status === 'Hired') {
      throw new AppError(`${candidate.candidateNumber} has already been converted to an employee.`, 409, 'ALREADY_CONVERTED')
    }
    const latestOffer = offers
      .filter((o) => o.candidateId === candidateId)
      .sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc))[0]
    if (!latestOffer || latestOffer.status !== 'Accepted') {
      throw new AppError(`${candidate.candidateNumber} has no Accepted offer to convert from.`, 409, 'NO_ACCEPTED_OFFER')
    }
    void input // employeeCode/officeLocationId/teamId/managerId/joinDate — accepted but not modeled by the mock Employees dataset
    const newEmployeeId = nextId()
    candidates = candidates.map((c) =>
      c.id === candidateId ? { ...c, status: 'Hired', convertedEmployeeId: newEmployeeId, updatedAtUtc: new Date().toISOString() } : c,
    )
    return { id: newEmployeeId }
  },
}
