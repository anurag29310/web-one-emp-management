import type { AuthSession, AuthenticatedUser } from '@/app/shared/models/user'

export interface LoginCredentials {
  userNameOrEmail: string
  password: string
}

/**
 * Result of POST /auth/login. When the account has MFA enabled the server
 * returns a challenge instead of tokens; the caller must complete it via
 * verifyMfa() before a session exists.
 */
export type LoginOutcome =
  | { requiresMfa: false; session: AuthSession }
  | { requiresMfa: true; mfaChallengeId: string }

export interface VerifyMfaCredentials {
  mfaChallengeId: string
  /** A 6-digit TOTP code from the authenticator app, or a recovery code (XXXXX-XXXXX) — either is accepted. */
  code: string
}

export interface RegisterInput {
  userName: string
  email: string
  password: string
}

export interface ForgotPasswordInput {
  email: string
}

export interface ResetPasswordInput {
  email: string
  resetToken: string
  newPassword: string
}

export interface ChangePasswordInput {
  currentPassword: string
  newPassword: string
}

export interface MfaSetupInfo {
  manualEntryKey: string
  otpAuthUri: string
}

export interface MfaRecoveryCodes {
  recoveryCodes: string[]
}

export interface DisableMfaInput {
  password: string
}

export interface RegenerateRecoveryCodesInput {
  password: string
}

/**
 * The contract every page/hook depends on. LoginPage, AuthContext, and any
 * future consumer talk only to this interface — never to the mock or API
 * implementation directly — so they cannot tell which data source is active.
 */
export interface AuthRepository {
  login(credentials: LoginCredentials): Promise<LoginOutcome>
  /** Completes a login that returned requiresMfa: true. */
  verifyMfa(credentials: VerifyMfaCredentials): Promise<AuthSession>
  logout(): Promise<void>
  /** Re-establishes a session from a persisted refresh token on app load, or resolves to null if none exists / it's no longer valid. */
  restoreSession(): Promise<AuthSession | null>
  register(input: RegisterInput): Promise<AuthSession>
  /** Always resolves — never reveals whether the email belongs to an account. */
  forgotPassword(input: ForgotPasswordInput): Promise<void>
  resetPassword(input: ResetPasswordInput): Promise<void>
  changePassword(input: ChangePasswordInput): Promise<void>
  getCurrentUser(): Promise<AuthenticatedUser>
  /** Starts MFA enrollment; does not enable MFA until confirmed via enableMfa(). */
  setupMfa(): Promise<MfaSetupInfo>
  /** Confirms enrollment and turns MFA on. Returns 10 one-time recovery codes. */
  enableMfa(code: string): Promise<MfaRecoveryCodes>
  disableMfa(input: DisableMfaInput): Promise<void>
  /** Invalidates all existing recovery codes and issues 10 new ones. */
  regenerateMfaRecoveryCodes(input: RegenerateRecoveryCodesInput): Promise<MfaRecoveryCodes>
}
