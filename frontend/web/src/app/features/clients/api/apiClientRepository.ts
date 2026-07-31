import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import type { Client, ClientInput, ClientListFilters, ClientRepository } from './clientRepository'

export const apiClientRepository: ClientRepository = {
  async list(filters?: ClientListFilters): Promise<PagedResult<Client>> {
    const response = await httpClient.get<PagedResult<Client>>('/clients', { params: filters })
    return response.data
  },

  async getById(id: string): Promise<Client> {
    const response = await httpClient.get<{ data: Client }>(`/clients/${id}`)
    return unwrap(response)
  },

  async create(input: ClientInput): Promise<Client> {
    const response = await httpClient.post<{ data: Client }>('/clients', input)
    return unwrap(response)
  },

  async update(id: string, input: ClientInput): Promise<Client> {
    const response = await httpClient.put<{ data: Client }>(`/clients/${id}`, { id, ...input })
    return unwrap(response)
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/clients/${id}`)
  },

  async activate(id: string): Promise<void> {
    await httpClient.post(`/clients/${id}/activate`)
  },

  async deactivate(id: string): Promise<void> {
    await httpClient.post(`/clients/${id}/deactivate`)
  },

  async archive(id: string): Promise<void> {
    await httpClient.post(`/clients/${id}/archive`)
  },

  async restore(id: string): Promise<void> {
    await httpClient.post(`/clients/${id}/restore`)
  },
}
