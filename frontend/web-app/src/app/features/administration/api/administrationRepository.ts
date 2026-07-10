import type { Role } from '@/app/shared/models/user'

export interface ManagedUser {
  id: string
  userName: string
  email: string
  role: Role
  isActive: boolean
}

/**
 * Contract only for now — no mock/API implementation yet, and no backend
 * users/roles endpoints are exposed on EMS.API yet (only application-layer
 * Features/Users). Follow the pattern in features/employees/api once both
 * are ready.
 */
export interface AdministrationRepository {
  listUsers(): Promise<ManagedUser[]>
  setUserStatus(userId: string, isActive: boolean): Promise<ManagedUser>
}
