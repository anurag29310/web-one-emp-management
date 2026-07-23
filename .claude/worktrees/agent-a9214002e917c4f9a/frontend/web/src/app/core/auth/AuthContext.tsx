import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { authRepository, type LoginCredentials } from '@/app/features/auth/api'
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

  const login = useCallback(async (credentials: LoginCredentials) => {
    const session = await authRepository.login(credentials)
    setUser(session.user)
  }, [])

  const logout = useCallback(async () => {
    await authRepository.logout()
    setUser(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null, isInitializing, login, logout }),
    [user, isInitializing, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
