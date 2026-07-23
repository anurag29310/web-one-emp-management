import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type {
  CreateLeaveTypeInput,
  LeaveType,
  LeaveTypeRepository,
  UpdateLeaveTypeInput,
} from './leaveTypeRepository'

export const apiLeaveTypeRepository: LeaveTypeRepository = {
  async list(): Promise<LeaveType[]> {
    const response = await httpClient.get<{ data: LeaveType[] }>('/leave-types')
    return unwrap(response)
  },

  async getById(id: string): Promise<LeaveType> {
    const response = await httpClient.get<{ data: LeaveType }>(`/leave-types/${id}`)
    return unwrap(response)
  },

  async create(input: CreateLeaveTypeInput): Promise<LeaveType> {
    const response = await httpClient.post<{ data: LeaveType }>('/leave-types', input)
    return unwrap(response)
  },

  async update(input: UpdateLeaveTypeInput): Promise<LeaveType> {
    const response = await httpClient.put<{ data: LeaveType }>(`/leave-types/${input.id}`, input)
    return unwrap(response)
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/leave-types/${id}`)
  },

  async restore(id: string): Promise<void> {
    await httpClient.post(`/leave-types/${id}/restore`)
  },
}
