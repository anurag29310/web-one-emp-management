import { httpClient, unwrap } from '@/app/core/api/httpClient'
import type { PlatformSettings, PlatformSettingsRepository } from './platformSettingsRepository'

export const apiPlatformSettingsRepository: PlatformSettingsRepository = {
  async get(): Promise<PlatformSettings> {
    const response = await httpClient.get<{ data: PlatformSettings }>('/platform/settings')
    return unwrap(response)
  },

  async update(settings: PlatformSettings): Promise<PlatformSettings> {
    const response = await httpClient.put<{ data: PlatformSettings }>('/platform/settings', settings)
    return unwrap(response)
  },
}
