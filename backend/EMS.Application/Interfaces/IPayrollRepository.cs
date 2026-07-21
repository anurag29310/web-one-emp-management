using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IPayrollRepository
    {
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<SalaryStructure?> GetEffectiveSalaryStructureAsync(Guid employeeId, DateTime asOf);
        Task CreatePayrollRunAsync(PayrollRun run);
        Task SavePayslipAsync(Payslip payslip);
        Task<Payslip?> GetPayslipByIdAsync(Guid id);
        // Salary structure CRUD
        Task CreateSalaryStructureAsync(SalaryStructure structure);
        Task<SalaryStructure?> GetSalaryStructureByIdAsync(Guid id);
        Task<IEnumerable<SalaryStructure>> GetSalaryStructuresAsync();
        Task UpdateSalaryStructureAsync(SalaryStructure structure);
        Task<bool> DeleteSalaryStructureAsync(Guid id);

        // Payroll runs
        Task<IEnumerable<PayrollRun>> GetPayrollRunsAsync();
        Task<PayrollRun?> GetPayrollRunByIdAsync(Guid id);
        Task UpdatePayrollRunAsync(PayrollRun run);

        // Payslip listings
        Task<IEnumerable<Payslip>> GetPayslipsForEmployeeAsync(Guid employeeId);
        Task SaveChangesAsync();
    }
}
