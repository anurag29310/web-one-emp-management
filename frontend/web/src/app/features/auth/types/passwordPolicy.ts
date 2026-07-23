import { z } from 'zod'

/**
 * Mirrors the password complexity rule enforced server-side by
 * ChangePasswordCommandValidator / ResetPasswordCommandValidator
 * (backend/EMS.Application/Features/Auth/Validators/*.cs): at least 8
 * characters, one uppercase letter, one lowercase letter, one digit, and one
 * special character. Register (POST /auth/register) enforces the same
 * policy — see docs/api-specification.md section 3.10.
 */
export const PASSWORD_POLICY_RULES: readonly string[] = [
  'At least 8 characters',
  'At least one uppercase letter (A-Z)',
  'At least one lowercase letter (a-z)',
  'At least one digit (0-9)',
  'At least one special character (e.g. !@#$%^&*)',
]

export const passwordPolicySchema = z
  .string()
  .min(8, 'Password must be at least 8 characters long.')
  .regex(/[A-Z]/, 'Password must contain at least one uppercase letter.')
  .regex(/[a-z]/, 'Password must contain at least one lowercase letter.')
  .regex(/[0-9]/, 'Password must contain at least one digit.')
  .regex(/[^a-zA-Z0-9]/, 'Password must contain at least one special character.')
