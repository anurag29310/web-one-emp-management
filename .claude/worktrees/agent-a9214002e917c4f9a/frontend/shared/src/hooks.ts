/**
 * Shared hooks (platform-independent)
 * These provide utility functions that can be used by both web and mobile
 */

import type { AxiosInstance } from 'axios'
import type { ApiListResponse } from './types'

/**
 * Create a hook for fetching authenticated data
 */
export function createUseFetch(httpClient: AxiosInstance) {
  return async function useFetch<T>(
    url: string,
    options?: { params?: Record<string, unknown> },
  ): Promise<{ data: T }> {
    const response = await httpClient.get<{ data: T }>(url, {
      params: options?.params,
    })
    return response.data
  }
}

/**
 * Create a hook for fetching paginated data
 */
export function createUsePaginatedFetch(httpClient: AxiosInstance) {
  return async function usePaginatedFetch<T>(
    url: string,
    page: number = 1,
    pageSize: number = 20,
    options?: { search?: string; sortBy?: string; sortDirection?: 'asc' | 'desc' },
  ): Promise<ApiListResponse<T>> {
    const response = await httpClient.get<ApiListResponse<T>>(url, {
      params: {
        page,
        pageSize,
        ...options,
      },
    })
    return response.data
  }
}

/**
 * Create a hook for creating resources
 */
export function createUseCreate(httpClient: AxiosInstance) {
  return async function useCreate<T>(url: string, data: unknown): Promise<{ data: T }> {
    const response = await httpClient.post<{ data: T }>(url, data)
    return response.data
  }
}

/**
 * Create a hook for updating resources
 */
export function createUseUpdate(httpClient: AxiosInstance) {
  return async function useUpdate<T>(url: string, data: unknown): Promise<{ data: T }> {
    const response = await httpClient.put<{ data: T }>(url, data)
    return response.data
  }
}

/**
 * Create a hook for deleting resources
 */
export function createUseDelete(httpClient: AxiosInstance) {
  return async function useDelete(url: string): Promise<void> {
    await httpClient.delete(url)
  }
}
