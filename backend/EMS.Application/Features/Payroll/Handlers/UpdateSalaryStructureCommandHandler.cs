using EMS.Application.Features.Payroll.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class UpdateSalaryStructureCommandHandler : IRequestHandler<UpdateSalaryStructureCommand>
    {
        private readonly IPayrollRepository _repo;

        public UpdateSalaryStructureCommandHandler(IPayrollRepository repo)
        {
            _repo = repo;
        }

        public async Task<Unit> Handle(UpdateSalaryStructureCommand request, CancellationToken cancellationToken)
        {
            var s = await _repo.GetSalaryStructureByIdAsync(request.Id);
            if (s == null) throw new InvalidOperationException("Salary structure not found");

            s.BasicSalary = request.BasicSalary;
            s.EffectiveFrom = request.EffectiveFrom;
            s.EffectiveTo = request.EffectiveTo;

            // simple replace children: remove existing and add new
            s.Allowances?.Clear();
            s.Deductions?.Clear();

            if (request.Allowances != null)
                s.Allowances = request.Allowances.Select(a => new Allowance { Id = a.Id ?? Guid.NewGuid(), SalaryStructureId = s.Id, Name = a.Name, Amount = a.Amount }).ToList();

            if (request.Deductions != null)
                s.Deductions = request.Deductions.Select(d => new Deduction { Id = d.Id ?? Guid.NewGuid(), SalaryStructureId = s.Id, Name = d.Name, Amount = d.Amount }).ToList();

            await _repo.UpdateSalaryStructureAsync(s);
            await _repo.SaveChangesAsync();
            return Unit.Value;
        }

        Task IRequestHandler<UpdateSalaryStructureCommand>.Handle(UpdateSalaryStructureCommand request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }
    }
}
