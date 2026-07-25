import { delay } from '@/app/shared/utils/delay'
import { AppError } from '@/app/shared/models/appError'
import type {
  CreateEmployeeInput,
  Employee,
  EmployeeListFilters,
  UpdateEmployeeInput,
  UpdateEmployeeStatusInput,
} from '@/app/shared/models/employee'
import type { PagedResult } from '@/app/shared/models/apiEnvelope'
import type { EmployeeRepository } from './employeeRepository'
import { mockEmployees } from './mockData'
import { mockDepartments } from '@/app/features/departments/api/mockData'
import { mockDesignations } from '@/app/features/designations/api/mockData'
import { mockTeams } from '@/app/features/teams/api/mockData'
import { mockOfficeLocations } from '@/app/features/office-locations/api/mockData'

let employees = [...mockEmployees]

function nextId(): string {
  return `10000000-0000-0000-0000-${Date.now().toString().padStart(12, '0')}`
}

function lookupNames(input: CreateEmployeeInput | UpdateEmployeeInput) {
  const department = input.departmentId
    ? mockDepartments.find((d) => d.id === input.departmentId)
    : undefined
  const team = input.teamId ? mockTeams.find((t) => t.id === input.teamId) : undefined
  const designation = mockDesignations.find((d) => d.id === input.designationId)
  const officeLocation = mockOfficeLocations.find((o) => o.id === input.officeLocationId)
  return {
    departmentName: department?.name ?? null,
    teamName: team?.name ?? null,
    designationName: designation?.name ?? null,
    officeLocationName: officeLocation?.name ?? null,
  }
}

export const mockEmployeeRepository: EmployeeRepository = {
  async list(filters: EmployeeListFilters = {}): Promise<PagedResult<Employee>> {
    await delay(300)
    const { page = 1, pageSize = 20, search, departmentId, status } = filters

    let filtered = employees.filter((e) => !e.isDeleted)
    if (search) {
      const term = search.toLowerCase()
      filtered = filtered.filter(
        (e) =>
          e.fullName.toLowerCase().includes(term) || e.employeeCode.toLowerCase().includes(term),
      )
    }
    if (departmentId) {
      filtered = filtered.filter((e) => e.departmentId === departmentId)
    }
    if (status) {
      filtered = filtered.filter((e) => e.employmentStatus === status)
    }

    const start = (page - 1) * pageSize
    const pageItems = filtered.slice(start, start + pageSize)

    return {
      data: pageItems,
      page,
      pageSize,
      totalCount: filtered.length,
      totalPages: Math.max(1, Math.ceil(filtered.length / pageSize)),
      correlationId: 'mock-correlation-id',
    }
  },

  async getById(id: string): Promise<Employee> {
    await delay(200)
    const employee = employees.find((e) => e.id === id)
    if (!employee) {
      throw new AppError(`Employee ${id} was not found.`, 404, 'NOT_FOUND')
    }
    return employee
  },

  async create(input: CreateEmployeeInput): Promise<Employee> {
    await delay(300)
    if (employees.some((e) => e.employeeCode.toLowerCase() === input.employeeCode.toLowerCase())) {
      throw new AppError('Employee code already exists', 409, 'CONFLICT')
    }
    if (input.email && employees.some((e) => e.email?.toLowerCase() === input.email?.toLowerCase())) {
      throw new AppError('Email already exists', 409, 'CONFLICT')
    }
    const names = lookupNames(input)
    const employee: Employee = {
      id: nextId(),
      employeeCode: input.employeeCode,
      firstName: input.firstName,
      middleName: input.middleName ?? null,
      lastName: input.lastName,
      fullName: `${input.firstName} ${input.lastName}`,
      email: input.email ?? null,
      phoneNumber: input.phoneNumber ?? null,
      dateOfBirth: input.dateOfBirth ?? null,
      gender: input.gender ?? null,
      address: {
        addressLine1: input.address?.addressLine1 ?? null,
        addressLine2: input.address?.addressLine2 ?? null,
        city: input.address?.city ?? null,
        state: input.address?.state ?? null,
        postalCode: input.address?.postalCode ?? null,
        country: input.address?.country ?? null,
      },
      emergencyContact: {
        name: input.emergencyContact?.name ?? null,
        phone: input.emergencyContact?.phone ?? null,
        relation: input.emergencyContact?.relation ?? null,
      },
      joinDate: input.joinDate,
      exitDate: null,
      departmentId: input.departmentId ?? null,
      departmentName: names.departmentName,
      teamId: input.teamId ?? null,
      teamName: names.teamName,
      designationId: input.designationId,
      designationName: names.designationName,
      managerId: input.managerId ?? null,
      officeLocationId: input.officeLocationId,
      officeLocationName: names.officeLocationName,
      profilePhotoDocumentId: null,
      employmentStatus: input.employmentStatus ?? 'Active',
      isActive: true,
      isDeleted: false,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: null,
    }
    employees = [...employees, employee]
    return employee
  },

  async update(input: UpdateEmployeeInput): Promise<Employee> {
    await delay(300)
    const existing = employees.find((e) => e.id === input.id)
    if (!existing) {
      throw new AppError(`Employee ${input.id} was not found.`, 404, 'NOT_FOUND')
    }
    if (
      employees.some(
        (e) => e.id !== input.id && e.employeeCode.toLowerCase() === input.employeeCode.toLowerCase(),
      )
    ) {
      throw new AppError('Employee code already exists', 409, 'CONFLICT')
    }
    if (
      input.email &&
      employees.some((e) => e.id !== input.id && e.email?.toLowerCase() === input.email?.toLowerCase())
    ) {
      throw new AppError('Email already exists', 409, 'CONFLICT')
    }
    const names = lookupNames(input)
    const updated: Employee = {
      ...existing,
      employeeCode: input.employeeCode,
      firstName: input.firstName,
      middleName: input.middleName ?? null,
      lastName: input.lastName,
      fullName: `${input.firstName} ${input.lastName}`,
      email: input.email ?? null,
      phoneNumber: input.phoneNumber ?? null,
      dateOfBirth: input.dateOfBirth ?? null,
      gender: input.gender ?? null,
      address: {
        addressLine1: input.address?.addressLine1 ?? null,
        addressLine2: input.address?.addressLine2 ?? null,
        city: input.address?.city ?? null,
        state: input.address?.state ?? null,
        postalCode: input.address?.postalCode ?? null,
        country: input.address?.country ?? null,
      },
      emergencyContact: {
        name: input.emergencyContact?.name ?? null,
        phone: input.emergencyContact?.phone ?? null,
        relation: input.emergencyContact?.relation ?? null,
      },
      joinDate: input.joinDate,
      departmentId: input.departmentId ?? null,
      departmentName: names.departmentName,
      teamId: input.teamId ?? null,
      teamName: names.teamName,
      designationId: input.designationId,
      designationName: names.designationName,
      managerId: input.managerId ?? null,
      officeLocationId: input.officeLocationId,
      officeLocationName: names.officeLocationName,
      employmentStatus: input.employmentStatus ?? existing.employmentStatus,
      updatedAtUtc: new Date().toISOString(),
    }
    employees = employees.map((e) => (e.id === input.id ? updated : e))
    return updated
  },

  async updateStatus(input: UpdateEmployeeStatusInput): Promise<void> {
    await delay(200)
    const existing = employees.find((e) => e.id === input.id)
    if (!existing) {
      throw new AppError(`Employee ${input.id} was not found.`, 404, 'NOT_FOUND')
    }
    const updated: Employee = {
      ...existing,
      employmentStatus: input.status,
      exitDate: input.exitDate ?? existing.exitDate,
      isActive: input.status === 'Active',
      updatedAtUtc: new Date().toISOString(),
    }
    employees = employees.map((e) => (e.id === input.id ? updated : e))
  },

  async remove(id: string): Promise<void> {
    await delay(200)
    employees = employees.map((e) => (e.id === id ? { ...e, isDeleted: true } : e))
  },

  async restore(id: string): Promise<void> {
    await delay(200)
    employees = employees.map((e) => (e.id === id ? { ...e, isDeleted: false } : e))
  },
}
