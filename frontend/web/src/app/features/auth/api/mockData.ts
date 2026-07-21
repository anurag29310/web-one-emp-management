import type { AuthenticatedUser } from '@/app/shared/models/user'

export interface MockAccount {
  password: string
  user: AuthenticatedUser
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
    },
  },
  {
    password: 'Hr@12345',
    user: {
      id: '00000000-0000-0000-0000-000000000002',
      userName: 'hr.user',
      email: 'hr@ems.local',
      role: 'HR',
      isActive: true,
    },
  },
  {
    password: 'Manager@123',
    user: {
      id: '00000000-0000-0000-0000-000000000003',
      userName: 'manager',
      email: 'manager@ems.local',
      role: 'Manager',
      isActive: true,
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
    },
  },
]
