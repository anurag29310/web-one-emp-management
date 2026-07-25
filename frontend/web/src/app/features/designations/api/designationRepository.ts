export interface Designation {
  id: string
  name: string
  code: string
  level: number | null
  isDeleted: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface CreateDesignationInput {
  name: string
  code: string
  level?: number
}

export interface UpdateDesignationInput extends CreateDesignationInput {
  id: string
}

export interface DesignationRepository {
  list(): Promise<Designation[]>
  getById(id: string): Promise<Designation>
  create(input: CreateDesignationInput): Promise<Designation>
  update(input: UpdateDesignationInput): Promise<Designation>
  remove(id: string): Promise<void>
}
