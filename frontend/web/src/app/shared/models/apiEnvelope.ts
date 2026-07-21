export interface ApiSuccessEnvelope<T> {
  data: T
  message: string
  correlationId: string
}

export interface PagedResult<T> {
  data: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  correlationId: string
}

export interface ApiErrorEnvelope {
  status: number
  code: string
  message: string
  errors?: unknown
  correlationId: string
}
