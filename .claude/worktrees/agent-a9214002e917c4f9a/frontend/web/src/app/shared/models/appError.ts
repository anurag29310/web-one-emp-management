export class AppError extends Error {
  readonly status: number
  readonly code: string

  constructor(message: string, status: number, code: string = 'UNKNOWN_ERROR') {
    super(message)
    this.name = 'AppError'
    this.status = status
    this.code = code
  }
}
