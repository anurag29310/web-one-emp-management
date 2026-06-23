using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<Department?> GetByIdAsync(Guid id);
        Task<IEnumerable<Department>> GetAllAsync();
        Task AddAsync(Department department);
        Task UpdateAsync(Department department);
        Task DeleteAsync(Department department);
        Task SaveChangesAsync();
    }
}
