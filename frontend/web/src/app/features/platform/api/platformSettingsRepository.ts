/** Contract for GET/PUT /platform/settings (docs/api-specification.md §27.6). */
export interface PlatformSettings {
  isPublicRegistrationEnabled: boolean
  requireApprovalForNewCompanies: boolean
}

export interface PlatformSettingsRepository {
  get(): Promise<PlatformSettings>
  update(settings: PlatformSettings): Promise<PlatformSettings>
}
