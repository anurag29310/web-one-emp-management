import { describe, expect, it } from 'vitest'
import { companyFormSchema } from './companySchema'

describe('companyFormSchema', () => {
  const validInput = { name: 'Acme Corp', timezone: 'UTC', currency: 'USD', logoUrl: '' }

  it('accepts a fully valid company', () => {
    const result = companyFormSchema.safeParse(validInput)
    expect(result.success).toBe(true)
  })

  it('accepts a valid logo URL', () => {
    const result = companyFormSchema.safeParse({ ...validInput, logoUrl: 'https://example.com/logo.png' })
    expect(result.success).toBe(true)
  })

  it('rejects a non-URL logo value', () => {
    const result = companyFormSchema.safeParse({ ...validInput, logoUrl: 'not-a-url' })
    expect(result.success).toBe(false)
  })

  it.each(['name', 'timezone', 'currency'] as const)('rejects a blank %s', (field) => {
    const result = companyFormSchema.safeParse({ ...validInput, [field]: '' })
    expect(result.success).toBe(false)
  })
})
