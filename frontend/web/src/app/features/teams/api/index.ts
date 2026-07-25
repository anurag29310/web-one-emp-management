import { selectRepository } from '@/app/core/config/selectRepository'
import { mockTeamRepository } from './mockTeamRepository'
import { apiTeamRepository } from './apiTeamRepository'
import type { TeamRepository } from './teamRepository'

export const teamRepository: TeamRepository = selectRepository({
  mock: mockTeamRepository,
  api: apiTeamRepository,
})

export type { Team, TeamRepository, CreateTeamInput, UpdateTeamInput } from './teamRepository'
