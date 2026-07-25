import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type {
  CreateOfficeLocationInput,
  OfficeLocation,
  OfficeLocationRepository,
  UpdateOfficeLocationInput,
} from './officeLocationRepository'

export const apiOfficeLocationRepository: OfficeLocationRepository = {
  async list(): Promise<OfficeLocation[]> {
    const response = await httpClient.get<{ data: OfficeLocation[] }>('/office-locations')
    return unwrap(response)
  },

  async getById(id: string): Promise<OfficeLocation> {
    const response = await httpClient.get<{ data: OfficeLocation }>(`/office-locations/${id}`)
    return unwrap(response)
  },

  async create(input: CreateOfficeLocationInput): Promise<OfficeLocation> {
    const response = await httpClient.post<{ data: OfficeLocation }>('/office-locations', input)
    return unwrap(response)
  },

  async update(input: UpdateOfficeLocationInput): Promise<OfficeLocation> {
    const response = await httpClient.put<{ data: OfficeLocation }>(`/office-locations/${input.id}`, input)
    return unwrap(response)
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/office-locations/${id}`)
  },
}
