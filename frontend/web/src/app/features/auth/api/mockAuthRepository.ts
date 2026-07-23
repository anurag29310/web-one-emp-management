import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type { AuthSession, AuthenticatedUser } from '@/app/shared/models/user'
import { tokenStorage } from '@/app/core/auth/tokenStorage'
import { mockAccounts, type MockAccount } from './mockData'
import type {
  AuthRepository,
  ChangePasswordInput,
  DisableMfaInput,
  ForgotPasswordInput,
  LoginCredentials,
  LoginOutcome,
  MfaRecoveryCodes,
  MfaSetupInfo,
  RegenerateRecoveryCodesInput,
  RegisterInput,
  ResetPasswordInput,
  VerifyMfaCredentials,
} from './authRepository'

const MOCK_REFRESH_PREFIX = 'mock-refresh-'
const MFA_CHALLENGE_LIFETIME_MS = 5 * 60 * 1000

interface PendingMfaChallenge {
  userId: string
  expiresAtMs: number
}

// In-memory only — this whole mock backend resets on page reload, same as mockAccounts itself.
const mfaChallenges = new Map<string, PendingMfaChallenge>()
const forgotPasswordTokens = new Map<string, string>() // email (lowercase) -> reset token

function toAuthenticatedUser(account: MockAccount): AuthenticatedUser {
  return { ...account.user, isMfaEnabled: account.mfa.enabled }
}

function buildSession(userId: string): AuthSession {
  const account = mockAccounts.find((a) => a.user.id === userId)
  if (!account) {
    throw new AppError('Mock session user not found.', 401, 'INVALID_CREDENTIALS')
  }
  return {
    accessToken: `mock-access-${userId}-${Date.now()}`,
    refreshToken: `${MOCK_REFRESH_PREFIX}${userId}`,
    expiresAtUtc: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    user: toAuthenticatedUser(account),
  }
}

function findAccountByCredentials(userNameOrEmail: string, password: string): MockAccount | undefined {
  return mockAccounts.find(
    (a) =>
      (a.user.email.toLowerCase() === userNameOrEmail.toLowerCase() ||
        a.user.userName.toLowerCase() === userNameOrEmail.toLowerCase()) &&
      a.password === password,
  )
}

/** Resolves the account behind the current mock session's refresh token — the mock analogue of GetCurrentUserId() in AuthController. */
function requireCurrentAccount(): MockAccount {
  const refreshToken = tokenStorage.getRefreshToken()
  const userId = refreshToken?.startsWith(MOCK_REFRESH_PREFIX)
    ? refreshToken.slice(MOCK_REFRESH_PREFIX.length)
    : null
  const account = userId ? mockAccounts.find((a) => a.user.id === userId) : undefined
  if (!account) {
    throw new AppError('Not authenticated.', 401, 'UNAUTHORIZED')
  }
  return account
}

function randomBlock(alphabet: string, length: number): string {
  let block = ''
  for (let i = 0; i < length; i++) {
    block += alphabet[Math.floor(Math.random() * alphabet.length)]
  }
  return block
}

function generateRecoveryCodes(): string[] {
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789' // avoids ambiguous 0/O, 1/I
  return Array.from({ length: 10 }, () => `${randomBlock(alphabet, 5)}-${randomBlock(alphabet, 5)}`)
}

function generateTotpSecret(): string {
  return randomBlock('ABCDEFGHIJKLMNOPQRSTUVWXYZ234567', 16) // Base32 alphabet
}

export const mockAuthRepository: AuthRepository = {
  async login({ userNameOrEmail, password }: LoginCredentials): Promise<LoginOutcome> {
    await delay(400)
    const account = findAccountByCredentials(userNameOrEmail, password)
    if (!account) {
      throw new AppError('Invalid username or password.', 401, 'INVALID_CREDENTIALS')
    }

    if (account.mfa.enabled) {
      const challengeId = `mock-challenge-${account.user.id}-${Date.now()}`
      mfaChallenges.set(challengeId, {
        userId: account.user.id,
        expiresAtMs: Date.now() + MFA_CHALLENGE_LIFETIME_MS,
      })
      return { requiresMfa: true, mfaChallengeId: challengeId }
    }

    const session = buildSession(account.user.id)
    tokenStorage.setAccessToken(session.accessToken)
    tokenStorage.setRefreshToken(session.refreshToken)
    return { requiresMfa: false, session }
  },

  async verifyMfa({ mfaChallengeId, code }: VerifyMfaCredentials): Promise<AuthSession> {
    await delay(300)
    const challenge = mfaChallenges.get(mfaChallengeId)

    // One generic failure for unknown / expired / consumed / wrong-code, mirroring the real
    // API's refusal to reveal which of those it was.
    const fail = () => {
      mfaChallenges.delete(mfaChallengeId)
      throw new AppError('Invalid or expired verification code.', 401, 'INVALID_MFA_CODE')
    }

    if (!challenge || challenge.expiresAtMs <= Date.now()) fail()

    const account = mockAccounts.find((a) => a.user.id === challenge!.userId)
    if (!account) fail()

    // Mock TOTP validation: any 6-digit numeric code is accepted since there's no real
    // authenticator clock to check against here. A stored recovery code also works and is
    // consumed on use, same as the real API.
    const isTotpShaped = /^\d{6}$/.test(code)
    const recoveryIndex = account!.mfa.recoveryCodes.indexOf(code.toUpperCase())

    if (!isTotpShaped && recoveryIndex === -1) fail()

    if (recoveryIndex !== -1) {
      account!.mfa.recoveryCodes.splice(recoveryIndex, 1)
    }

    mfaChallenges.delete(mfaChallengeId)

    const session = buildSession(account!.user.id)
    tokenStorage.setAccessToken(session.accessToken)
    tokenStorage.setRefreshToken(session.refreshToken)
    return session
  },

  async logout(): Promise<void> {
    await delay(150)
    tokenStorage.clear()
  },

  async restoreSession(): Promise<AuthSession | null> {
    await delay(150)
    const refreshToken = tokenStorage.getRefreshToken()
    if (!refreshToken?.startsWith(MOCK_REFRESH_PREFIX)) return null

    const userId = refreshToken.slice(MOCK_REFRESH_PREFIX.length)
    try {
      const session = buildSession(userId)
      tokenStorage.setAccessToken(session.accessToken)
      tokenStorage.setRefreshToken(session.refreshToken)
      return session
    } catch {
      tokenStorage.clear()
      return null
    }
  },

  async register({ userName, email, password }: RegisterInput): Promise<AuthSession> {
    await delay(400)
    const exists = mockAccounts.some(
      (a) =>
        a.user.email.toLowerCase() === email.toLowerCase() ||
        a.user.userName.toLowerCase() === userName.toLowerCase(),
    )
    if (exists) {
      throw new AppError('Username or email already exists.', 409, 'ACCOUNT_EXISTS')
    }

    const account: MockAccount = {
      password,
      user: {
        id: `mock-${Date.now()}`,
        userName,
        email,
        role: null, // self-registration never grants a role, matching POST /auth/register
        isActive: true,
        isMfaEnabled: false,
      },
      mfa: { enabled: false, secret: null, pendingSecret: null, recoveryCodes: [] },
    }
    mockAccounts.push(account)

    const session = buildSession(account.user.id)
    tokenStorage.setAccessToken(session.accessToken)
    tokenStorage.setRefreshToken(session.refreshToken)
    return session
  },

  async forgotPassword({ email }: ForgotPasswordInput): Promise<void> {
    await delay(300)
    const account = mockAccounts.find((a) => a.user.email.toLowerCase() === email.toLowerCase())
    // Resolves the same way whether or not the account exists, matching the real API's
    // anti-enumeration behavior. There's no mailbox to deliver to in mock mode, so the token is
    // only ever surfaced to the dev console.
    if (account) {
      const token = randomBlock('abcdefghjkmnpqrstuvwxyz23456789', 8)
      forgotPasswordTokens.set(email.toLowerCase(), token)
      console.info(`[mock-auth] Password reset code for ${email}: ${token}`)
    }
  },

  async resetPassword({ email, resetToken, newPassword }: ResetPasswordInput): Promise<void> {
    await delay(300)
    const expectedToken = forgotPasswordTokens.get(email.toLowerCase())
    const account = mockAccounts.find((a) => a.user.email.toLowerCase() === email.toLowerCase())
    if (!account || !expectedToken || expectedToken !== resetToken) {
      throw new AppError('Invalid or expired reset code.', 400, 'INVALID_RESET_TOKEN')
    }
    account.password = newPassword
    forgotPasswordTokens.delete(email.toLowerCase())
  },

  async changePassword({ currentPassword, newPassword }: ChangePasswordInput): Promise<void> {
    await delay(300)
    const account = requireCurrentAccount()
    if (account.password !== currentPassword) {
      throw new AppError('Current password is incorrect.', 401, 'INVALID_PASSWORD')
    }
    account.password = newPassword
  },

  async getCurrentUser(): Promise<AuthenticatedUser> {
    await delay(150)
    return toAuthenticatedUser(requireCurrentAccount())
  },

  async setupMfa(): Promise<MfaSetupInfo> {
    await delay(300)
    const account = requireCurrentAccount()
    if (account.mfa.enabled) {
      throw new AppError('MFA is already enabled.', 409, 'MFA_ALREADY_ENABLED')
    }
    const secret = generateTotpSecret()
    account.mfa.pendingSecret = secret
    return {
      manualEntryKey: secret,
      otpAuthUri: `otpauth://totp/EMS:${encodeURIComponent(account.user.email)}?secret=${secret}&issuer=EMS&algorithm=SHA1&digits=6&period=30`,
    }
  },

  async enableMfa(code: string): Promise<MfaRecoveryCodes> {
    await delay(300)
    const account = requireCurrentAccount()
    if (account.mfa.enabled) {
      throw new AppError('MFA is already enabled.', 409, 'MFA_ALREADY_ENABLED')
    }
    if (!account.mfa.pendingSecret) {
      throw new AppError('Start MFA setup before confirming a code.', 409, 'MFA_SETUP_NOT_STARTED')
    }
    if (!/^\d{6}$/.test(code)) {
      throw new AppError('Invalid verification code.', 401, 'INVALID_MFA_CODE')
    }

    const recoveryCodes = generateRecoveryCodes()
    account.mfa.enabled = true
    account.mfa.secret = account.mfa.pendingSecret
    account.mfa.pendingSecret = null
    account.mfa.recoveryCodes = recoveryCodes
    account.user.isMfaEnabled = true
    return { recoveryCodes }
  },

  async disableMfa({ password }: DisableMfaInput): Promise<void> {
    await delay(300)
    const account = requireCurrentAccount()
    if (account.password !== password) {
      throw new AppError('Incorrect password.', 401, 'INVALID_PASSWORD')
    }
    account.mfa.enabled = false
    account.mfa.secret = null
    account.mfa.pendingSecret = null
    account.mfa.recoveryCodes = []
    account.user.isMfaEnabled = false
  },

  async regenerateMfaRecoveryCodes({ password }: RegenerateRecoveryCodesInput): Promise<MfaRecoveryCodes> {
    await delay(300)
    const account = requireCurrentAccount()
    if (account.password !== password) {
      throw new AppError('Incorrect password.', 401, 'INVALID_PASSWORD')
    }
    if (!account.mfa.enabled) {
      throw new AppError('MFA is not enabled.', 409, 'MFA_NOT_ENABLED')
    }
    const recoveryCodes = generateRecoveryCodes()
    account.mfa.recoveryCodes = recoveryCodes
    return { recoveryCodes }
  },
}
