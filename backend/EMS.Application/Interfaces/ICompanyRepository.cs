using EMS.Domain.Entities;
using EMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface ICompanyRepository
    {
        Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Company?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Company>> GetAllAsync(int page, int pageSize, CompanyStatus? status, string? search, CancellationToken ct = default);
        Task<int> CountAsync(CompanyStatus? status, string? search, CancellationToken ct = default);
        Task AddAsync(Company company, CancellationToken ct = default);
        Task UpdateAsync(Company company, CancellationToken ct = default);
        Task DeleteAsync(Company company, CancellationToken ct = default);
        Task RestoreAsync(Company company, CancellationToken ct = default);
        Task<int> GetEmployeeCountAsync(Guid companyId, CancellationToken ct = default);
        Task<IEnumerable<User>> GetAdminsAsync(Guid companyId, CancellationToken ct = default);
        Task<int> GetTotalEmployeeCountAcrossAllCompaniesAsync(CancellationToken ct = default);
        Task<IReadOnlyDictionary<CompanyStatus, int>> GetStatusCountsAsync(CancellationToken ct = default);
        Task<IEnumerable<Company>> GetRecentRegistrationsAsync(int count, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
