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
            => await _db.Employees.AsNoTracking().Include(e => e.Department).Include(e => e.Designation).Where(e => e.IsActive).ToListAsync();

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

        public Task UpdateSalaryStructureAsync(SalaryStructure structure)
        {
            // `structure` is always the tracked instance returned by GetSalaryStructureByIdAsync on
            // this same DbContext, never a detached one, so scalar property changes and the removal
            // of cleared Allowances/Deductions (a required relationship — severing deletes rather
            // than orphans) are picked up automatically. The newly-assigned replacement Allowances/
            // Deductions (the "replace children" pattern in UpdateSalaryStructureCommandHandler) need
            // an explicit push, though: EF Core only auto-detects a reachable-but-untracked entity as
            // Added when its key is unset. These are plain `new Allowance { Id = Guid.NewGuid(), ... }`
            // objects with an already-assigned key, so EF's change detection assumes the row might
            // already exist and marks them Modified instead — SaveChanges then throws
            // DbUpdateConcurrencyException trying to UPDATE a row that was never inserted.
            foreach (var allowance in structure.Allowances ?? Enumerable.Empty<Allowance>())
            {
                if (_db.Entry(allowance).State != EntityState.Added)
                    _db.Entry(allowance).State = EntityState.Added;
            }
            foreach (var deduction in structure.Deductions ?? Enumerable.Empty<Deduction>())
            {
                if (_db.Entry(deduction).State != EntityState.Added)
                    _db.Entry(deduction).State = EntityState.Added;
            }

            return Task.CompletedTask;
        }

        public async Task<bool> DeleteSalaryStructureAsync(Guid id)
        {
            var s = await _db.SalaryStructures.FindAsync(id);
            if (s == null) return false;
            _db.SalaryStructures.Remove(s);
            return true;
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
