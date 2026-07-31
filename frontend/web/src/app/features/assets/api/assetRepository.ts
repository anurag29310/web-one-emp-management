import type { PagedResult } from '@/app/shared/models/apiEnvelope'

export type AssetStatus = 'Available' | 'Assigned' | 'UnderRepair' | 'Retired' | 'Lost'

export interface Asset {
  id: string
  assetTag: string
  category: string
  brand: string
  model: string
  serialNumber: string
  purchaseDate: string
  purchaseCost: number
  status: AssetStatus
  notes: string | null
  isDeleted: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface AssetListFilters {
  page?: number
  pageSize?: number
  status?: AssetStatus
  category?: string
  search?: string
}

export interface AssetInput {
  category: string
  brand: string
  model: string
  serialNumber: string
  purchaseDate: string
  purchaseCost: number
  notes?: string
}

export interface AssetStatusChangeInput {
  status: Exclude<AssetStatus, 'Assigned'>
  notes?: string
}

export interface AssetAssignment {
  id: string
  assetId: string
  assetTag: string
  employeeId: string
  employeeName: string
  assignedByUserId: string
  assignedDate: string
  expectedReturnDate: string | null
  conditionAtAssignment: string | null
  returnedDate: string | null
  conditionAtReturn: string | null
  notes: string | null
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface AssignAssetInput {
  employeeId: string
  expectedReturnDate?: string
  conditionAtAssignment?: string
  notes?: string
}

export interface ReturnAssignmentInput {
  conditionAtReturn?: string
  resultingAssetStatus?: Exclude<AssetStatus, 'Assigned'>
  notes?: string
}

export interface AssetRepository {
  list(filters?: AssetListFilters): Promise<PagedResult<Asset>>
  getById(id: string): Promise<Asset>
  create(input: AssetInput): Promise<Asset>
  update(id: string, input: AssetInput): Promise<Asset>
  remove(id: string): Promise<void>
  restore(id: string): Promise<void>
  changeStatus(id: string, input: AssetStatusChangeInput): Promise<Asset>
  getAssignments(assetId: string): Promise<AssetAssignment[]>
  assign(assetId: string, input: AssignAssetInput): Promise<AssetAssignment>
  returnAssignment(assignmentId: string, input: ReturnAssignmentInput): Promise<AssetAssignment>
}
