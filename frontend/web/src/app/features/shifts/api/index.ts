import { selectRepository } from '@/app/core/config/selectRepository'
import { mockShiftRepository } from './mockShiftRepository'
import { apiShiftRepository } from './apiShiftRepository'
import type { ShiftRepository } from './shiftRepository'

export const shiftRepository: ShiftRepository = selectRepository({
  mock: mockShiftRepository,
  api: apiShiftRepository,
})

export type {
  AssignEmployeeShiftInput,
  CreateShiftInput,
  EmployeeShift,
  Shift,
  ShiftRepository,
  UpdateEmployeeShiftInput,
  UpdateShiftInput,
} from './shiftRepository'
