import { selectRepository } from '@/app/core/config/selectRepository'
import { mockLeaveTypeRepository } from './mockLeaveTypeRepository'
import { apiLeaveTypeRepository } from './apiLeaveTypeRepository'
import type { LeaveTypeRepository } from './leaveTypeRepository'

export const leaveTypeRepository: LeaveTypeRepository = selectRepository({
  mock: mockLeaveTypeRepository,
  api: apiLeaveTypeRepository,
})

export type {
  LeaveType,
  LeaveTypeRepository,
  CreateLeaveTypeInput,
  UpdateLeaveTypeInput,
} from './leaveTypeRepository'
