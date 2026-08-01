import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import { mockAccounts, type MockAccount } from '@/app/features/auth/api/mockData'
import type {
  CompanyRegistrationInput,
  CompanyRegistrationRepository,
  RegisterCompanyResult,
} from './companyRegistrationRepository'

const MOCK_REFRESH_PREFIX = 'mock-refresh-'

/**
 * Mirrors the two PlatformSettings toggles (docs/database-design.md §24) purely for exercising
 * the registration page's "closed" / "requires approval" states in mock mode — the real settings
 * live server-side and are managed through features/platform's Platform Settings page in api mode.
 */
export const mockRegistrationSettings = {
  isPublicRegistrationEnabled: true,
  requireApprovalForNewCompanies: true,
}

function nextId(): string {
  return `00000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

export const mockCompanyRegistrationRepository: CompanyRegistrationRepository = {
  async getStatus(): Promise<boolean> {
    await delay(150)
    return mockRegistrationSettings.isPublicRegistrationEnabled
  },

  async register(input: CompanyRegistrationInput): Promise<RegisterCompanyResult> {
    await delay(400)

    if (!mockRegistrationSettings.isPublicRegistrationEnabled) {
      throw new AppError('Registration is currently closed.', 403, 'REGISTRATION_DISABLED')
    }

    const exists = mockAccounts.some(
      (a) =>
        a.user.email.toLowerCase() === input.adminEmail.toLowerCase() ||
        a.user.userName.toLowerCase() === input.adminUserName.toLowerCase(),
    )
    if (exists) {
      throw new AppError('Username or email already exists.', 409, 'ACCOUNT_EXISTS')
    }

    const companyId = nextId()
    const userId = nextId()
    const requiresApproval = mockRegistrationSettings.requireApprovalForNewCompanies

    const account: MockAccount = {
      password: input.adminPassword,
      user: {
        id: userId,
        userName: input.adminUserName,
        email: input.adminEmail,
        role: 'Admin',
        isActive: true,
        isMfaEnabled: false,
      },
      mfa: { enabled: false, secret: null, pendingSecret: null, recoveryCodes: [] },
    }
    mockAccounts.push(account)

    if (requiresApproval) {
      return {
        companyId,
        companyStatus: 'PendingApproval',
        requiresApproval: true,
        accessToken: null,
        refreshToken: null,
        expiresInSeconds: null,
      }
    }

    return {
      companyId,
      companyStatus: 'Trial',
      requiresApproval: false,
      accessToken: `mock-access-${userId}-${Date.now()}`,
      refreshToken: `${MOCK_REFRESH_PREFIX}${userId}`,
      expiresInSeconds: 3600,
    }
  },
}
