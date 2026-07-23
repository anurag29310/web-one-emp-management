import { httpClient, unwrap } from '@/app/core/api/httpClient'
import { tokenStorage } from '@/app/core/auth/tokenStorage'
import type { AuthenticatedUser, AuthSession } from '@/app/shared/models/user'
import type { AuthRepository, LoginCredentials } from './authRepository'

interface LoginResultDto {
  accessToken: string
  refreshToken: string
  expiresInSeconds: number
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

export const apiAuthRepository: AuthRepository = {
  async login(credentials: LoginCredentials): Promise<AuthSession> {
    const response = await httpClient.post<{ data: LoginResultDto }>('/auth/login', credentials)
    const { accessToken, refreshToken, expiresInSeconds } = unwrap(response)

    tokenStorage.setAccessToken(accessToken)
    tokenStorage.setRefreshToken(refreshToken)

    const user = await fetchCurrentUser()
    return {
      accessToken,
      refreshToken,
      expiresAtUtc: new Date(Date.now() + expiresInSeconds * 1000).toISOString(),
      user,
    }
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
}
