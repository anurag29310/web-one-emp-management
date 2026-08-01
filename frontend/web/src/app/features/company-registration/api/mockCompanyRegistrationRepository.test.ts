import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { CompanyRegistrationRepository } from './companyRegistrationRepository'

// mockRegistrationSettings and the shared mockAccounts array are module-level mutable state, so
// each test needs a fresh module instance — see mockAssetRepository.test.ts for the same pattern.
async function loadRepository(): Promise<{
  repository: CompanyRegistrationRepository
  settings: { isPublicRegistrationEnabled: boolean; requireApprovalForNewCompanies: boolean }
}> {
  const module = await import('./mockCompanyRegistrationRepository')
  return { repository: module.mockCompanyRegistrationRepository, settings: module.mockRegistrationSettings }
}

beforeEach(() => {
  vi.resetModules()
})

const validInput = {
  companyName: 'New Co',
  timezone: 'UTC',
  currency: 'USD',
  adminUserName: 'newco.admin',
  adminEmail: 'admin@newco.example.com',
  adminPassword: 'Password@123',
}

describe('mockCompanyRegistrationRepository.getStatus', () => {
  it('reflects the isPublicRegistrationEnabled toggle', async () => {
    const { repository, settings } = await loadRepository()
    expect(await repository.getStatus()).toBe(true)

    settings.isPublicRegistrationEnabled = false
    expect(await repository.getStatus()).toBe(false)
  })
})

describe('mockCompanyRegistrationRepository.register', () => {
  it('throws when registration is disabled', async () => {
    const { repository, settings } = await loadRepository()
    settings.isPublicRegistrationEnabled = false

    await expect(repository.register(validInput)).rejects.toMatchObject({
      status: 403,
      code: 'REGISTRATION_DISABLED',
    })
  })

  it('lands in PendingApproval with no tokens when approval is required', async () => {
    const { repository, settings } = await loadRepository()
    settings.requireApprovalForNewCompanies = true

    const result = await repository.register(validInput)

    expect(result.requiresApproval).toBe(true)
    expect(result.companyStatus).toBe('PendingApproval')
    expect(result.accessToken).toBeNull()
    expect(result.refreshToken).toBeNull()
  })

  it('lands in Trial with tokens when approval is not required', async () => {
    const { repository, settings } = await loadRepository()
    settings.requireApprovalForNewCompanies = false

    const result = await repository.register(validInput)

    expect(result.requiresApproval).toBe(false)
    expect(result.companyStatus).toBe('Trial')
    expect(result.accessToken).toBeTruthy()
    expect(result.refreshToken).toBeTruthy()
  })

  it('rejects a duplicate email/username', async () => {
    const { repository } = await loadRepository()

    await expect(
      repository.register({ ...validInput, adminEmail: 'admin@ems.local', adminUserName: 'not-admin' }),
    ).rejects.toMatchObject({ status: 409, code: 'ACCOUNT_EXISTS' })
  })
})
