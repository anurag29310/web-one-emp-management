import axios, { AxiosInstance, AxiosError, InternalAxiosRequestConfig } from 'axios'
import type { ApiErrorResponse, Session } from './types'
import {
  ValidationError,
  AuthenticationError,
  AuthorizationError,
  NotFoundError,
  AppError,
} from './errors'

/**
 * Token storage interface for platform independence
 */
export interface ITokenStorage {
  getAccessToken(): Promise<string | null>
  getRefreshToken(): Promise<string | null>
  setTokens(accessToken: string, refreshToken: string): Promise<void>
  clearTokens(): Promise<void>
}

/**
 * Session callback interface
 */
export interface ISessionCallbacks {
  onSessionExpired?: () => void
  onTokenRefreshed?: (session: Session) => void
  onRefreshFailed?: () => void
}

/**
 * Creates a configured HTTP client for the EMS API
 */
export function createHttpClient(
  baseURL: string,
  tokenStorage?: ITokenStorage,
  callbacks?: ISessionCallbacks,
): AxiosInstance {
  const client = axios.create({
    baseURL,
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
    },
    timeout: 30000,
  })

  // Request interceptor: Add auth token
  client.interceptors.request.use(
    async (config: InternalAxiosRequestConfig) => {
      if (tokenStorage) {
        const token = await tokenStorage.getAccessToken()
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
        }
      }
      return config
    },
    (error) => Promise.reject(error),
  )

  // Response interceptor: Handle errors and token refresh
  client.interceptors.response.use(
    (response) => response,
    async (error: AxiosError<ApiErrorResponse>) => {
      const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }

      // Handle 401 Unauthorized - attempt token refresh
      if (error.response?.status === 401 && !originalRequest._retry && tokenStorage) {
        originalRequest._retry = true

        try {
          const refreshToken = await tokenStorage.getRefreshToken()
          if (!refreshToken) {
            callbacks?.onSessionExpired?.()
            return Promise.reject(new AuthenticationError())
          }

          // Attempt to refresh token
          const response = await axios.post<{ data: Session }>(
            `${baseURL}/auth/refresh`,
            { refreshToken },
          )

          const { accessToken, refreshToken: newRefreshToken } = response.data.data
          await tokenStorage.setTokens(accessToken, newRefreshToken)
          callbacks?.onTokenRefreshed?.(response.data.data)

          // Retry original request with new token
          originalRequest.headers.Authorization = `Bearer ${accessToken}`
          return client(originalRequest)
        } catch {
          callbacks?.onRefreshFailed?.()
          callbacks?.onSessionExpired?.()
          await tokenStorage.clearTokens()
          return Promise.reject(new AuthenticationError('Session expired'))
        }
      }

      // Handle specific error codes
      return Promise.reject(transformError(error))
    },
  )

  return client
}

/**
 * Transform axios error to AppError
 */
function transformError(error: AxiosError<ApiErrorResponse>): AppError {
  if (!error.response) {
    return new AppError(error.message || 'Network error', undefined, 'NETWORK_ERROR')
  }

  const { status, data } = error.response

  if (!data) {
    return new AppError('Server error', status)
  }

  switch (status) {
    case 400:
      if (Array.isArray(data.errors)) {
        const fields = data.errors.reduce(
          (acc, err) => {
            acc[err.field] = err.message
            return acc
          },
          {} as Record<string, string>,
        )
        return new ValidationError(data.message, fields)
      }
      return new AppError(data.message, status, data.code)

    case 401:
      return new AuthenticationError(data.message)

    case 403:
      return new AuthorizationError(data.message)

    case 404:
      return new NotFoundError(data.message)

    default:
      return new AppError(data.message, status, data.code, { correlationId: data.correlationId })
  }
}
