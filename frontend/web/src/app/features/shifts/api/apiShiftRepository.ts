import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type {
  AssignEmployeeShiftInput,
  EmployeeShift,
  Shift,
  ShiftRepository,
  CreateShiftInput,
  UpdateEmployeeShiftInput,
  UpdateShiftInput,
} from './shiftRepository'

export const apiShiftRepository: ShiftRepository = {
  async list(): Promise<Shift[]> {
    const response = await httpClient.get<{ data: Shift[] }>('/shifts')
    return unwrap(response)
  },

  async getById(id: string): Promise<Shift> {
    const response = await httpClient.get<{ data: Shift }>(`/shifts/${id}`)
    return unwrap(response)
  },

  async create(input: CreateShiftInput): Promise<Shift> {
    const response = await httpClient.post<{ data: Shift }>('/shifts', input)
    return unwrap(response)
  },

  async update(input: UpdateShiftInput): Promise<Shift> {
    const { id, ...body } = input
    const response = await httpClient.put<{ data: Shift }>(`/shifts/${id}`, body)
    return unwrap(response)
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/shifts/${id}`)
  },

  async getEmployeeShifts(employeeId: string): Promise<EmployeeShift[]> {
    const response = await httpClient.get<{ data: EmployeeShift[] }>(`/employees/${employeeId}/shifts`)
    return unwrap(response)
  },

  async assignEmployeeShift(input: AssignEmployeeShiftInput): Promise<EmployeeShift> {
    const { employeeId, ...body } = input
    const response = await httpClient.post<{ data: EmployeeShift }>(`/employees/${employeeId}/shifts`, body)
    return unwrap(response)
  },

  async updateEmployeeShift(input: UpdateEmployeeShiftInput): Promise<EmployeeShift> {
    const { employeeId, assignmentId, ...body } = input
    const response = await httpClient.put<{ data: EmployeeShift }>(
      `/employees/${employeeId}/shifts/${assignmentId}`,
      body,
    )
    return unwrap(response)
  },

  async endEmployeeShift(employeeId: string, assignmentId: string): Promise<void> {
    await httpClient.delete(`/employees/${employeeId}/shifts/${assignmentId}`)
  },
}
