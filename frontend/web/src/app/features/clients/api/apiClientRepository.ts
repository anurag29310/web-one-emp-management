import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { ApiSuccessEnvelope, PagedResult } from '@/app/shared/models/apiEnvelope'
import type { Client, ClientInput, ClientListFilters, ClientRepository } from './clientRepository'

/**
 * Shape of EMS.Application.Common.DTOs.PagedResult<T> as it actually serializes: the backend
 * wraps it a second time inside ApiResponse<T>, so a list endpoint's JSON body is
 * `{ data: { data: [...], page, pageSize, totalCount, totalPages }, message, correlationId }`
 * — the pagination fields live one level deeper than the flat `{ data, page, pageSize, ... }`
 * shape documented in api-specification.md §2.3. Confirmed against
 * ClientController.GetAll, which returns `ApiResponse<PagedResult<ClientDto>>.Success(result)`
 * — same pattern already used in attendance/audit-logs/performance repositories.
 */
interface BackendPagedResult<T> {
  data: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

function unwrapPaged<T>(response: { data: ApiSuccessEnvelope<BackendPagedResult<T>> }): PagedResult<T> {
  const envelope = response.data
  const paged = envelope.data
  return {
    data: paged.data,
    page: paged.page,
    pageSize: paged.pageSize,
    totalCount: paged.totalCount,
    totalPages: paged.totalPages,
    correlationId: envelope.correlationId,
  }
}

export const apiClientRepository: ClientRepository = {
  async list(filters?: ClientListFilters): Promise<PagedResult<Client>> {
    const response = await httpClient.get<ApiSuccessEnvelope<BackendPagedResult<Client>>>('/clients', {
      params: filters,
    })
    return unwrapPaged(response)
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
