using EMS.Application.Features.Payroll.Commands;
using EMS.Application.Features.Payroll.Dtos;
using EMS.Domain.Entities;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class CreateSalaryStructureCommandHandler : IRequestHandler<CreateSalaryStructureCommand, Guid>
    {
        private readonly IPayrollRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;

        public CreateSalaryStructureCommandHandler(IPayrollRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
        }

        public async Task<Guid> Handle(CreateSalaryStructureCommand request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var s = new SalaryStructure
            {
                Id = Guid.NewGuid(),
                EmployeeId = request.EmployeeId,
                BasicSalary = request.BasicSalary,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = request.EffectiveTo,
                CreatedAtUtc = now,
                CreatedBy = _currentUser.UserId,
                IsDeleted = false
            };

            if (request.Allowances != null && request.Allowances.Any())
            {
                s.Allowances = request.Allowances.Select(a => new Allowance { Id = Guid.NewGuid(), Name = a.Name, Amount = a.Amount, SalaryStructureId = s.Id, CreatedAtUtc = now }).ToList();
            }

            if (request.Deductions != null && request.Deductions.Any())
            {
                s.Deductions = request.Deductions.Select(d => new Deduction { Id = Guid.NewGuid(), Name = d.Name, Amount = d.Amount, SalaryStructureId = s.Id, CreatedAtUtc = now }).ToList();
            }

            await _repo.CreateSalaryStructureAsync(s);
            await _repo.SaveChangesAsync();

            await _auditLogger.LogAsync("SalaryStructure", s.Id, "Created", ct: cancellationToken);

            return s.Id;
        }
    }
}
