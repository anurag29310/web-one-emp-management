import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type {
  CreateLeaveTypeInput,
  LeaveType,
  LeaveTypeRepository,
  UpdateLeaveTypeInput,
} from './leaveTypeRepository'
import { mockLeaveTypes } from './mockData'

let leaveTypes = [...mockLeaveTypes]
const deletedIds = new Set<string>()

function nextId(): string {
  return `30000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

export const mockLeaveTypeRepository: LeaveTypeRepository = {
  async list(): Promise<LeaveType[]> {
    await delay(250)
    return leaveTypes.filter((lt) => !deletedIds.has(lt.id))
  },

  async getById(id: string): Promise<LeaveType> {
    await delay(200)
    const leaveType = leaveTypes.find((lt) => lt.id === id)
    if (!leaveType) {
      throw new AppError(`Leave type ${id} was not found.`, 404, 'NOT_FOUND')
    }
    return leaveType
  },

  async create(input: CreateLeaveTypeInput): Promise<LeaveType> {
    await delay(300)
    if (input.code && leaveTypes.some((lt) => lt.code === input.code && !deletedIds.has(lt.id))) {
      throw new AppError('Leave type code already exists.', 409, 'CONFLICT')
    }
    const leaveType: LeaveType = {
      id: nextId(),
      name: input.name,
      code: input.code ?? null,
      isPaid: input.isPaid,
      requiresApproval: input.requiresApproval,
      annualEntitlementDays: input.annualEntitlementDays ?? null,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    leaveTypes = [...leaveTypes, leaveType]
    return leaveType
  },

  async update(input: UpdateLeaveTypeInput): Promise<LeaveType> {
    await delay(300)
    const existing = leaveTypes.find((lt) => lt.id === input.id)
    if (!existing) {
      throw new AppError(`Leave type ${input.id} was not found.`, 404, 'NOT_FOUND')
    }
    if (
      input.code &&
      leaveTypes.some((lt) => lt.id !== input.id && lt.code === input.code && !deletedIds.has(lt.id))
    ) {
      throw new AppError('Leave type code already exists.', 409, 'CONFLICT')
    }
    const updated: LeaveType = {
      ...existing,
      name: input.name,
      code: input.code ?? null,
      isPaid: input.isPaid,
      requiresApproval: input.requiresApproval,
      annualEntitlementDays: input.annualEntitlementDays ?? null,
      updatedAtUtc: new Date().toISOString(),
    }
    leaveTypes = leaveTypes.map((lt) => (lt.id === input.id ? updated : lt))
    return updated
  },

  async remove(id: string): Promise<void> {
    await delay(200)
    deletedIds.add(id)
  },

  async restore(id: string): Promise<void> {
    await delay(200)
    deletedIds.delete(id)
  },
}
