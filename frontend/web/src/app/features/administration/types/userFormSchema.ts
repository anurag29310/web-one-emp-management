import { z } from 'zod'
import { passwordPolicySchema } from '@/app/features/auth/types/passwordPolicy'

const usernameSchema = z
  .string()
  .min(1, 'Username is required.')
  .max(256, 'Username must be 256 characters or fewer.')

const emailSchema = z
  .string()
  .min(1, 'Email is required.')
  .max(256, 'Email must be 256 characters or fewer.')
  .email('Enter a valid email address.')

export const createUserFormSchema = z.object({
  userName: usernameSchema,
  email: emailSchema,
  temporaryPassword: passwordPolicySchema,
  roleId: z.string().optional().or(z.literal('')),
  employeeId: z.string().optional().or(z.literal('')),
  isActive: z.boolean(),
})

export type CreateUserFormValues = z.infer<typeof createUserFormSchema>

export const editUserFormSchema = z.object({
  userName: usernameSchema,
  email: emailSchema,
  roleId: z.string().optional().or(z.literal('')),
  employeeId: z.string().optional().or(z.literal('')),
})

export type EditUserFormValues = z.infer<typeof editUserFormSchema>
