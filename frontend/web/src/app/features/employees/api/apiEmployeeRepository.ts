import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type {
  CreateEmployeeInput,
  Employee,
  EmployeeListFilters,
  UpdateEmployeeInput,
  UpdateEmployeeStatusInput,
} from '@/app/shared/models/employee'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import type { EmployeeRepository } from './employeeRepository'

export const apiEmployeeRepository: EmployeeRepository = {
  async list(filters?: EmployeeListFilters): Promise<PagedResult<Employee>> {
    const response = await httpClient.get<{ data: PagedResult<Employee> }>('/employees', {
      params: filters,
    })
    return unwrap(response)
  },

  async getById(id: string): Promise<Employee> {
    const response = await httpClient.get<{ data: Employee }>(`/employees/${id}`)
    return unwrap(response)
  },

  async create(input: CreateEmployeeInput): Promise<Employee> {
    const response = await httpClient.post<{ data: Employee }>('/employees', input)
    return unwrap(response)
  },

  async update(input: UpdateEmployeeInput): Promise<Employee> {
    const response = await httpClient.put<{ data: Employee }>(`/employees/${input.id}`, input)
    return unwrap(response)
  },

  async updateStatus(input: UpdateEmployeeStatusInput): Promise<void> {
    await httpClient.patch(`/employees/${input.id}/status`, {
      status: input.status,
      exitDate: input.exitDate,
      reason: input.reason,
    })
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/employees/${id}`)
  },

  async restore(id: string): Promise<void> {
    await httpClient.post(`/employees/${id}/restore`)
  },
}
