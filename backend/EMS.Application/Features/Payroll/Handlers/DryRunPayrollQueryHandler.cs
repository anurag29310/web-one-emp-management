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
        private readonly IReimbursementRepository _reimbursementRepo;

        public DryRunPayrollQueryHandler(IPayrollRepository repo, IReimbursementRepository reimbursementRepo)
        {
            _repo = repo;
            _reimbursementRepo = reimbursementRepo;
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
                var totalReimbursements = (await _reimbursementRepo.GetApprovedUnprocessedByEmployeeAsync(emp.Id, cancellationToken)).Sum(r => r.Amount);
                var net = gross - totalDeduct + totalReimbursements;
                previews.Add(new PayslipPreview { EmployeeId = emp.Id, Basic = structure.BasicSalary, TotalAllowances = totalAllow, TotalDeductions = totalDeduct, TotalReimbursements = totalReimbursements, GrossPay = gross, NetPay = net });
            }
            return previews;
        }
    }
}
