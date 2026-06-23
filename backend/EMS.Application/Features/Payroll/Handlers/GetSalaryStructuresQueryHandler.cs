using EMS.Application.Features.Payroll.Queries;
using EMS.Application.Features.Payroll.Dtos;
using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class GetSalaryStructuresQueryHandler : IRequestHandler<GetSalaryStructuresQuery, IEnumerable<SalaryStructureDto>>
    {
        private readonly IPayrollRepository _repo;

        public GetSalaryStructuresQueryHandler(IPayrollRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<SalaryStructureDto>> Handle(GetSalaryStructuresQuery request, CancellationToken cancellationToken)
        {
            var list = await _repo.GetSalaryStructuresAsync();
            return list.Select(s => new SalaryStructureDto
            {
                Id = s.Id,
                EmployeeId = s.EmployeeId,
                BasicSalary = s.BasicSalary,
                EffectiveFrom = s.EffectiveFrom,
                EffectiveTo = s.EffectiveTo,
                Allowances = s.Allowances?.ConvertAll(a => new AllowanceDto { Id = a.Id, Name = a.Name, Amount = a.Amount }) ?? new(),
                Deductions = s.Deductions?.ConvertAll(d => new DeductionDto { Id = d.Id, Name = d.Name, Amount = d.Amount }) ?? new()
            }).ToList();
        }
    }
}
