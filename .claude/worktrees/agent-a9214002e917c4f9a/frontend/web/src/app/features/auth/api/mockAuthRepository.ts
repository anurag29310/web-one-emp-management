import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type { AuthSession } from '@/app/shared/models/user'
import { tokenStorage } from '@/app/core/auth/tokenStorage'
import { mockAccounts } from './mockData'
import type { AuthRepository, LoginCredentials } from './authRepository'

const MOCK_REFRESH_PREFIX = 'mock-refresh-'

function buildSession(userId: string): AuthSession {
  const account = mockAccounts.find((a) => a.user.id === userId)
  if (!account) {
    throw new AppError('Mock session user not found.', 401, 'INVALID_CREDENTIALS')
  }
  return {
    accessToken: `mock-access-${userId}-${Date.now()}`,
    refreshToken: `${MOCK_REFRESH_PREFIX}${userId}`,
    expiresAtUtc: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    user: account.user,
  }
}

export const mockAuthRepository: AuthRepository = {
  async login({ userNameOrEmail, password }: LoginCredentials): Promise<AuthSession> {
    await delay(400)
    const account = mockAccounts.find(
      (a) =>
        (a.user.email.toLowerCase() === userNameOrEmail.toLowerCase() ||
          a.user.userName.toLowerCase() === userNameOrEmail.toLowerCase()) &&
        a.password === password,
    )
    if (!account) {
      throw new AppError('Invalid username or password.', 401, 'INVALID_CREDENTIALS')
    }

    const session = buildSession(account.user.id)
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
}
