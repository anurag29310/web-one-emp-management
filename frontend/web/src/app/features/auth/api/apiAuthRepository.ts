import { httpClient, unwrap } from '@/app/core/api/httpClient'
import { tokenStorage } from '@/app/core/auth/tokenStorage'
import { AppError } from '@/app/shared/models/appError'
import type { AuthenticatedUser, AuthSession } from '@/app/shared/models/user'
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

interface LoginResultDto {
  // Null when requiresMfa is true — the caller must complete POST /auth/mfa/verify with
  // mfaChallengeId before tokens are issued (see docs/api-specification.md section 3.1).
  accessToken: string | null
  refreshToken: string | null
  expiresInSeconds: number
  requiresMfa: boolean
  mfaChallengeId: string | null
}

interface RefreshResultDto {
  accessToken: string
  refreshToken: string
  expiresAtUtc: string
}

async function fetchCurrentUser(): Promise<AuthenticatedUser> {
  const response = await httpClient.get<{ data: AuthenticatedUser }>('/auth/me')
  return unwrap(response)
}

async function establishSession(
  accessToken: string,
  refreshToken: string,
  expiresInSeconds: number,
): Promise<AuthSession> {
  tokenStorage.setAccessToken(accessToken)
  tokenStorage.setRefreshToken(refreshToken)

  const user = await fetchCurrentUser()
  return {
    accessToken,
    refreshToken,
    expiresAtUtc: new Date(Date.now() + expiresInSeconds * 1000).toISOString(),
    user,
  }
}

function requireTokens(result: LoginResultDto): { accessToken: string; refreshToken: string } {
  if (!result.accessToken || !result.refreshToken) {
    throw new AppError('Server did not return access tokens.', 500, 'INVALID_RESPONSE')
  }
  return { accessToken: result.accessToken, refreshToken: result.refreshToken }
}

export const apiAuthRepository: AuthRepository = {
  async login(credentials: LoginCredentials): Promise<LoginOutcome> {
    const response = await httpClient.post<{ data: LoginResultDto }>('/auth/login', credentials)
    const result = unwrap(response)

    if (result.requiresMfa) {
      if (!result.mfaChallengeId) {
        throw new AppError('Server did not return an MFA challenge id.', 500, 'INVALID_RESPONSE')
      }
      return { requiresMfa: true, mfaChallengeId: result.mfaChallengeId }
    }

    const { accessToken, refreshToken } = requireTokens(result)
    const session = await establishSession(accessToken, refreshToken, result.expiresInSeconds)
    return { requiresMfa: false, session }
  },

  async verifyMfa({ mfaChallengeId, code }: VerifyMfaCredentials): Promise<AuthSession> {
    const response = await httpClient.post<{ data: LoginResultDto }>('/auth/mfa/verify', {
      mfaChallengeId,
      code,
    })
    const result = unwrap(response)
    const { accessToken, refreshToken } = requireTokens(result)
    return establishSession(accessToken, refreshToken, result.expiresInSeconds)
  },

  async logout(): Promise<void> {
    const refreshToken = tokenStorage.getRefreshToken()
    try {
      if (refreshToken) {
        await httpClient.post('/auth/logout', { refreshToken })
      }
    } finally {
      tokenStorage.clear()
    }
  },

  async restoreSession(): Promise<AuthSession | null> {
    const refreshToken = tokenStorage.getRefreshToken()
    if (!refreshToken) return null

    try {
      const response = await httpClient.post<{ data: RefreshResultDto }>('/auth/refresh', {
        refreshToken,
      })
      const result = unwrap(response)
      tokenStorage.setAccessToken(result.accessToken)
      tokenStorage.setRefreshToken(result.refreshToken)

      const user = await fetchCurrentUser()
      return { ...result, user }
    } catch {
      tokenStorage.clear()
      return null
    }
  },

  async register(input: RegisterInput): Promise<AuthSession> {
    const response = await httpClient.post<{ data: LoginResultDto }>('/auth/register', input)
    const result = unwrap(response)
    const { accessToken, refreshToken } = requireTokens(result)
    return establishSession(accessToken, refreshToken, result.expiresInSeconds)
  },

  async forgotPassword(input: ForgotPasswordInput): Promise<void> {
    await httpClient.post('/auth/forgot-password', input)
  },

  async resetPassword(input: ResetPasswordInput): Promise<void> {
    await httpClient.post('/auth/reset-password', input)
  },

  async changePassword(input: ChangePasswordInput): Promise<void> {
    await httpClient.post('/auth/change-password', input)
  },

  async getCurrentUser(): Promise<AuthenticatedUser> {
    return fetchCurrentUser()
  },

  async setupMfa(): Promise<MfaSetupInfo> {
    const response = await httpClient.post<{ data: MfaSetupInfo }>('/auth/mfa/setup')
    return unwrap(response)
  },

  async enableMfa(code: string): Promise<MfaRecoveryCodes> {
    const response = await httpClient.post<{ data: MfaRecoveryCodes }>('/auth/mfa/enable', { code })
    return unwrap(response)
  },

  async disableMfa(input: DisableMfaInput): Promise<void> {
    await httpClient.post('/auth/mfa/disable', input)
  },

  async regenerateMfaRecoveryCodes(input: RegenerateRecoveryCodesInput): Promise<MfaRecoveryCodes> {
    const response = await httpClient.post<{ data: MfaRecoveryCodes }>(
      '/auth/mfa/recovery-codes/regenerate',
      input,
    )
    return unwrap(response)
  },
}
