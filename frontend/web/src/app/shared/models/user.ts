export type Role = 'Admin' | 'HR' | 'Manager' | 'Employee'

export interface AuthenticatedUser {
  id: string
  userName: string
  email: string
  role: Role | null
  isActive: boolean
  isMfaEnabled: boolean
}

export interface AuthSession {
  accessToken: string
  refreshToken: string
  expiresAtUtc: string
  user: AuthenticatedUser
}
