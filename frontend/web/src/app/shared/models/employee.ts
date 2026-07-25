export type EmploymentStatus = 'Active' | 'Inactive' | 'OnLeave' | 'Terminated'

export interface EmployeeAddress {
  addressLine1: string | null
  addressLine2: string | null
  city: string | null
  state: string | null
  postalCode: string | null
  country: string | null
}

export interface EmployeeEmergencyContact {
  name: string | null
  phone: string | null
  relation: string | null
}

export interface Employee {
  id: string
  employeeCode: string
  firstName: string
  middleName: string | null
  lastName: string
  fullName: string
  email: string | null
  phoneNumber: string | null
  dateOfBirth: string | null
  gender: string | null
  address: EmployeeAddress
  emergencyContact: EmployeeEmergencyContact
  joinDate: string
  exitDate: string | null
  departmentId: string | null
  departmentName: string | null
  teamId: string | null
  teamName: string | null
  designationId: string
  designationName: string | null
  managerId: string | null
  officeLocationId: string
  officeLocationName: string | null
  profilePhotoDocumentId: string | null
  employmentStatus: EmploymentStatus | null
  isActive: boolean
  isDeleted: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface EmployeeListFilters {
  page?: number
  pageSize?: number
  search?: string
  departmentId?: string
  teamId?: string
  designationId?: string
  officeLocationId?: string
  status?: EmploymentStatus
  joinDateFrom?: string
  joinDateTo?: string
}

/** Mirrors backend/EMS.Application/Features/Employees/Commands/CreateEmployeeCommand.cs. */
export interface EmployeeWriteInput {
  employeeCode: string
  firstName: string
  middleName?: string
  lastName: string
  email?: string
  phoneNumber?: string
  dateOfBirth?: string
  gender?: string
  address?: Partial<EmployeeAddress>
  emergencyContact?: Partial<EmployeeEmergencyContact>
  joinDate: string
  departmentId?: string
  teamId?: string
  designationId: string
  managerId?: string
  officeLocationId: string
  employmentStatus?: EmploymentStatus
}

export type CreateEmployeeInput = EmployeeWriteInput

export interface UpdateEmployeeInput extends EmployeeWriteInput {
  id: string
}

/** Mirrors backend UpdateEmployeeStatusCommand.cs. */
export interface UpdateEmployeeStatusInput {
  id: string
  status: EmploymentStatus
  exitDate?: string
  reason?: string
}
