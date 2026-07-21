export type EmploymentStatus = 'Active' | 'Inactive' | 'OnLeave' | 'Terminated'

export interface Employee {
  id: string
  employeeCode: string
  firstName: string
  lastName: string
  fullName: string
  email: string | null
  phoneNumber: string | null
  dateOfBirth: string | null
  gender: string | null
  address: string | null
  emergencyContactName: string | null
  emergencyContactNumber: string | null
  joinDate: string
  exitDate: string | null
  departmentId: string | null
  departmentName: string | null
  designation: string | null
  managerId: string | null
  profilePhotoUrl: string | null
  employmentStatus: EmploymentStatus | null
  isActive: boolean
}

export interface EmployeeListFilters {
  page?: number
  pageSize?: number
  search?: string
  departmentId?: string
  status?: EmploymentStatus
}
