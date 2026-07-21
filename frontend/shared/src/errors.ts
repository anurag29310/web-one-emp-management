/**
 * Shared application error class
 */
export class AppError extends Error {
  constructor(
    message: string,
    public status?: number,
    public code?: string,
    public details?: Record<string, unknown>,
  ) {
    super(message)
    this.name = 'AppError'
  }
}

/**
 * Validation error for form fields
 */
export class ValidationError extends AppError {
  constructor(
    message: string,
    public fields: Record<string, string> = {},
  ) {
    super(message, 400, 'VALIDATION_ERROR')
    this.name = 'ValidationError'
  }
}

/**
 * Authentication error
 */
export class AuthenticationError extends AppError {
  constructor(message: string = 'Authentication failed') {
    super(message, 401, 'UNAUTHORIZED')
    this.name = 'AuthenticationError'
  }
}

/**
 * Authorization error
 */
export class AuthorizationError extends AppError {
  constructor(message: string = 'You do not have permission to access this resource') {
    super(message, 403, 'FORBIDDEN')
    this.name = 'AuthorizationError'
  }
}

/**
 * Not found error
 */
export class NotFoundError extends AppError {
  constructor(message: string = 'Resource not found') {
    super(message, 404, 'NOT_FOUND')
    this.name = 'NotFoundError'
  }
}
