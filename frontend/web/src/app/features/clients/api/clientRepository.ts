import type { PagedResult } from '@/app/shared/models/apiEnvelope'

export interface Client {
  id: string
  clientName: string
  companyName: string
  contactPerson: string
  mobileNumber: string
  alternateMobile: string | null
  email: string
  gstNumber: string | null
  addressLine1: string
  addressLine2: string | null
  city: string
  state: string | null
  country: string
  postalCode: string
  latitude: number | null
  longitude: number | null
  notes: string | null
  isActive: boolean
  isArchived: boolean
  isDeleted: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface ClientListFilters {
  page?: number
  pageSize?: number
  search?: string
  isActive?: boolean
}

export interface ClientInput {
  clientName: string
  companyName: string
  contactPerson: string
  mobileNumber: string
  alternateMobile?: string
  email: string
  gstNumber?: string
  addressLine1: string
  addressLine2?: string
  city: string
  state?: string
  country: string
  postalCode: string
  latitude?: number
  longitude?: number
  notes?: string
}

export interface ClientRepository {
  list(filters?: ClientListFilters): Promise<PagedResult<Client>>
  getById(id: string): Promise<Client>
  create(input: ClientInput): Promise<Client>
  update(id: string, input: ClientInput): Promise<Client>
  remove(id: string): Promise<void>
  activate(id: string): Promise<void>
  deactivate(id: string): Promise<void>
  archive(id: string): Promise<void>
  restore(id: string): Promise<void>
}
