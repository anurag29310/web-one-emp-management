using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IDocumentRepository
    {
        Task AddAsync(EmployeeDocument doc);
        Task<EmployeeDocument?> GetByIdAsync(Guid id);
        Task<IEnumerable<EmployeeDocument>> GetForEmployeeAsync(Guid employeeId, int page, int pageSize, string? search, string? documentType);
        Task SoftDeleteAsync(EmployeeDocument doc);
        Task SaveChangesAsync();
    }
}
