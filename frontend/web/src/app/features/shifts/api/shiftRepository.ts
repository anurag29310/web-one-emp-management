export interface Shift {
  id: string
  name: string
  /** "HH:mm:ss" (TimeSpan serialized by the backend). */
  startTime: string
  /** "HH:mm:ss" (TimeSpan serialized by the backend). */
  endTime: string
  graceMinutes: number
  isNightShift: boolean
}

export interface CreateShiftInput {
  name: string
  startTime: string
  endTime: string
  graceMinutes: number
  isNightShift: boolean
}

export interface UpdateShiftInput extends CreateShiftInput {
  id: string
}

export interface EmployeeShift {
  id: string
  employeeId: string
  shiftId: string
  effectiveFrom: string
  effectiveTo: string | null
}

export interface AssignEmployeeShiftInput {
  employeeId: string
  shiftId: string
  effectiveFrom: string
  effectiveTo?: string
}

export interface UpdateEmployeeShiftInput {
  employeeId: string
  assignmentId: string
  shiftId: string
  effectiveFrom: string
  effectiveTo?: string
}

/**
 * Contract for /shifts and /employees/{employeeId}/shifts (api-specification.md §8.9),
 * cross-checked against backend/EMS.API/Controllers/ShiftController.cs. Shift CRUD and
 * assignment mutations require the CanManageShifts policy (Admin, HR); enforced server-side,
 * gated client-side via useAuth().user.role.
 */
export interface ShiftRepository {
  list(): Promise<Shift[]>
  getById(id: string): Promise<Shift>
  create(input: CreateShiftInput): Promise<Shift>
  update(input: UpdateShiftInput): Promise<Shift>
  remove(id: string): Promise<void>
  getEmployeeShifts(employeeId: string): Promise<EmployeeShift[]>
  assignEmployeeShift(input: AssignEmployeeShiftInput): Promise<EmployeeShift>
  updateEmployeeShift(input: UpdateEmployeeShiftInput): Promise<EmployeeShift>
  endEmployeeShift(employeeId: string, assignmentId: string): Promise<void>
}
