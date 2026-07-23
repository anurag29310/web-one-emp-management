import type { AuthenticatedUser } from '@/app/shared/models/user'

export interface MockAccountMfaState {
  enabled: boolean
  /** Confirmed TOTP secret once MFA is enabled — cleared on disable. */
  secret: string | null
  /** Secret from setupMfa() awaiting confirmation via enableMfa() — cleared once confirmed. */
  pendingSecret: string | null
  /** Unused recovery codes still valid for login (mock-only; the real backend stores hashes). */
  recoveryCodes: string[]
}

export interface MockAccount {
  password: string
  user: AuthenticatedUser
  mfa: MockAccountMfaState
}

export const mockAccounts: MockAccount[] = [
  {
    password: 'Admin@123',
    user: {
      id: '00000000-0000-0000-0000-000000000001',
      userName: 'admin',
      email: 'admin@ems.local',
      role: 'Admin',
      isActive: true,
      isMfaEnabled: false,
    },
    mfa: { enabled: false, secret: null, pendingSecret: null, recoveryCodes: [] },
  },
  {
    password: 'Hr@12345',
    user: {
      id: '00000000-0000-0000-0000-000000000002',
      userName: 'hr.user',
      email: 'hr@ems.local',
      role: 'HR',
      isActive: true,
      isMfaEnabled: false,
    },
    mfa: { enabled: false, secret: null, pendingSecret: null, recoveryCodes: [] },
  },
  {
    password: 'Manager@123',
    user: {
      id: '00000000-0000-0000-0000-000000000003',
      userName: 'manager',
      email: 'manager@ems.local',
      role: 'Manager',
      isActive: true,
      // Seeded with MFA already on so the login-challenge screen is exercisable in mock mode
      // without an enrollment step first. Any 6-digit code is accepted (see mockAuthRepository),
      // and the recovery code below also works and is consumed on use.
      isMfaEnabled: true,
    },
    mfa: {
      enabled: true,
      secret: 'MOCKMFASECRETKEY',
      pendingSecret: null,
      recoveryCodes: ['K7M9X-4RT2W', 'Q4XN8-7WKPT'],
    },
  },
  {
    password: 'Employee@123',
    user: {
      id: '00000000-0000-0000-0000-000000000004',
      userName: 'employee',
      email: 'employee@ems.local',
      role: 'Employee',
      isActive: true,
      isMfaEnabled: false,
    },
    mfa: { enabled: false, secret: null, pendingSecret: null, recoveryCodes: [] },
  },
]
