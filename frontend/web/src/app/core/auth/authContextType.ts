import { createContext } from 'react'
import type { AuthenticatedUser } from '@/app/shared/models/user'
import type { LoginCredentials, LoginOutcome, VerifyMfaCredentials } from '@/app/features/auth/api'

export interface AuthContextValue {
  user: AuthenticatedUser | null
  isAuthenticated: boolean
  isInitializing: boolean
  /** May resolve to an MFA challenge instead of establishing a session — see LoginOutcome. */
  login: (credentials: LoginCredentials) => Promise<LoginOutcome>
  /** Completes a login that returned requiresMfa: true. */
  completeMfaLogin: (credentials: VerifyMfaCredentials) => Promise<void>
  /**
   * Adopts a session whose tokens were issued outside the normal login flow — currently only by
   * POST /company-registration landing directly in Trial (no approval required). Stores the
   * tokens and fetches /auth/me, same as login()/verifyMfa() do internally.
   */
  establishSession: (accessToken: string, refreshToken: string) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
