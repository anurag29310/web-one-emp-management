import { z } from 'zod'
import { passwordPolicySchema } from './passwordPolicy'

export const mfaChallengeSchema = z.object({
  // POST /auth/mfa/verify accepts either a 6-digit TOTP code or an
  // XXXXX-XXXXX recovery code in the same field — validation stays loose
  // here and the server is the source of truth on which one it was.
  code: z
    .string()
    .min(1, 'Enter the 6-digit code from your authenticator app or a recovery code.')
    .max(20, 'Code is too long.'),
})
export type MfaChallengeFormValues = z.infer<typeof mfaChallengeSchema>

export const registerSchema = z
  .object({
    userName: z
      .string()
      .min(3, 'Username must be at least 3 characters long.')
      .max(50, 'Username must be 50 characters or fewer.'),
    email: z.string().min(1, 'Email is required.').email('Enter a valid email address.'),
    password: passwordPolicySchema,
    confirmPassword: z.string().min(1, 'Confirm your password.'),
  })
  .refine((values) => values.password === values.confirmPassword, {
    message: 'Passwords do not match.',
    path: ['confirmPassword'],
  })
export type RegisterFormValues = z.infer<typeof registerSchema>

export const forgotPasswordSchema = z.object({
  email: z.string().min(1, 'Email is required.').email('Enter a valid email address.'),
})
export type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>

export const resetPasswordSchema = z
  .object({
    email: z.string().min(1, 'Email is required.').email('Enter a valid email address.'),
    resetToken: z.string().min(1, 'Reset code is required.'),
    newPassword: passwordPolicySchema,
    confirmNewPassword: z.string().min(1, 'Confirm your new password.'),
  })
  .refine((values) => values.newPassword === values.confirmNewPassword, {
    message: 'Passwords do not match.',
    path: ['confirmNewPassword'],
  })
export type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Current password is required.'),
    newPassword: passwordPolicySchema,
    confirmNewPassword: z.string().min(1, 'Confirm your new password.'),
  })
  .refine((values) => values.newPassword !== values.currentPassword, {
    message: 'New password must be different from current password.',
    path: ['newPassword'],
  })
  .refine((values) => values.newPassword === values.confirmNewPassword, {
    message: 'Passwords do not match.',
    path: ['confirmNewPassword'],
  })
export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>

export const mfaEnableCodeSchema = z.object({
  code: z
    .string()
    .min(1, 'Enter the 6-digit code from your authenticator app.')
    .regex(/^\d{6}$/, 'Code must be exactly 6 digits.'),
})
export type MfaEnableCodeFormValues = z.infer<typeof mfaEnableCodeSchema>

export const passwordConfirmSchema = z.object({
  password: z.string().min(1, 'Password is required.'),
})
export type PasswordConfirmFormValues = z.infer<typeof passwordConfirmSchema>
