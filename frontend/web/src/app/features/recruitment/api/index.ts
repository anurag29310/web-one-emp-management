import { selectRepository } from '@/app/core/config/selectRepository'
import { mockRecruitmentRepository } from './mockRecruitmentRepository'
import { apiRecruitmentRepository } from './apiRecruitmentRepository'
import type { RecruitmentRepository } from './recruitmentRepository'

export const recruitmentRepository: RecruitmentRepository = selectRepository({
  mock: mockRecruitmentRepository,
  api: apiRecruitmentRepository,
})

export type {
  AddChecklistItemInput,
  Candidate,
  CandidateAttachment,
  CandidateListFilters,
  CandidateStatus,
  ChecklistItem,
  ConvertToEmployeeInput,
  CreateCandidateInput,
  CreateOfferInput,
  FileDownload,
  Interview,
  InterviewMode,
  InterviewOutcome,
  InterviewStatus,
  Offer,
  OfferStatus,
  RecruitmentRepository,
  RescheduleInterviewInput,
  ScheduleInterviewInput,
  SubmitInterviewFeedbackInput,
  UpdateCandidateInput,
} from './recruitmentRepository'
