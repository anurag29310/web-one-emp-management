import type { PagedResult } from '@/app/shared/models/apiEnvelope'

export type CandidateStatus = 'Applied' | 'Screening' | 'Interviewing' | 'Offered' | 'Hired' | 'Rejected' | 'Withdrawn'
export type InterviewMode = 'Onsite' | 'Phone' | 'VideoCall'
export type InterviewStatus = 'Scheduled' | 'Completed' | 'Cancelled' | 'NoShow'
export type InterviewOutcome = 'Pending' | 'Passed' | 'Failed' | 'OnHold'
export type OfferStatus = 'Draft' | 'Sent' | 'Accepted' | 'Rejected' | 'Withdrawn' | 'Expired'

export interface Candidate {
  id: string
  candidateNumber: string
  firstName: string
  lastName: string
  email: string
  phoneNumber: string | null
  designationId: string
  designationName: string | null
  departmentId: string | null
  departmentName: string | null
  source: string | null
  appliedDate: string
  status: CandidateStatus
  notes: string | null
  convertedEmployeeId: string | null
  isDeleted: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface CandidateListFilters {
  page?: number
  pageSize?: number
  status?: CandidateStatus
  designationId?: string
  search?: string
}

export interface CreateCandidateInput {
  firstName: string
  lastName: string
  email: string
  phoneNumber?: string
  designationId: string
  departmentId?: string
  source?: string
  appliedDate: string
  notes?: string
}

// Update has no appliedDate — it's set once at registration and never edited afterward.
export interface UpdateCandidateInput {
  firstName: string
  lastName: string
  email: string
  phoneNumber?: string
  designationId: string
  departmentId?: string
  source?: string
  notes?: string
}

export interface CandidateAttachment {
  id: string
  originalFileName: string
  contentType: string
  fileSizeBytes: number
  uploadedAtUtc: string
}

export interface Interview {
  id: string
  candidateId: string
  interviewerEmployeeId: string
  interviewerName: string | null
  round: string
  mode: InterviewMode
  scheduledAtUtc: string
  durationMinutes: number | null
  status: InterviewStatus
  feedback: string | null
  rating: number | null
  outcome: InterviewOutcome
  createdAtUtc: string
}

export interface ScheduleInterviewInput {
  interviewerEmployeeId: string
  round: string
  mode: InterviewMode
  scheduledAtUtc: string
  durationMinutes?: number
}

export interface RescheduleInterviewInput {
  scheduledAtUtc: string
  durationMinutes?: number
}

export interface SubmitInterviewFeedbackInput {
  feedback: string
  rating: number
  outcome: Exclude<InterviewOutcome, 'Pending'>
}

export interface Offer {
  id: string
  offerNumber: string
  candidateId: string
  designationId: string
  designationName: string | null
  departmentId: string | null
  departmentName: string | null
  offeredSalary: number
  joiningDate: string
  status: OfferStatus
  issuedAtUtc: string | null
  respondedAtUtc: string | null
  expiresAtUtc: string | null
  notes: string | null
  hasDocument: boolean
  createdAtUtc: string
}

export interface CreateOfferInput {
  designationId: string
  departmentId?: string
  offeredSalary: number
  joiningDate: string
  expiresAtUtc?: string
  notes?: string
}

export interface ChecklistItem {
  id: string
  candidateId: string
  itemName: string
  isCompleted: boolean
  completedAtUtc: string | null
  notes: string | null
  createdAtUtc: string
}

export interface AddChecklistItemInput {
  itemName: string
  notes?: string
}

export interface ConvertToEmployeeInput {
  employeeCode: string
  officeLocationId: string
  teamId?: string
  managerId?: string
  joinDate?: string
}

export interface FileDownload {
  blob: Blob
  fileName: string
}

export interface RecruitmentRepository {
  listCandidates(filters?: CandidateListFilters): Promise<PagedResult<Candidate>>
  getCandidateById(id: string): Promise<Candidate>
  createCandidate(input: CreateCandidateInput): Promise<{ id: string }>
  updateCandidate(id: string, input: UpdateCandidateInput): Promise<void>
  deleteCandidate(id: string): Promise<void>
  restoreCandidate(id: string): Promise<void>
  rejectCandidate(id: string, reason?: string): Promise<void>
  withdrawCandidate(id: string, reason?: string): Promise<void>

  getCandidateAttachments(candidateId: string): Promise<CandidateAttachment[]>
  uploadCandidateAttachment(candidateId: string, file: File): Promise<{ id: string }>
  downloadCandidateAttachment(attachmentId: string): Promise<FileDownload>

  getInterviews(candidateId: string): Promise<Interview[]>
  scheduleInterview(candidateId: string, input: ScheduleInterviewInput): Promise<{ id: string }>
  rescheduleInterview(id: string, input: RescheduleInterviewInput): Promise<void>
  cancelInterview(id: string): Promise<void>
  markInterviewNoShow(id: string): Promise<void>
  submitInterviewFeedback(id: string, input: SubmitInterviewFeedbackInput): Promise<void>

  getOffers(candidateId: string): Promise<Offer[]>
  createOffer(candidateId: string, input: CreateOfferInput): Promise<{ id: string }>
  sendOffer(id: string): Promise<void>
  acceptOffer(id: string): Promise<void>
  rejectOffer(id: string, reason?: string): Promise<void>
  withdrawOffer(id: string): Promise<void>
  downloadOffer(id: string): Promise<FileDownload>

  getChecklist(candidateId: string): Promise<ChecklistItem[]>
  addChecklistItem(candidateId: string, input: AddChecklistItemInput): Promise<{ id: string }>
  completeChecklistItem(itemId: string, notes?: string): Promise<void>

  convertToEmployee(candidateId: string, input: ConvertToEmployeeInput): Promise<{ id: string }>
}
