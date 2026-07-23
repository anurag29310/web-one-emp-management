import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { Employee, EmployeeListFilters } from '@/app/shared/models/employee'
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
}
