import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
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
import { mockAssetAssignments, mockAssets } from './mockData'

let assets = [...mockAssets]
let assignments = [...mockAssetAssignments]

function nextId(prefix: string): string {
  return `${prefix}-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

function nextAssetTag(): string {
  return `AST-${Date.now().toString(16).toUpperCase().slice(-8)}`
}

function findAssetOrThrow(id: string): Asset {
  const asset = assets.find((a) => a.id === id)
  if (!asset) {
    throw new AppError(`Asset ${id} was not found.`, 404, 'NOT_FOUND')
  }
  return asset
}

export const mockAssetRepository: AssetRepository = {
  async list(filters: AssetListFilters = {}): Promise<PagedResult<Asset>> {
    await delay(300)
    const { page = 1, pageSize = 20, status, category, search } = filters

    let filtered = assets.filter((a) => !a.isDeleted)
    if (status) filtered = filtered.filter((a) => a.status === status)
    if (category) filtered = filtered.filter((a) => a.category.toLowerCase() === category.toLowerCase())
    if (search) {
      const term = search.toLowerCase()
      filtered = filtered.filter(
        (a) =>
          a.assetTag.toLowerCase().includes(term) ||
          a.brand.toLowerCase().includes(term) ||
          a.model.toLowerCase().includes(term) ||
          a.serialNumber.toLowerCase().includes(term),
      )
    }

    filtered = [...filtered].sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc))

    const start = (page - 1) * pageSize
    const pageItems = filtered.slice(start, start + pageSize)

    return {
      data: pageItems,
      page,
      pageSize,
      totalCount: filtered.length,
      totalPages: Math.max(1, Math.ceil(filtered.length / pageSize)),
      correlationId: 'mock-correlation-id',
    }
  },

  async getById(id: string): Promise<Asset> {
    await delay(200)
    return findAssetOrThrow(id)
  },

  async create(input: AssetInput): Promise<Asset> {
    await delay(300)
    const asset: Asset = {
      id: nextId('00000000'),
      assetTag: nextAssetTag(),
      category: input.category,
      brand: input.brand,
      model: input.model,
      serialNumber: input.serialNumber,
      purchaseDate: input.purchaseDate,
      purchaseCost: input.purchaseCost,
      status: 'Available',
      notes: input.notes ?? null,
      isDeleted: false,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    assets = [asset, ...assets]
    return asset
  },

  async update(id: string, input: AssetInput): Promise<Asset> {
    await delay(300)
    const existing = findAssetOrThrow(id)
    const updated: Asset = {
      ...existing,
      category: input.category,
      brand: input.brand,
      model: input.model,
      serialNumber: input.serialNumber,
      purchaseDate: input.purchaseDate,
      purchaseCost: input.purchaseCost,
      notes: input.notes ?? null,
      updatedAtUtc: new Date().toISOString(),
    }
    assets = assets.map((a) => (a.id === id ? updated : a))
    return updated
  },

  async remove(id: string): Promise<void> {
    await delay(200)
    const existing = findAssetOrThrow(id)
    if (existing.status === 'Assigned') {
      throw new AppError('Cannot delete an asset that is currently assigned.', 409, 'ASSET_ASSIGNED')
    }
    assets = assets.map((a) => (a.id === id ? { ...a, isDeleted: true } : a))
  },

  async restore(id: string): Promise<void> {
    await delay(200)
    assets = assets.map((a) => (a.id === id ? { ...a, isDeleted: false } : a))
  },

  async changeStatus(id: string, input: AssetStatusChangeInput): Promise<Asset> {
    await delay(250)
    const existing = findAssetOrThrow(id)
    if (existing.status === 'Assigned') {
      throw new AppError('Asset must be returned before its status can change.', 409, 'ASSET_ASSIGNED')
    }
    const updated: Asset = {
      ...existing,
      status: input.status,
      notes: input.notes ?? existing.notes,
      updatedAtUtc: new Date().toISOString(),
    }
    assets = assets.map((a) => (a.id === id ? updated : a))
    return updated
  },

  async getAssignments(assetId: string): Promise<AssetAssignment[]> {
    await delay(250)
    return assignments
      .filter((a) => a.assetId === assetId)
      .sort((a, b) => b.assignedDate.localeCompare(a.assignedDate))
  },

  async assign(assetId: string, input: AssignAssetInput): Promise<AssetAssignment> {
    await delay(300)
    const existing = findAssetOrThrow(assetId)
    if (existing.status !== 'Available') {
      throw new AppError('Only an available asset can be assigned.', 409, 'ASSET_NOT_AVAILABLE')
    }

    const assignment: AssetAssignment = {
      id: nextId('00000000'),
      assetId,
      assetTag: existing.assetTag,
      employeeId: input.employeeId,
      employeeName: input.employeeId,
      assignedByUserId: '00000000-0000-0000-0000-000000000001',
      assignedDate: new Date().toISOString(),
      expectedReturnDate: input.expectedReturnDate ?? null,
      conditionAtAssignment: input.conditionAtAssignment ?? null,
      returnedDate: null,
      conditionAtReturn: null,
      notes: input.notes ?? null,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    assignments = [assignment, ...assignments]
    assets = assets.map((a) => (a.id === assetId ? { ...a, status: 'Assigned', updatedAtUtc: assignment.createdAtUtc } : a))
    return assignment
  },

  async returnAssignment(assignmentId: string, input: ReturnAssignmentInput): Promise<AssetAssignment> {
    await delay(300)
    const existing = assignments.find((a) => a.id === assignmentId)
    if (!existing) {
      throw new AppError(`Assignment ${assignmentId} was not found.`, 404, 'NOT_FOUND')
    }
    const returnedAt = new Date().toISOString()
    const updated: AssetAssignment = {
      ...existing,
      returnedDate: returnedAt,
      conditionAtReturn: input.conditionAtReturn ?? null,
      notes: input.notes ?? existing.notes,
      updatedAtUtc: returnedAt,
    }
    assignments = assignments.map((a) => (a.id === assignmentId ? updated : a))
    assets = assets.map((a) =>
      a.id === existing.assetId
        ? { ...a, status: input.resultingAssetStatus ?? 'Available', updatedAtUtc: returnedAt }
        : a,
    )
    return updated
  },
}
