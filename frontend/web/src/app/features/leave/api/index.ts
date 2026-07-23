import { selectRepository } from '@/app/core/config/selectRepository'
import { mockLeaveRepository } from './mockLeaveRepository'
import { apiLeaveRepository } from './apiLeaveRepository'
import type { LeaveRepository } from './leaveRepository'

export const leaveRepository: LeaveRepository = selectRepository({
  mock: mockLeaveRepository,
  api: apiLeaveRepository,
})

export type {
  LeaveRequest,
  LeaveRepository,
  LeaveStatus,
  LeaveListFilters,
  ApplyLeaveInput,
  LeaveBalance,
  AdjustLeaveBalanceInput,
} from './leaveRepository'
