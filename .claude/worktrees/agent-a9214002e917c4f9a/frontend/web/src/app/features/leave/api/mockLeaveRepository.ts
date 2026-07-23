import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import type { ApplyLeaveInput, LeaveListFilters, LeaveRepository, LeaveRequest } from './leaveRepository'
import { mockLeaveRequests } from './mockData'

let leaveRequests = [...mockLeaveRequests]

function nextId(): string {
  return `40000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

export const mockLeaveRepository: LeaveRepository = {
  async list(filters: LeaveListFilters = {}): Promise<PagedResult<LeaveRequest>> {
    await delay(300)
    const { page = 1, pageSize = 20, employeeId, leaveTypeId, status } = filters

    let filtered = leaveRequests
    if (employeeId) filtered = filtered.filter((r) => r.employeeId === employeeId)
    if (leaveTypeId) filtered = filtered.filter((r) => r.leaveTypeId === leaveTypeId)
    if (status) filtered = filtered.filter((r) => r.status === status)

    filtered = [...filtered].sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc))

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

  async getById(id: string): Promise<LeaveRequest> {
    await delay(200)
    const request = leaveRequests.find((r) => r.id === id)
    if (!request) {
      throw new AppError(`Leave request ${id} was not found.`, 404, 'NOT_FOUND')
    }
    return request
  },

  async apply(input: ApplyLeaveInput): Promise<LeaveRequest> {
    await delay(300)
    const request: LeaveRequest = {
      id: nextId(),
      employeeId: input.employeeId,
      leaveTypeId: input.leaveTypeId,
      approverEmployeeId: null,
      startDate: input.startDate,
      endDate: input.endDate,
      totalDays: input.totalDays,
      reason: input.reason ?? null,
      status: 'Pending',
      createdAtUtc: new Date().toISOString(),
      decisionAtUtc: null,
      decisionComments: null,
    }
    leaveRequests = [request, ...leaveRequests]
    return request
  },

  async approve(id: string, comments?: string): Promise<void> {
    await delay(200)
    leaveRequests = leaveRequests.map((r) =>
      r.id === id
        ? { ...r, status: 'Approved', decisionAtUtc: new Date().toISOString(), decisionComments: comments ?? null }
        : r,
    )
  },

  async reject(id: string, comments?: string): Promise<void> {
    await delay(200)
    leaveRequests = leaveRequests.map((r) =>
      r.id === id
        ? { ...r, status: 'Rejected', decisionAtUtc: new Date().toISOString(), decisionComments: comments ?? null }
        : r,
    )
  },

  async cancel(id: string): Promise<void> {
    await delay(200)
    leaveRequests = leaveRequests.map((r) => (r.id === id ? { ...r, status: 'Cancelled' } : r))
  },
}
