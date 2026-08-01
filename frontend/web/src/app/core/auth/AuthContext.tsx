import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import {
  authRepository,
  type LoginCredentials,
  type LoginOutcome,
  type VerifyMfaCredentials,
} from '@/app/features/auth/api'
import type { AuthenticatedUser } from '@/app/shared/models/user'
import { tokenStorage } from './tokenStorage'
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

  const establishSession = useCallback(async (accessToken: string, refreshToken: string): Promise<void> => {
    tokenStorage.setAccessToken(accessToken)
    tokenStorage.setRefreshToken(refreshToken)
    const user = await authRepository.getCurrentUser()
    setUser(user)
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
      establishSession,
      logout,
    }),
    [user, isInitializing, login, completeMfaLogin, establishSession, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
