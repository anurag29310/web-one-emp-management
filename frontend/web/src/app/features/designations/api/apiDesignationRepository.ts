import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type {
  CreateDesignationInput,
  Designation,
  DesignationRepository,
  UpdateDesignationInput,
} from './designationRepository'

export const apiDesignationRepository: DesignationRepository = {
  async list(): Promise<Designation[]> {
    const response = await httpClient.get<{ data: Designation[] }>('/designations')
    return unwrap(response)
  },

  async getById(id: string): Promise<Designation> {
    const response = await httpClient.get<{ data: Designation }>(`/designations/${id}`)
    return unwrap(response)
  },

  async create(input: CreateDesignationInput): Promise<Designation> {
    const response = await httpClient.post<{ data: Designation }>('/designations', input)
    return unwrap(response)
  },

  async update(input: UpdateDesignationInput): Promise<Designation> {
    const response = await httpClient.put<{ data: Designation }>(`/designations/${input.id}`, input)
    return unwrap(response)
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/designations/${id}`)
  },
}
