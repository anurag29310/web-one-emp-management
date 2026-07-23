export interface User {
  id: string
  userName: string
  email: string
  isActive: boolean
  roleId: string | null
  roleName: string | null
  employeeId: string | null
  isDeleted: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface UserListFilters {
  includeDeleted?: boolean
  roleId?: string
  isActive?: boolean
}

export interface CreateUserInput {
  userName: string
  email: string
  temporaryPassword: string
  roleId?: string
  employeeId?: string
  isActive?: boolean
}

export interface UpdateUserInput {
  id: string
  userName: string
  email: string
  roleId?: string
  employeeId?: string
}

export interface UpdateUserStatusInput {
  id: string
  isActive: boolean
}

/**
 * Backend contract: docs/api-specification.md section 4.1, cross-checked
 * against backend/EMS.API/Controllers/UsersController.cs. This API uses a
 * single-role-per-user model (`User.RoleId`) rather than the many-to-many
 * design shown elsewhere in database-design.md, so `roleId` is singular and
 * there is no dedicated roles-assignment endpoint — role assignment happens
 * via create/update. Gated to the `CanManageUsers` policy (Admin only).
 */
export interface UserRepository {
  list(filters?: UserListFilters): Promise<User[]>
  getById(id: string): Promise<User>
  create(input: CreateUserInput): Promise<User>
  update(input: UpdateUserInput): Promise<User>
  updateStatus(input: UpdateUserStatusInput): Promise<User>
  remove(id: string): Promise<void>
  restore(id: string): Promise<void>
}
