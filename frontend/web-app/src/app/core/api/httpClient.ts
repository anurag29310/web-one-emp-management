import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { appConfig } from '../config/env'
import { tokenStorage } from '../auth/tokenStorage'
import { sessionEvents } from '../auth/sessionEvents'
import { AppError } from '@/app/shared/models/appError'
import type { ApiErrorEnvelope } from '@/app/shared/models/apiEnvelope'

interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retried?: boolean
}

export const httpClient = axios.create({
  baseURL: appConfig.apiBaseUrl,
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json',
  },
})

// Bare client for the refresh call itself, so a failed refresh can't trigger
// the response interceptor below and recurse into another refresh attempt.
const refreshClient = axios.create({ baseURL: appConfig.apiBaseUrl })

httpClient.interceptors.request.use((config) => {
  const accessToken = tokenStorage.getAccessToken()
  if (accessToken) {
    config.headers.set('Authorization', `Bearer ${accessToken}`)
  }
  return config
})

let refreshPromise: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = tokenStorage.getRefreshToken()
  if (!refreshToken) return null

  try {
    const response = await refreshClient.post<{
      data: { accessToken: string; refreshToken: string }
    }>('/auth/refresh', { refreshToken })
    const { accessToken, refreshToken: rotatedRefreshToken } = response.data.data
    tokenStorage.setAccessToken(accessToken)
    tokenStorage.setRefreshToken(rotatedRefreshToken)
    return accessToken
  } catch {
    tokenStorage.clear()
    sessionEvents.emitSessionExpired()
    return null
  }
}

httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiErrorEnvelope>) => {
    const originalRequest = error.config as RetryableRequestConfig | undefined
    const isUnauthorized = error.response?.status === 401
    const isAuthEndpoint = originalRequest?.url?.startsWith('/auth/')

    if (isUnauthorized && originalRequest && !originalRequest._retried && !isAuthEndpoint) {
      originalRequest._retried = true

      refreshPromise ??= refreshAccessToken().finally(() => {
        refreshPromise = null
      })
      const newAccessToken = await refreshPromise

      if (newAccessToken) {
        originalRequest.headers.set('Authorization', `Bearer ${newAccessToken}`)
        return httpClient(originalRequest)
      }
    }

    const status = error.response?.status ?? 0
    const body = error.response?.data
    throw new AppError(
      body?.message ?? error.message ?? 'An unexpected error occurred.',
      status,
      body?.code ?? 'NETWORK_ERROR',
    )
  },
)

export function unwrap<T>(response: { data: { data: T } }): T {
  return response.data.data
}
