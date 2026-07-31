import { selectRepository } from '@/app/core/config/selectRepository'
import { mockAssetRepository } from './mockAssetRepository'
import { apiAssetRepository } from './apiAssetRepository'
import type { AssetRepository } from './assetRepository'

export const assetRepository: AssetRepository = selectRepository({
  mock: mockAssetRepository,
  api: apiAssetRepository,
})

export type {
  Asset,
  AssetAssignment,
  AssetInput,
  AssetListFilters,
  AssetRepository,
  AssetStatus,
  AssetStatusChangeInput,
  AssignAssetInput,
  ReturnAssignmentInput,
} from './assetRepository'
