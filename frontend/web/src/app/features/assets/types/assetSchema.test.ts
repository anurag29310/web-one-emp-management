import { describe, expect, it } from 'vitest'
import {
  assetFormSchema,
  assetStatusChangeFormSchema,
  assignAssetFormSchema,
  returnAssetFormSchema,
} from './assetSchema'

describe('assetFormSchema', () => {
  const validInput = {
    category: 'Laptop',
    brand: 'Dell',
    model: 'Latitude 5440',
    serialNumber: 'SN-88213X',
    purchaseDate: '2026-01-15',
    purchaseCost: 1200,
    notes: '',
  }

  it('accepts a fully valid asset', () => {
    const result = assetFormSchema.safeParse(validInput)
    expect(result.success).toBe(true)
  })

  it('coerces a string purchase cost from a number input field', () => {
    const result = assetFormSchema.safeParse({ ...validInput, purchaseCost: '1200' })
    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.data.purchaseCost).toBe(1200)
    }
  })

  it.each(['category', 'brand', 'model', 'serialNumber', 'purchaseDate'] as const)(
    'rejects a blank %s',
    (field) => {
      const result = assetFormSchema.safeParse({ ...validInput, [field]: '' })
      expect(result.success).toBe(false)
    },
  )

  it('rejects a negative purchase cost', () => {
    const result = assetFormSchema.safeParse({ ...validInput, purchaseCost: -1 })
    expect(result.success).toBe(false)
  })
})

describe('assignAssetFormSchema', () => {
  it('requires an employee to be selected', () => {
    const result = assignAssetFormSchema.safeParse({ employeeId: '' })
    expect(result.success).toBe(false)
  })

  it('accepts an employee id with all other fields omitted', () => {
    const result = assignAssetFormSchema.safeParse({ employeeId: 'employee-1' })
    expect(result.success).toBe(true)
  })
})

describe('returnAssetFormSchema', () => {
  it('requires a valid resulting status', () => {
    const result = returnAssetFormSchema.safeParse({ resultingAssetStatus: 'Assigned' })
    expect(result.success).toBe(false)
  })

  it('accepts a valid resulting status', () => {
    const result = returnAssetFormSchema.safeParse({ resultingAssetStatus: 'Available' })
    expect(result.success).toBe(true)
  })
})

describe('assetStatusChangeFormSchema', () => {
  it('rejects Assigned as a manual status change target', () => {
    const result = assetStatusChangeFormSchema.safeParse({ status: 'Assigned' })
    expect(result.success).toBe(false)
  })

  it('accepts a non-Assigned status', () => {
    const result = assetStatusChangeFormSchema.safeParse({ status: 'Lost' })
    expect(result.success).toBe(true)
  })
})
