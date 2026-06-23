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

        public CreateSalaryStructureCommandHandler(IPayrollRepository repo)
        {
            _repo = repo;
        }

        public async Task<Guid> Handle(CreateSalaryStructureCommand request, CancellationToken cancellationToken)
        {
            var s = new SalaryStructure
            {
                Id = Guid.NewGuid(),
                EmployeeId = request.EmployeeId,
                BasicSalary = request.BasicSalary,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = request.EffectiveTo
            };

            if (request.Allowances != null && request.Allowances.Any())
            {
                s.Allowances = request.Allowances.Select(a => new Allowance { Id = Guid.NewGuid(), Name = a.Name, Amount = a.Amount, SalaryStructureId = s.Id }).ToList();
            }

            if (request.Deductions != null && request.Deductions.Any())
            {
                s.Deductions = request.Deductions.Select(d => new Deduction { Id = Guid.NewGuid(), Name = d.Name, Amount = d.Amount, SalaryStructureId = s.Id }).ToList();
            }

            await _repo.CreateSalaryStructureAsync(s);
            await _repo.SaveChangesAsync();
            return s.Id;
        }
    }
}
