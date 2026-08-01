import { describe, expect, it } from 'vitest'
import { companyRegistrationSchema } from './companyRegistrationSchema'

describe('companyRegistrationSchema', () => {
  const validInput = {
    companyName: 'Acme Corp',
    timezone: 'UTC',
    currency: 'USD',
    adminUserName: 'admin',
    adminEmail: 'admin@acme.example.com',
    adminPassword: 'Password@123',
    confirmPassword: 'Password@123',
  }

  it('accepts a fully valid registration', () => {
    const result = companyRegistrationSchema.safeParse(validInput)
    expect(result.success).toBe(true)
  })

  it('rejects mismatched passwords', () => {
    const result = companyRegistrationSchema.safeParse({ ...validInput, confirmPassword: 'Different@123' })
    expect(result.success).toBe(false)
  })

  it('rejects a password that fails the complexity policy', () => {
    const result = companyRegistrationSchema.safeParse({
      ...validInput,
      adminPassword: 'password',
      confirmPassword: 'password',
    })
    expect(result.success).toBe(false)
  })

  it('rejects an invalid admin email', () => {
    const result = companyRegistrationSchema.safeParse({ ...validInput, adminEmail: 'not-an-email' })
    expect(result.success).toBe(false)
  })

  it('rejects a blank company name', () => {
    const result = companyRegistrationSchema.safeParse({ ...validInput, companyName: '' })
    expect(result.success).toBe(false)
  })
})
