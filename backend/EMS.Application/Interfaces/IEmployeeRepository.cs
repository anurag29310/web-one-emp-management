using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<Employee?> GetByIdAsync(Guid id);
        Task<IEnumerable<Employee>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, string? sortDir, Guid? departmentId, string? status);
        Task<IEnumerable<Employee>> GetByDepartmentAsync(Guid departmentId, int page, int pageSize);
        Task<IEnumerable<Employee>> GetReportingEmployeesAsync(Guid managerId, int page, int pageSize);
        Task AddAsync(Employee employee);
        Task UpdateAsync(Employee employee);
        Task DeleteAsync(Employee employee);
        Task<bool> EmailExistsAsync(string email, Guid? excludeId = null);
        Task<bool> EmployeeCodeExistsAsync(string code, Guid? excludeId = null);
        Task SaveChangesAsync();
    }
}
