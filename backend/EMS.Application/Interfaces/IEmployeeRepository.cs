using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IEmployeeRepository
    {
        // Note: GetByIdAsync and the lookup/hierarchy methods below deliberately do NOT take a
        // companyId — they're consumed by ~25 other modules (Attendance, Leave, Payroll, Tasks,
        // Reimbursements, Recruitment, Assets, Performance, Messaging, ...) that aren't
        // company-scoped yet in this phase. Every handler within the Employees module itself
        // that uses GetByIdAsync/GetByIdIncludingDeletedAsync compensates with an explicit
        // post-fetch `emp.CompanyId == currentUser.CompanyId` check (treated as not-found on
        // mismatch, matching this codebase's existing "404 not 403" ownership-check convention)
        // rather than changing this shared signature. See docs/database-design.md's multi-tenancy
        // notes for the full rationale — this is the one deliberate scoping gap in this phase.
        Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Employee?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Employee>> GetAllAsync(Guid companyId, int page, int pageSize, string? search, string? sortBy, string? sortDir, Guid? departmentId, string? status, Guid? teamId = null, Guid? designationId = null, Guid? officeLocationId = null, CancellationToken ct = default);
        Task<int> CountAsync(Guid companyId, string? search, Guid? departmentId, string? status, Guid? teamId = null, Guid? designationId = null, Guid? officeLocationId = null, CancellationToken ct = default);
        Task<IEnumerable<Employee>> GetAllForExportAsync(Guid companyId, string? search, string? sortBy, string? sortDir, Guid? departmentId, string? status, Guid? teamId = null, Guid? designationId = null, Guid? officeLocationId = null, CancellationToken ct = default);
        Task<IEnumerable<Employee>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
        Task<IEnumerable<Employee>> GetByDepartmentAsync(Guid departmentId, int page, int pageSize, CancellationToken ct = default);
        Task<IEnumerable<Employee>> GetByTeamAsync(Guid teamId, int page, int pageSize, CancellationToken ct = default);
        Task<IEnumerable<Employee>> GetDirectReportsAsync(Guid managerId, int page, int pageSize, CancellationToken ct = default);
        Task<IEnumerable<Guid>> GetDirectReportIdsAsync(Guid managerId, CancellationToken ct = default);
        Task<bool> IsDirectReportAsync(Guid managerId, Guid employeeId, CancellationToken ct = default);
        Task<IEnumerable<Employee>> GetManagerChainAsync(Guid employeeId, CancellationToken ct = default);
        Task AddAsync(Employee employee, CancellationToken ct = default);
        Task UpdateAsync(Employee employee, CancellationToken ct = default);
        Task DeleteAsync(Employee employee, CancellationToken ct = default);
        Task RestoreAsync(Employee employee, CancellationToken ct = default);
        Task<bool> EmailExistsAsync(string email, Guid companyId, Guid? excludeId = null, CancellationToken ct = default);
        Task<bool> EmployeeCodeExistsAsync(string code, Guid companyId, Guid? excludeId = null, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
