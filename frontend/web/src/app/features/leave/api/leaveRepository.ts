import type { PagedResult } from '@/app/shared/models/apiEnvelope'

export type LeaveStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled'

export interface LeaveRequest {
  id: string
  employeeId: string
  leaveTypeId: string
  approverEmployeeId: string | null
  startDate: string
  endDate: string
  totalDays: number
  reason: string | null
  status: LeaveStatus
  createdAtUtc: string
  decisionAtUtc: string | null
  decisionComments: string | null
}

export interface LeaveListFilters {
  page?: number
  pageSize?: number
  employeeId?: string
  leaveTypeId?: string
  year?: number
  status?: LeaveStatus
}

export interface ApplyLeaveInput {
  employeeId: string
  leaveTypeId: string
  startDate: string
  endDate: string
  totalDays: number
  reason?: string
}

export interface LeaveBalance {
  id: string
  employeeId: string
  leaveTypeId: string
  year: number
  openingBalance: number
  accrued: number
  used: number
  adjusted: number
  available: number
}

export interface AdjustLeaveBalanceInput {
  employeeId: string
  leaveTypeId: string
  year: number
  adjusted: number
  reason?: string
}

export interface LeaveRepository {
  list(filters?: LeaveListFilters): Promise<PagedResult<LeaveRequest>>
  getById(id: string): Promise<LeaveRequest>
  apply(input: ApplyLeaveInput): Promise<LeaveRequest>
  approve(id: string, comments?: string): Promise<void>
  reject(id: string, comments?: string): Promise<void>
  cancel(id: string): Promise<void>
  getBalances(employeeId: string): Promise<LeaveBalance[]>
  adjustBalance(input: AdjustLeaveBalanceInput): Promise<LeaveBalance>
}
