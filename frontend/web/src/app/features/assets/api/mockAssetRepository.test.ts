import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AssetRepository } from './assetRepository'

// The mock repository holds its "database" as module-level mutable state, so
// each test needs a fresh module instance — otherwise assign/return/delete
// mutations from one test would leak into the next.
async function loadRepository(): Promise<AssetRepository> {
  const module = await import('./mockAssetRepository')
  return module.mockAssetRepository
}

beforeEach(() => {
  vi.resetModules()
})

const AVAILABLE_ASSET_ID = '00000000-0000-0000-0000-000000001602' // MacBook Pro, status: Available
const ASSIGNED_ASSET_ID = '00000000-0000-0000-0000-000000001601' // Dell Latitude, status: Assigned
const RETIRED_ASSET_ID = '00000000-0000-0000-0000-000000001604' // LG monitor, status: Retired
const OUTSTANDING_ASSIGNMENT_ID = '00000000-0000-0000-0000-000000001701' // open assignment on the Dell

describe('mockAssetRepository.list', () => {
  it('excludes soft-deleted assets and sorts newest first', async () => {
    const repository = await loadRepository()
    const result = await repository.list()

    expect(result.data.every((asset) => !asset.isDeleted)).toBe(true)
    const createdDates = result.data.map((asset) => asset.createdAtUtc)
    expect(createdDates).toEqual([...createdDates].sort().reverse())
  })

  it('filters by status', async () => {
    const repository = await loadRepository()
    const result = await repository.list({ status: 'Available' })

    expect(result.data.length).toBeGreaterThan(0)
    expect(result.data.every((asset) => asset.status === 'Available')).toBe(true)
  })

  it('filters by search across tag, brand, model, and serial number', async () => {
    const repository = await loadRepository()
    const result = await repository.list({ search: 'dell' })

    expect(result.data).toHaveLength(1)
    expect(result.data[0].brand).toBe('Dell')
  })
})

describe('mockAssetRepository.getById', () => {
  it('throws a 404 AppError for an unknown id', async () => {
    const repository = await loadRepository()

    await expect(repository.getById('does-not-exist')).rejects.toMatchObject({
      status: 404,
      code: 'NOT_FOUND',
    })
  })
})

describe('mockAssetRepository.create', () => {
  it('always starts a new asset at Available status', async () => {
    const repository = await loadRepository()
    const asset = await repository.create({
      category: 'Tablet',
      brand: 'Samsung',
      model: 'Galaxy Tab S9',
      serialNumber: 'SN-NEW-001',
      purchaseDate: '2026-07-01',
      purchaseCost: 650,
    })

    expect(asset.status).toBe('Available')
    expect(asset.assetTag).toMatch(/^AST-/)
  })
})

describe('mockAssetRepository.assign', () => {
  it('assigns an available asset and flips its status to Assigned', async () => {
    const repository = await loadRepository()
    const assignment = await repository.assign(AVAILABLE_ASSET_ID, { employeeId: 'employee-1' })

    expect(assignment.assetId).toBe(AVAILABLE_ASSET_ID)
    expect(assignment.returnedDate).toBeNull()

    const asset = await repository.getById(AVAILABLE_ASSET_ID)
    expect(asset.status).toBe('Assigned')
  })

  it('rejects assigning an asset that is not Available', async () => {
    const repository = await loadRepository()

    await expect(repository.assign(ASSIGNED_ASSET_ID, { employeeId: 'employee-1' })).rejects.toMatchObject({
      status: 409,
    })
  })
})

describe('mockAssetRepository.returnAssignment', () => {
  it('closes out the assignment and applies the resulting asset status', async () => {
    const repository = await loadRepository()

    const returned = await repository.returnAssignment(OUTSTANDING_ASSIGNMENT_ID, {
      resultingAssetStatus: 'UnderRepair',
      conditionAtReturn: 'Keyboard sticking.',
    })

    expect(returned.returnedDate).not.toBeNull()
    expect(returned.conditionAtReturn).toBe('Keyboard sticking.')

    const asset = await repository.getById(ASSIGNED_ASSET_ID)
    expect(asset.status).toBe('UnderRepair')
  })
})

describe('mockAssetRepository.remove', () => {
  it('rejects deleting an asset that is currently Assigned', async () => {
    const repository = await loadRepository()

    await expect(repository.remove(ASSIGNED_ASSET_ID)).rejects.toMatchObject({ status: 409 })
  })

  it('soft-deletes an asset that is not Assigned, and restore brings it back', async () => {
    const repository = await loadRepository()

    await repository.remove(RETIRED_ASSET_ID)
    const afterDelete = await repository.list()
    expect(afterDelete.data.some((asset) => asset.id === RETIRED_ASSET_ID)).toBe(false)

    await repository.restore(RETIRED_ASSET_ID)
    const afterRestore = await repository.list()
    expect(afterRestore.data.some((asset) => asset.id === RETIRED_ASSET_ID)).toBe(true)
  })
})

describe('mockAssetRepository.changeStatus', () => {
  it('rejects a direct status change while the asset is Assigned', async () => {
    const repository = await loadRepository()

    await expect(repository.changeStatus(ASSIGNED_ASSET_ID, { status: 'Retired' })).rejects.toMatchObject({
      status: 409,
    })
  })

  it('applies a status change for an asset that is not Assigned', async () => {
    const repository = await loadRepository()

    const updated = await repository.changeStatus(AVAILABLE_ASSET_ID, {
      status: 'UnderRepair',
      notes: 'Battery replacement.',
    })

    expect(updated.status).toBe('UnderRepair')
    expect(updated.notes).toBe('Battery replacement.')
  })
})
