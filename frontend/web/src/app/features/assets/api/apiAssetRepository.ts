import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { ApiSuccessEnvelope, PagedResult } from '@/app/shared/models/apiEnvelope'
import type {
  Asset,
  AssetAssignment,
  AssetInput,
  AssetListFilters,
  AssetRepository,
  AssetStatusChangeInput,
  AssignAssetInput,
  ReturnAssignmentInput,
} from './assetRepository'

/**
 * Shape of EMS.Application.Common.DTOs.PagedResult<T> as it actually serializes: the backend
 * wraps it a second time inside ApiResponse<T>, so a list endpoint's JSON body is
 * `{ data: { data: [...], page, pageSize, totalCount, totalPages }, message, correlationId }`
 * — the pagination fields live one level deeper than the flat `{ data, page, pageSize, ... }`
 * shape documented in api-specification.md §2.3. Confirmed against
 * AssetsController.GetAll, which returns `ApiResponse<PagedResult<AssetDto>>.Success(result)`
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

export const apiAssetRepository: AssetRepository = {
  async list(filters?: AssetListFilters): Promise<PagedResult<Asset>> {
    const response = await httpClient.get<ApiSuccessEnvelope<BackendPagedResult<Asset>>>('/assets', {
      params: filters,
    })
    return unwrapPaged(response)
  },

  async getById(id: string): Promise<Asset> {
    const response = await httpClient.get<{ data: Asset }>(`/assets/${id}`)
    return unwrap(response)
  },

  async create(input: AssetInput): Promise<Asset> {
    const response = await httpClient.post<{ data: Asset }>('/assets', input)
    return unwrap(response)
  },

  async update(id: string, input: AssetInput): Promise<Asset> {
    const response = await httpClient.put<{ data: Asset }>(`/assets/${id}`, input)
    return unwrap(response)
  },

  async remove(id: string): Promise<void> {
    await httpClient.delete(`/assets/${id}`)
  },

  async restore(id: string): Promise<void> {
    await httpClient.post(`/assets/${id}/restore`)
  },

  async changeStatus(id: string, input: AssetStatusChangeInput): Promise<Asset> {
    const response = await httpClient.post<{ data: Asset }>(`/assets/${id}/status`, input)
    return unwrap(response)
  },

  async getAssignments(assetId: string): Promise<AssetAssignment[]> {
    const response = await httpClient.get<{ data: AssetAssignment[] }>(`/assets/${assetId}/assignments`)
    return unwrap(response)
  },

  async assign(assetId: string, input: AssignAssetInput): Promise<AssetAssignment> {
    const response = await httpClient.post<{ data: AssetAssignment }>(`/assets/${assetId}/assign`, input)
    return unwrap(response)
  },

  async returnAssignment(assignmentId: string, input: ReturnAssignmentInput): Promise<AssetAssignment> {
    const response = await httpClient.post<{ data: AssetAssignment }>(
      `/asset-assignments/${assignmentId}/return`,
      input,
    )
    return unwrap(response)
  },
}
