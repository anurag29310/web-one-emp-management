/**
 * Contract for the public, unauthenticated company self-registration surface
 * (docs/api-specification.md §27.4) — creates a Company and its first Admin
 * User atomically. This is the only way to create a new tenant; the legacy
 * POST /auth/register (self-registration with no company) was removed.
 */
export interface CompanyRegistrationInput {
  companyName: string
  timezone: string
  currency: string
  adminUserName: string
  adminEmail: string
  adminPassword: string
}

export interface RegisterCompanyResult {
  companyId: string
  companyStatus: string
  requiresApproval: boolean
  /** Populated only when the company lands directly in Trial (requiresApproval === false). */
  accessToken: string | null
  refreshToken: string | null
  expiresInSeconds: number | null
}

export interface CompanyRegistrationRepository {
  /** Whether the registration form should currently be shown (PlatformSettings.IsPublicRegistrationEnabled). */
  getStatus(): Promise<boolean>
  register(input: CompanyRegistrationInput): Promise<RegisterCompanyResult>
}
