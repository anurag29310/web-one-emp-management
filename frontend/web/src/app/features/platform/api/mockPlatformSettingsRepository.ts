import { delay } from '@/app/shared/utils/delay'
import { mockRegistrationSettings } from '@/app/features/company-registration/api/mockCompanyRegistrationRepository'
import type { PlatformSettings, PlatformSettingsRepository } from './platformSettingsRepository'

export const mockPlatformSettingsRepository: PlatformSettingsRepository = {
  async get(): Promise<PlatformSettings> {
    await delay(150)
    return { ...mockRegistrationSettings }
  },

  async update(settings: PlatformSettings): Promise<PlatformSettings> {
    await delay(200)
    mockRegistrationSettings.isPublicRegistrationEnabled = settings.isPublicRegistrationEnabled
    mockRegistrationSettings.requireApprovalForNewCompanies = settings.requireApprovalForNewCompanies
    return { ...mockRegistrationSettings }
  },
}
