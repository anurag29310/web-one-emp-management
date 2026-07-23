export interface Role {
  id: string
  name: string
  description: string | null
  isDeleted: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface RoleListFilters {
  includeDeleted?: boolean
}

/**
 * Backend contract: docs/api-specification.md section 4.2, cross-checked
 * against backend/EMS.API/Controllers/RolesController.cs. `GET /roles` is
 * gated to `CanViewRoles` (Admin, HR); all other endpoints are Admin-only
 * via `CanManageUsers`. The web app currently only needs the read-only
 * list/detail views, so only those are wired up on the frontend.
 */
export interface RoleRepository {
  list(filters?: RoleListFilters): Promise<Role[]>
  getById(id: string): Promise<Role>
}
