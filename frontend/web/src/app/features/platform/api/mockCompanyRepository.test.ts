import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { PlatformCompanyRepository } from './companyRepository'

// The mock repository holds its "database" as module-level mutable state, so each test needs a
// fresh module instance — see mockAssetRepository.test.ts for the same pattern.
async function loadRepository(): Promise<PlatformCompanyRepository> {
  const module = await import('./mockCompanyRepository')
  return module.mockCompanyRepository
}

beforeEach(() => {
  vi.resetModules()
})

const ACTIVE_COMPANY_ID = '00000000-0000-0000-0000-00000000c001' // Acme Corporation, status: Active
const SUSPENDED_COMPANY_ID = '00000000-0000-0000-0000-00000000c003' // Initech Solutions, status: Suspended
const PENDING_COMPANY_ID = '00000000-0000-0000-0000-00000000c004' // Umbrella Staffing, status: PendingApproval
const REJECTED_COMPANY_ID = '00000000-0000-0000-0000-00000000c005' // Wayne Enterprises, status: Rejected

describe('mockCompanyRepository.list', () => {
  it('filters by status', async () => {
    const repository = await loadRepository()
    const result = await repository.list({ status: 'Suspended' })

    expect(result.data.length).toBeGreaterThan(0)
    expect(result.data.every((c) => c.status === 'Suspended')).toBe(true)
  })

  it('filters by search across company name', async () => {
    const repository = await loadRepository()
    const result = await repository.list({ search: 'acme' })

    expect(result.data).toHaveLength(1)
    expect(result.data[0].id).toBe(ACTIVE_COMPANY_ID)
  })
})

describe('mockCompanyRepository.getById', () => {
  it('throws a 404 AppError for an unknown id', async () => {
    const repository = await loadRepository()

    await expect(repository.getById('does-not-exist')).rejects.toMatchObject({
      status: 404,
      code: 'NOT_FOUND',
    })
  })

  it('includes employeeCount and admins', async () => {
    const repository = await loadRepository()
    const detail = await repository.getById(ACTIVE_COMPANY_ID)

    expect(detail.employeeCount).toBeGreaterThan(0)
    expect(detail.admins.length).toBeGreaterThan(0)
  })
})

describe('mockCompanyRepository.create', () => {
  it('lands directly in Active status', async () => {
    const repository = await loadRepository()
    const company = await repository.create({ name: 'New Co', timezone: 'UTC', currency: 'USD' })

    expect(company.status).toBe('Active')
  })

  it('rejects a duplicate company name', async () => {
    const repository = await loadRepository()

    await expect(
      repository.create({ name: 'Acme Corporation', timezone: 'UTC', currency: 'USD' }),
    ).rejects.toMatchObject({ status: 409 })
  })
})

describe('mockCompanyRepository status transitions', () => {
  it('activates a suspended company', async () => {
    const repository = await loadRepository()
    await repository.activate(SUSPENDED_COMPANY_ID)

    const company = await repository.getById(SUSPENDED_COMPANY_ID)
    expect(company.status).toBe('Active')
  })

  it('suspends an active company and records the reason', async () => {
    const repository = await loadRepository()
    await repository.suspend(ACTIVE_COMPANY_ID, 'Non-payment')

    const company = await repository.getById(ACTIVE_COMPANY_ID)
    expect(company.status).toBe('Suspended')
    expect(company.suspendedReason).toBe('Non-payment')
  })

  it('approves a pending-approval company into Trial', async () => {
    const repository = await loadRepository()
    await repository.approve(PENDING_COMPANY_ID)

    const company = await repository.getById(PENDING_COMPANY_ID)
    expect(company.status).toBe('Trial')
  })

  it('rejects approving a company that is not PendingApproval', async () => {
    const repository = await loadRepository()

    await expect(repository.approve(ACTIVE_COMPANY_ID)).rejects.toMatchObject({
      status: 409,
      code: 'INVALID_STATUS_TRANSITION',
    })
  })

  it('rejects a pending-approval company with a reason', async () => {
    const repository = await loadRepository()
    await repository.reject(PENDING_COMPANY_ID, 'Duplicate registration')

    const company = await repository.getById(PENDING_COMPANY_ID)
    expect(company.status).toBe('Rejected')
    expect(company.rejectedReason).toBe('Duplicate registration')
  })

  it('rejects rejecting a company that is not PendingApproval', async () => {
    const repository = await loadRepository()

    await expect(repository.reject(REJECTED_COMPANY_ID)).rejects.toMatchObject({
      status: 409,
      code: 'INVALID_STATUS_TRANSITION',
    })
  })
})

describe('mockCompanyRepository.remove / restore', () => {
  it('soft-deletes a company and restore brings it back', async () => {
    const repository = await loadRepository()

    await repository.remove(ACTIVE_COMPANY_ID)
    const afterDelete = await repository.list()
    expect(afterDelete.data.find((c) => c.id === ACTIVE_COMPANY_ID)?.isDeleted).toBe(true)

    await repository.restore(ACTIVE_COMPANY_ID)
    const afterRestore = await repository.list()
    expect(afterRestore.data.find((c) => c.id === ACTIVE_COMPANY_ID)?.isDeleted).toBe(false)
  })
})

describe('mockCompanyRepository.resetAdminPassword', () => {
  it('returns a token for a known admin', async () => {
    const repository = await loadRepository()
    const detail = await repository.getById(ACTIVE_COMPANY_ID)
    const token = await repository.resetAdminPassword(ACTIVE_COMPANY_ID, detail.admins[0].userId)

    expect(token).toBeTruthy()
  })

  it('throws a 404 AppError for an unknown admin', async () => {
    const repository = await loadRepository()

    await expect(repository.resetAdminPassword(ACTIVE_COMPANY_ID, 'unknown-user')).rejects.toMatchObject({
      status: 404,
    })
  })
})
