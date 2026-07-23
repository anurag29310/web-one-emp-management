using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Employee?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Employee>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, string? sortDir, Guid? departmentId, string? status, CancellationToken ct = default);
        Task<int> CountAsync(string? search, Guid? departmentId, string? status, CancellationToken ct = default);
        Task<IEnumerable<Employee>> GetAllForExportAsync(string? search, string? sortBy, string? sortDir, Guid? departmentId, string? status, CancellationToken ct = default);
        Task<IEnumerable<Employee>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
        Task<IEnumerable<Employee>> GetByDepartmentAsync(Guid departmentId, int page, int pageSize, CancellationToken ct = default);
        Task<IEnumerable<Employee>> GetDirectReportsAsync(Guid managerId, int page, int pageSize, CancellationToken ct = default);
        Task<IEnumerable<Guid>> GetDirectReportIdsAsync(Guid managerId, CancellationToken ct = default);
        Task<bool> IsDirectReportAsync(Guid managerId, Guid employeeId, CancellationToken ct = default);
        Task<IEnumerable<Employee>> GetManagerChainAsync(Guid employeeId, CancellationToken ct = default);
        Task AddAsync(Employee employee, CancellationToken ct = default);
        Task UpdateAsync(Employee employee, CancellationToken ct = default);
        Task DeleteAsync(Employee employee, CancellationToken ct = default);
        Task RestoreAsync(Employee employee, CancellationToken ct = default);
        Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken ct = default);
        Task<bool> EmployeeCodeExistsAsync(string code, Guid? excludeId = null, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
