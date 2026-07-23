import { createContext } from 'react'
import type { AuthenticatedUser } from '@/app/shared/models/user'
import type {
  LoginCredentials,
  LoginOutcome,
  RegisterInput,
  VerifyMfaCredentials,
} from '@/app/features/auth/api'

export interface AuthContextValue {
  user: AuthenticatedUser | null
  isAuthenticated: boolean
  isInitializing: boolean
  /** May resolve to an MFA challenge instead of establishing a session — see LoginOutcome. */
  login: (credentials: LoginCredentials) => Promise<LoginOutcome>
  /** Completes a login that returned requiresMfa: true. */
  completeMfaLogin: (credentials: VerifyMfaCredentials) => Promise<void>
  register: (input: RegisterInput) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
