import { z } from 'zod'

// Auth Types
export const AuthenticatedUserSchema = z.object({
  id: z.string().uuid(),
  employeeId: z.string().uuid(),
  displayName: z.string(),
  email: z.string().email(),
  roles: z.array(z.string()),
})

export type AuthenticatedUser = z.infer<typeof AuthenticatedUserSchema>

export const LoginCredentialsSchema = z.object({
  userNameOrEmail: z.string().min(1, 'Username or email is required'),
  password: z.string().min(1, 'Password is required'),
})

export type LoginCredentials = z.infer<typeof LoginCredentialsSchema>

export const SessionSchema = z.object({
  accessToken: z.string(),
  refreshToken: z.string(),
  expiresAtUtc: z.string().datetime(),
  requiresMfa: z.boolean(),
  user: AuthenticatedUserSchema,
})

export type Session = z.infer<typeof SessionSchema>

// API Response Types
export const ApiResponseSchema = z.object({
  data: z.unknown(),
  message: z.string().optional(),
  correlationId: z.string().optional(),
})

export type ApiResponse<T> = {
  data: T
  message?: string
  correlationId?: string
}

export const ApiListResponseSchema = z.object({
  data: z.array(z.unknown()),
  page: z.number(),
  pageSize: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
  correlationId: z.string().optional(),
})

export type ApiListResponse<T> = {
  data: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  correlationId?: string
}

export const ApiErrorResponseSchema = z.object({
  status: z.number(),
  code: z.string(),
  message: z.string(),
  errors: z.array(
    z.object({
      field: z.string(),
      message: z.string(),
    }),
  ),
  correlationId: z.string().optional(),
})

export type ApiErrorResponse = z.infer<typeof ApiErrorResponseSchema>

// Employee Types
export const EmployeeSchema = z.object({
  id: z.string().uuid(),
  employeeId: z.string(),
  displayName: z.string(),
  email: z.string().email(),
  phone: z.string().optional(),
  jobTitle: z.string(),
  department: z.string(),
  status: z.enum(['Active', 'InActive', 'OnLeave', 'Resigned']),
  joinDate: z.string().datetime(),
  profilePhotoUrl: z.string().optional(),
})

export type Employee = z.infer<typeof EmployeeSchema>

// Department Types
export const DepartmentSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  description: z.string().optional(),
  headId: z.string().uuid().optional(),
  createdAt: z.string().datetime(),
})

export type Department = z.infer<typeof DepartmentSchema>

// Leave Types
export const LeaveSchema = z.object({
  id: z.string().uuid(),
  employeeId: z.string().uuid(),
  leaveType: z.string(),
  startDate: z.string().date(),
  endDate: z.string().date(),
  status: z.enum(['Pending', 'Approved', 'Rejected', 'Cancelled']),
  reason: z.string().optional(),
  appliedDate: z.string().datetime(),
})

export type Leave = z.infer<typeof LeaveSchema>
