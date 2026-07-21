import type { AuthSession } from '@/app/shared/models/user'

export interface LoginCredentials {
  userNameOrEmail: string
  password: string
}

/**
 * The contract every page/hook depends on. LoginPage, AuthContext, and any
 * future consumer talk only to this interface — never to the mock or API
 * implementation directly — so they cannot tell which data source is active.
 */
export interface AuthRepository {
  login(credentials: LoginCredentials): Promise<AuthSession>
  logout(): Promise<void>
  /** Re-establishes a session from a persisted refresh token on app load, or resolves to null if none exists / it's no longer valid. */
  restoreSession(): Promise<AuthSession | null>
}
