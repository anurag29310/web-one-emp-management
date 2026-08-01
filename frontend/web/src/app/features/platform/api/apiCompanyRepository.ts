import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { ApiSuccessEnvelope, PagedResult } from '@/app/shared/models/apiEnvelope'
import type {
  Company,
  CompanyCreateInput,
  CompanyDetail,
  CompanyListFilters,
  CompanyUpdateInput,
  PlatformCompanyRepository,
} from './companyRepository'

/**
 * The backend wraps EMS.Application.Common.DTOs.PagedResult<T> a second time inside
 * ApiResponse<T> (PlatformCompaniesController.GetAll -> ApiResponse<PagedResult<CompanyDto>>.Success),
 * so the list response body is `{ data: { data: [...], page, pageSize, totalCount, totalPages },
 * message, correlationId }` — same double-wrapping already documented in
 * features/clients/api/apiClientRepository.ts.
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

export const apiCompanyRepository: PlatformCompanyRepository = {
  async list(filters?: CompanyListFilters): Promise<PagedResult<Company>> {
    const response = await httpClient.get<ApiSuccessEnvelope<BackendPagedResult<Company>>>('/platform/companies', {
      params: filters,
    })
    return unwrapPaged(response)
  },

  async getById(id: string): Promise<CompanyDetail> {
    const response = await httpClient.get<{ data: CompanyDetail }>(`/platform/companies/${id}`)
    return unwrap(response)
  },

  async create(input: CompanyCreateInput): Promise<Company> {
    const response = await httpClient.post<{ data: Company }>('/platform/companies', input)
    return unwrap(response)
  },

  async update(id: string, input: CompanyUpdateInput): Promise<Company> {
    const response = await httpClient.put<{ data: Company }>(`/platform/companies/${id}`, { id, ...input })
    return unwrap(response)
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/platform/companies/${id}`)
  },

  async restore(id: string): Promise<void> {
    await httpClient.post(`/platform/companies/${id}/restore`)
  },

  async activate(id: string): Promise<void> {
    await httpClient.post(`/platform/companies/${id}/activate`)
  },

  async suspend(id: string, reason?: string): Promise<void> {
    await httpClient.post(`/platform/companies/${id}/suspend`, reason ? { reason } : undefined)
  },

  async approve(id: string): Promise<void> {
    await httpClient.post(`/platform/companies/${id}/approve`)
  },

  async reject(id: string, reason?: string): Promise<void> {
    await httpClient.post(`/platform/companies/${id}/reject`, reason ? { reason } : undefined)
  },

  async forceLogout(id: string): Promise<void> {
    await httpClient.post(`/platform/companies/${id}/force-logout`)
  },

  async resetAdminPassword(companyId: string, userId: string): Promise<string> {
    const response = await httpClient.post<{ data: string }>(
      `/platform/companies/${companyId}/admins/${userId}/reset-password`,
    )
    return unwrap(response)
  },
}
