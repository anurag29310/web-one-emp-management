using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class PayrollRepository : IPayrollRepository
    {
        private readonly ApplicationDbContext _db;

        public PayrollRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
            => await _db.Employees.AsNoTracking().Where(e => e.IsActive).ToListAsync();

        public async Task<SalaryStructure?> GetEffectiveSalaryStructureAsync(Guid employeeId, DateTime asOf)
        {
            return await _db.SalaryStructures
                .Include(s => s.Allowances)
                .Include(s => s.Deductions)
                .Where(s => s.EmployeeId == employeeId && s.EffectiveFrom <= asOf && (s.EffectiveTo == null || s.EffectiveTo >= asOf))
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefaultAsync();
        }

        public async Task CreatePayrollRunAsync(PayrollRun run)
        {
            await _db.PayrollRuns.AddAsync(run);
        }

        public async Task CreateSalaryStructureAsync(SalaryStructure structure)
        {
            await _db.SalaryStructures.AddAsync(structure);
        }

        public async Task<SalaryStructure?> GetSalaryStructureByIdAsync(Guid id)
        {
            return await _db.SalaryStructures.Include(s => s.Allowances).Include(s => s.Deductions).FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<SalaryStructure>> GetSalaryStructuresAsync()
        {
            return await _db.SalaryStructures.Include(s => s.Allowances).Include(s => s.Deductions).AsNoTracking().ToListAsync();
        }

        public async Task UpdateSalaryStructureAsync(SalaryStructure structure)
        {
            _db.SalaryStructures.Update(structure);
            await Task.CompletedTask;
        }

        public async Task DeleteSalaryStructureAsync(Guid id)
        {
            var s = await _db.SalaryStructures.FindAsync(id);
            if (s != null) _db.SalaryStructures.Remove(s);
        }

        public async Task SavePayslipAsync(Payslip payslip)
        {
            await _db.Payslips.AddAsync(payslip);
        }

        public async Task<Payslip?> GetPayslipByIdAsync(Guid id)
            => await _db.Payslips.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

        public async Task<IEnumerable<PayrollRun>> GetPayrollRunsAsync()
        {
            return await _db.PayrollRuns.Include(r => r.Payslips).AsNoTracking().ToListAsync();
        }

        public async Task<PayrollRun?> GetPayrollRunByIdAsync(Guid id)
        {
            return await _db.PayrollRuns.Include(r => r.Payslips).FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task UpdatePayrollRunAsync(PayrollRun run)
        {
            _db.PayrollRuns.Update(run);
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<Payslip>> GetPayslipsForEmployeeAsync(Guid employeeId)
        {
            return await _db.Payslips.AsNoTracking().Where(p => p.EmployeeId == employeeId).ToListAsync();
        }

        public async Task SaveChangesAsync()
            => await _db.SaveChangesAsync();
    }
}
