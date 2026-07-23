import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import type {
  AdjustLeaveBalanceInput,
  ApplyLeaveInput,
  LeaveBalance,
  LeaveListFilters,
  LeaveRepository,
  LeaveRequest,
} from './leaveRepository'

export const apiLeaveRepository: LeaveRepository = {
  async list(filters?: LeaveListFilters): Promise<PagedResult<LeaveRequest>> {
    const response = await httpClient.get<PagedResult<LeaveRequest>>('/leave/requests', {
      params: filters,
    })
    return response.data
  },

  async getById(id: string): Promise<LeaveRequest> {
    const response = await httpClient.get<{ data: LeaveRequest }>(`/leave/requests/${id}`)
    return unwrap(response)
  },

  async apply(input: ApplyLeaveInput): Promise<LeaveRequest> {
    const response = await httpClient.post<{ data: LeaveRequest }>('/leave/requests', input)
    return unwrap(response)
  },

  async approve(id: string, comments?: string): Promise<void> {
    await httpClient.post(`/leave/requests/${id}/approve`, { comments })
  },

  async reject(id: string, comments?: string): Promise<void> {
    await httpClient.post(`/leave/requests/${id}/reject`, { comments })
  },

  async cancel(id: string): Promise<void> {
    await httpClient.post(`/leave/requests/${id}/cancel`)
  },

  async getBalances(employeeId: string): Promise<LeaveBalance[]> {
    const response = await httpClient.get<{ data: LeaveBalance[] }>('/leave/balances', {
      params: { employeeId },
    })
    return unwrap(response)
  },

  async adjustBalance(input: AdjustLeaveBalanceInput): Promise<LeaveBalance> {
    const response = await httpClient.put<{ data: LeaveBalance }>(
      `/employees/${input.employeeId}/leave-balances/${input.leaveTypeId}`,
      { year: input.year, adjusted: input.adjusted, reason: input.reason },
    )
    return unwrap(response)
  },
}
