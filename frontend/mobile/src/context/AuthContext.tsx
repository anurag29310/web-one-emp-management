import React, { createContext, useCallback, useEffect, useMemo, useState } from 'react'
import AsyncStorage from '@react-native-async-storage/async-storage'
import { createHttpClient, type LoginCredentials, type AuthenticatedUser } from '@ems/shared'
import type { ITokenStorage, ISessionCallbacks } from '@ems/shared'

interface AuthContextType {
  user: AuthenticatedUser | null
  isAuthenticated: boolean
  isInitializing: boolean
  login: (credentials: LoginCredentials) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined)

/**
 * Token storage implementation for React Native
 */
class AsyncStorageTokenStorage implements ITokenStorage {
  async getAccessToken(): Promise<string | null> {
    return AsyncStorage.getItem('accessToken')
  }

  async getRefreshToken(): Promise<string | null> {
    return AsyncStorage.getItem('refreshToken')
  }

  async setTokens(accessToken: string, refreshToken: string): Promise<void> {
    await Promise.all([
      AsyncStorage.setItem('accessToken', accessToken),
      AsyncStorage.setItem('refreshToken', refreshToken),
    ])
  }

  async clearTokens(): Promise<void> {
    await Promise.all([AsyncStorage.removeItem('accessToken'), AsyncStorage.removeItem('refreshToken')])
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthenticatedUser | null>(null)
  const [isInitializing, setIsInitializing] = useState(true)

  const tokenStorage = useMemo(() => new AsyncStorageTokenStorage(), [])

  const callbacks = useMemo<ISessionCallbacks>(
    () => ({
      onSessionExpired: () => {
        setUser(null)
      },
      onTokenRefreshed: (session) => {
        setUser(session.user)
      },
      onRefreshFailed: () => {
        setUser(null)
      },
    }),
    [],
  )

  const httpClient = useMemo(
    () => createHttpClient(process.env.EXPO_PUBLIC_API_URL || 'http://localhost:5000/api/v1', tokenStorage, callbacks),
    [tokenStorage, callbacks],
  )

  // Restore session on app startup
  useEffect(() => {
    async function restoreSession() {
      try {
        const accessToken = await tokenStorage.getAccessToken()
        if (!accessToken) {
          setUser(null)
          return
        }

        // TODO: Verify token is still valid
        // For now, we'll just set the user if we have a token
        // In production, you might want to call an endpoint to get user info
        setUser(null)
      } catch (err) {
        console.error('Failed to restore session:', err)
        setUser(null)
      } finally {
        setIsInitializing(false)
      }
    }

    restoreSession()
  }, [tokenStorage])

  const login = useCallback(
    async (credentials: LoginCredentials) => {
      const response = await httpClient.post<{ data: { accessToken: string; refreshToken: string; user: AuthenticatedUser } }>(
        '/auth/login',
        credentials,
      )

      const { accessToken, refreshToken, user: loginUser } = response.data.data
      await tokenStorage.setTokens(accessToken, refreshToken)
      setUser(loginUser)
    },
    [httpClient, tokenStorage],
  )

  const logout = useCallback(async () => {
    try {
      await httpClient.post('/auth/logout', {})
    } catch (err) {
      console.error('Logout failed:', err)
    } finally {
      await tokenStorage.clearTokens()
      setUser(null)
    }
  }, [httpClient, tokenStorage])

  const value = useMemo<AuthContextType>(
    () => ({
      user,
      isAuthenticated: user !== null,
      isInitializing,
      login,
      logout,
    }),
    [user, isInitializing, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = React.useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
