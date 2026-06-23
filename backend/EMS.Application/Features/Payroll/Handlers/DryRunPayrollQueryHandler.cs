using EMS.Application.Features.Payroll.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class DryRunPayrollQueryHandler : IRequestHandler<DryRunPayrollQuery, IEnumerable<PayslipPreview>>
    {
        private readonly IPayrollRepository _repo;

        public DryRunPayrollQueryHandler(IPayrollRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<PayslipPreview>> Handle(DryRunPayrollQuery request, CancellationToken cancellationToken)
        {
            var employees = await _repo.GetAllEmployeesAsync();
            var previews = new List<PayslipPreview>();
            foreach (var emp in employees)
            {
                var structure = await _repo.GetEffectiveSalaryStructureAsync(emp.Id, request.PeriodStart);
                if (structure == null) continue;
                var totalAllow = structure.Allowances?.Sum(a => a.Amount) ?? 0m;
                var totalDeduct = structure.Deductions?.Sum(d => d.Amount) ?? 0m;
                var gross = structure.BasicSalary + totalAllow;
                var net = gross - totalDeduct;
                previews.Add(new PayslipPreview { EmployeeId = emp.Id, Basic = structure.BasicSalary, TotalAllowances = totalAllow, TotalDeductions = totalDeduct, GrossPay = gross, NetPay = net });
            }
            return previews;
        }
    }
}
