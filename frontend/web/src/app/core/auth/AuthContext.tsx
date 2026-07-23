import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import {
  authRepository,
  type LoginCredentials,
  type LoginOutcome,
  type RegisterInput,
  type VerifyMfaCredentials,
} from '@/app/features/auth/api'
import type { AuthenticatedUser } from '@/app/shared/models/user'
import { sessionEvents } from './sessionEvents'
import { AuthContext, type AuthContextValue } from './authContextType'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthenticatedUser | null>(null)
  const [isInitializing, setIsInitializing] = useState(true)

  useEffect(() => {
    let cancelled = false
    authRepository
      .restoreSession()
      .then((session) => {
        if (!cancelled) setUser(session?.user ?? null)
      })
      .finally(() => {
        if (!cancelled) setIsInitializing(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    sessionEvents.onSessionExpired(() => setUser(null))
  }, [])

  const login = useCallback(async (credentials: LoginCredentials): Promise<LoginOutcome> => {
    const outcome = await authRepository.login(credentials)
    if (!outcome.requiresMfa) {
      setUser(outcome.session.user)
    }
    return outcome
  }, [])

  const completeMfaLogin = useCallback(async (credentials: VerifyMfaCredentials): Promise<void> => {
    const session = await authRepository.verifyMfa(credentials)
    setUser(session.user)
  }, [])

  const register = useCallback(async (input: RegisterInput): Promise<void> => {
    const session = await authRepository.register(input)
    setUser(session.user)
  }, [])

  const logout = useCallback(async () => {
    await authRepository.logout()
    setUser(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      isInitializing,
      login,
      completeMfaLogin,
      register,
      logout,
    }),
    [user, isInitializing, login, completeMfaLogin, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
