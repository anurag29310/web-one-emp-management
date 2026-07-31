import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
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

export const apiAssetRepository: AssetRepository = {
  async list(filters?: AssetListFilters): Promise<PagedResult<Asset>> {
    const response = await httpClient.get<PagedResult<Asset>>('/assets', { params: filters })
    return response.data
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
