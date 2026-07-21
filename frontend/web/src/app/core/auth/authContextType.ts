import { createContext } from 'react'
import type { AuthenticatedUser } from '@/app/shared/models/user'
import type { LoginCredentials } from '@/app/features/auth/api'

export interface AuthContextValue {
  user: AuthenticatedUser | null
  isAuthenticated: boolean
  isInitializing: boolean
  login: (credentials: LoginCredentials) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
