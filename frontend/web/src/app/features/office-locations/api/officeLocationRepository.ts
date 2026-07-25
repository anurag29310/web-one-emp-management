export interface OfficeLocation {
  id: string
  name: string
  code: string
  addressLine1: string | null
  addressLine2: string | null
  city: string
  state: string | null
  country: string
  timeZoneId: string
  isDeleted: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface CreateOfficeLocationInput {
  name: string
  code: string
  addressLine1?: string
  addressLine2?: string
  city: string
  state?: string
  country: string
  timeZoneId: string
}

export interface UpdateOfficeLocationInput extends CreateOfficeLocationInput {
  id: string
}

export interface OfficeLocationRepository {
  list(): Promise<OfficeLocation[]>
  getById(id: string): Promise<OfficeLocation>
  create(input: CreateOfficeLocationInput): Promise<OfficeLocation>
  update(input: UpdateOfficeLocationInput): Promise<OfficeLocation>
  remove(id: string): Promise<void>
}
