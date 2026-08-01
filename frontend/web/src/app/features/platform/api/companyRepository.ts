import type { PagedResult } from '@/app/shared/models/apiEnvelope'

/**
 * Contract for /platform/companies (docs/api-specification.md §27.3), cross-checked against
 * backend/EMS.API/Controllers/PlatformCompaniesController.cs and
 * EMS.Application/Features/Companies/DTOs/CompanyDto.cs. Every endpoint here requires the
 * IsSuperAdmin policy — enforced server-side; the frontend additionally hides the whole
 * /platform/* route tree from non-SuperAdmin users, see core/routes/PlatformProtectedRoute.tsx.
 */
export type CompanyStatus = 'Trial' | 'Active' | 'Suspended' | 'Inactive' | 'PendingApproval' | 'Rejected'

export interface Company {
  id: string
  name: string
  status: CompanyStatus
  timezone: string
  currency: string
  logoUrl: string | null
  registeredAtUtc: string
  approvedAtUtc: string | null
  suspendedAtUtc: string | null
  suspendedReason: string | null
  rejectedAtUtc: string | null
  rejectedReason: string | null
  isDeleted: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface CompanyAdmin {
  userId: string
  userName: string
  email: string
  isActive: boolean
}

export interface CompanyDetail extends Company {
  employeeCount: number
  admins: CompanyAdmin[]
}

export interface CompanyListFilters {
  page?: number
  pageSize?: number
  status?: CompanyStatus
  search?: string
}

export interface CompanyCreateInput {
  name: string
  timezone: string
  currency: string
  logoUrl?: string
}

export type CompanyUpdateInput = CompanyCreateInput

export interface PlatformCompanyRepository {
  list(filters?: CompanyListFilters): Promise<PagedResult<Company>>
  getById(id: string): Promise<CompanyDetail>
  /** Directly creates a company — no approval gate, lands in Active. */
  create(input: CompanyCreateInput): Promise<Company>
  update(id: string, input: CompanyUpdateInput): Promise<Company>
  /** Soft-deletes the company. */
  remove(id: string): Promise<void>
  restore(id: string): Promise<void>
  /** Suspended/Inactive -> Active. */
  activate(id: string): Promise<void>
  /** -> Suspended. Also revokes every refresh token for the company's users. */
  suspend(id: string, reason?: string): Promise<void>
  /** PendingApproval -> Trial only. */
  approve(id: string): Promise<void>
  /** PendingApproval -> Rejected only. */
  reject(id: string, reason?: string): Promise<void>
  /** Revokes every refresh token for the company's users without changing its status. */
  forceLogout(id: string): Promise<void>
  /** Issues a password-reset token for one of the company's Admin users, returning that token. */
  resetAdminPassword(companyId: string, userId: string): Promise<string>
}
