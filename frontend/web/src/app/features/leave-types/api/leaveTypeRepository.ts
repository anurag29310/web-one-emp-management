export interface LeaveType {
  id: string
  name: string
  code: string | null
  isPaid: boolean
  requiresApproval: boolean
  annualEntitlementDays: number | null
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface CreateLeaveTypeInput {
  name: string
  code?: string
  isPaid: boolean
  requiresApproval: boolean
  annualEntitlementDays?: number
}

export interface UpdateLeaveTypeInput extends CreateLeaveTypeInput {
  id: string
}

export interface LeaveTypeRepository {
  list(): Promise<LeaveType[]>
  getById(id: string): Promise<LeaveType>
  create(input: CreateLeaveTypeInput): Promise<LeaveType>
  update(input: UpdateLeaveTypeInput): Promise<LeaveType>
  remove(id: string): Promise<void>
  restore(id: string): Promise<void>
}
