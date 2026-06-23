using EMS.Application.Features.Payroll.Queries;
using EMS.Application.Features.Payroll.Dtos;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class GetSalaryStructureByIdQueryHandler : IRequestHandler<GetSalaryStructureByIdQuery, SalaryStructureDto?>
    {
        private readonly IPayrollRepository _repo;

        public GetSalaryStructureByIdQueryHandler(IPayrollRepository repo)
        {
            _repo = repo;
        }

        public async Task<SalaryStructureDto?> Handle(GetSalaryStructureByIdQuery request, CancellationToken cancellationToken)
        {
            var s = await _repo.GetSalaryStructureByIdAsync(request.Id);
            if (s == null) return null;
            return new SalaryStructureDto
            {
                Id = s.Id,
                EmployeeId = s.EmployeeId,
                BasicSalary = s.BasicSalary,
                EffectiveFrom = s.EffectiveFrom,
                EffectiveTo = s.EffectiveTo,
                Allowances = s.Allowances?.ConvertAll(a => new AllowanceDto { Id = a.Id, Name = a.Name, Amount = a.Amount }) ?? new(),
                Deductions = s.Deductions?.ConvertAll(d => new DeductionDto { Id = d.Id, Name = d.Name, Amount = d.Amount }) ?? new()
            };
        }
    }
}
