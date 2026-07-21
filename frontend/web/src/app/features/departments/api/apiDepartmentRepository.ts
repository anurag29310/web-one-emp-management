import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type {
  CreateDepartmentInput,
  Department,
  DepartmentRepository,
  UpdateDepartmentInput,
} from './departmentRepository'

export const apiDepartmentRepository: DepartmentRepository = {
  async list(): Promise<Department[]> {
    const response = await httpClient.get<{ data: Department[] }>('/departments')
    return unwrap(response)
  },

  async getById(id: string): Promise<Department> {
    const response = await httpClient.get<{ data: Department }>(`/departments/${id}`)
    return unwrap(response)
  },

  async create(input: CreateDepartmentInput): Promise<Department> {
    const response = await httpClient.post<{ data: Department }>('/departments', input)
    return unwrap(response)
  },

  async update(input: UpdateDepartmentInput): Promise<Department> {
    const response = await httpClient.put<{ data: Department }>(`/departments/${input.id}`, input)
    return unwrap(response)
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/departments/${id}`)
  },

  async restore(id: string): Promise<void> {
    await httpClient.post(`/departments/${id}/restore`)
  },
}
