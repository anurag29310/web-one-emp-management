import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import type {
  Company,
  CompanyCreateInput,
  CompanyDetail,
  CompanyListFilters,
  CompanyUpdateInput,
  PlatformCompanyRepository,
} from './companyRepository'
import { mockCompanies, mockCompanyAdmins, mockCompanyEmployeeCounts } from './mockData'

let companies = [...mockCompanies]

function nextId(): string {
  return `00000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

function findCompanyOrThrow(id: string): Company {
  const company = companies.find((c) => c.id === id)
  if (!company) {
    throw new AppError(`Company ${id} was not found.`, 404, 'NOT_FOUND')
  }
  return company
}

function updateCompany(id: string, patch: Partial<Company>): void {
  companies = companies.map((c) => (c.id === id ? { ...c, ...patch, updatedAtUtc: new Date().toISOString() } : c))
}

export const mockCompanyRepository: PlatformCompanyRepository = {
  async list(filters: CompanyListFilters = {}): Promise<PagedResult<Company>> {
    await delay(300)
    const { page = 1, pageSize = 20, status, search } = filters

    let filtered = companies
    if (status) filtered = filtered.filter((c) => c.status === status)
    if (search) {
      const term = search.toLowerCase()
      filtered = filtered.filter((c) => c.name.toLowerCase().includes(term))
    }

    filtered = [...filtered].sort((a, b) => b.registeredAtUtc.localeCompare(a.registeredAtUtc))

    const start = (page - 1) * pageSize
    const pageItems = filtered.slice(start, start + pageSize)

    return {
      data: pageItems,
      page,
      pageSize,
      totalCount: filtered.length,
      totalPages: Math.max(1, Math.ceil(filtered.length / pageSize)),
      correlationId: 'mock-correlation-id',
    }
  },

  async getById(id: string): Promise<CompanyDetail> {
    await delay(200)
    const company = findCompanyOrThrow(id)
    return {
      ...company,
      employeeCount: mockCompanyEmployeeCounts[id] ?? 0,
      admins: mockCompanyAdmins[id] ?? [],
    }
  },

  async create(input: CompanyCreateInput): Promise<Company> {
    await delay(300)
    if (companies.some((c) => !c.isDeleted && c.name.toLowerCase() === input.name.toLowerCase())) {
      throw new AppError(`A company named "${input.name}" already exists.`, 409, 'VALIDATION_ERROR')
    }
    const now = new Date().toISOString()
    const company: Company = {
      id: nextId(),
      name: input.name,
      status: 'Active',
      timezone: input.timezone,
      currency: input.currency,
      logoUrl: input.logoUrl ?? null,
      registeredAtUtc: now,
      approvedAtUtc: now,
      suspendedAtUtc: null,
      suspendedReason: null,
      rejectedAtUtc: null,
      rejectedReason: null,
      isDeleted: false,
      createdAtUtc: now,
      updatedAtUtc: null,
    }
    companies = [company, ...companies]
    return company
  },

  async update(id: string, input: CompanyUpdateInput): Promise<Company> {
    await delay(300)
    findCompanyOrThrow(id)
    updateCompany(id, {
      name: input.name,
      timezone: input.timezone,
      currency: input.currency,
      logoUrl: input.logoUrl ?? null,
    })
    return findCompanyOrThrow(id)
  },

  async remove(id: string): Promise<void> {
    await delay(200)
    findCompanyOrThrow(id)
    updateCompany(id, { isDeleted: true })
  },

  async restore(id: string): Promise<void> {
    await delay(200)
    findCompanyOrThrow(id)
    updateCompany(id, { isDeleted: false })
  },

  async activate(id: string): Promise<void> {
    await delay(200)
    findCompanyOrThrow(id)
    updateCompany(id, { status: 'Active', suspendedAtUtc: null, suspendedReason: null })
  },

  async suspend(id: string, reason?: string): Promise<void> {
    await delay(200)
    findCompanyOrThrow(id)
    updateCompany(id, { status: 'Suspended', suspendedAtUtc: new Date().toISOString(), suspendedReason: reason ?? null })
  },

  async approve(id: string): Promise<void> {
    await delay(200)
    const company = findCompanyOrThrow(id)
    if (company.status !== 'PendingApproval') {
      throw new AppError('Only a company pending approval can be approved.', 409, 'INVALID_STATUS_TRANSITION')
    }
    updateCompany(id, { status: 'Trial', approvedAtUtc: new Date().toISOString() })
  },

  async reject(id: string, reason?: string): Promise<void> {
    await delay(200)
    const company = findCompanyOrThrow(id)
    if (company.status !== 'PendingApproval') {
      throw new AppError('Only a company pending approval can be rejected.', 409, 'INVALID_STATUS_TRANSITION')
    }
    updateCompany(id, { status: 'Rejected', rejectedAtUtc: new Date().toISOString(), rejectedReason: reason ?? null })
  },

  async forceLogout(id: string): Promise<void> {
    await delay(200)
    findCompanyOrThrow(id)
  },

  async resetAdminPassword(companyId: string, userId: string): Promise<string> {
    await delay(200)
    findCompanyOrThrow(companyId)
    const admins = mockCompanyAdmins[companyId] ?? []
    if (!admins.some((a) => a.userId === userId)) {
      throw new AppError(`Admin ${userId} was not found for this company.`, 404, 'NOT_FOUND')
    }
    return `mock-reset-token-${userId}-${Date.now()}`
  },
}
