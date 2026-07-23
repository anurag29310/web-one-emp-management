import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type {
  AssignEmployeeShiftInput,
  CreateShiftInput,
  EmployeeShift,
  Shift,
  ShiftRepository,
  UpdateEmployeeShiftInput,
  UpdateShiftInput,
} from './shiftRepository'
import { mockEmployeeShifts, mockShifts } from './mockData'

let shifts = [...mockShifts]
let assignments = [...mockEmployeeShifts]

function nextId(prefix: string): string {
  return `${prefix}-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

export const mockShiftRepository: ShiftRepository = {
  async list(): Promise<Shift[]> {
    await delay(250)
    return shifts
  },

  async getById(id: string): Promise<Shift> {
    await delay(200)
    const shift = shifts.find((s) => s.id === id)
    if (!shift) throw new AppError(`Shift ${id} was not found.`, 404, 'NOT_FOUND')
    return shift
  },

  async create(input: CreateShiftInput): Promise<Shift> {
    await delay(300)
    const shift: Shift = { id: nextId('30000000'), ...input }
    shifts = [...shifts, shift]
    return shift
  },

  async update(input: UpdateShiftInput): Promise<Shift> {
    await delay(300)
    const existing = shifts.find((s) => s.id === input.id)
    if (!existing) throw new AppError(`Shift ${input.id} was not found.`, 404, 'NOT_FOUND')
    const updated: Shift = { ...existing, ...input }
    shifts = shifts.map((s) => (s.id === input.id ? updated : s))
    return updated
  },

  async remove(id: string): Promise<void> {
    await delay(200)
    shifts = shifts.filter((s) => s.id !== id)
  },

  async getEmployeeShifts(employeeId: string): Promise<EmployeeShift[]> {
    await delay(200)
    return assignments.filter((a) => a.employeeId === employeeId)
  },

  async assignEmployeeShift(input: AssignEmployeeShiftInput): Promise<EmployeeShift> {
    await delay(300)
    const assignment: EmployeeShift = {
      id: nextId('60000000'),
      employeeId: input.employeeId,
      shiftId: input.shiftId,
      effectiveFrom: input.effectiveFrom,
      effectiveTo: input.effectiveTo ?? null,
    }
    assignments = [...assignments, assignment]
    return assignment
  },

  async updateEmployeeShift(input: UpdateEmployeeShiftInput): Promise<EmployeeShift> {
    await delay(300)
    const existing = assignments.find((a) => a.id === input.assignmentId && a.employeeId === input.employeeId)
    if (!existing) throw new AppError(`Shift assignment ${input.assignmentId} was not found.`, 404, 'NOT_FOUND')
    const updated: EmployeeShift = {
      ...existing,
      shiftId: input.shiftId,
      effectiveFrom: input.effectiveFrom,
      effectiveTo: input.effectiveTo ?? null,
    }
    assignments = assignments.map((a) => (a.id === input.assignmentId ? updated : a))
    return updated
  },

  async endEmployeeShift(employeeId: string, assignmentId: string): Promise<void> {
    await delay(200)
    assignments = assignments.filter((a) => !(a.id === assignmentId && a.employeeId === employeeId))
  },
}
