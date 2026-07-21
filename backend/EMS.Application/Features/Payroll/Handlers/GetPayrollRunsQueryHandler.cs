using EMS.Application.Features.Payroll.Dtos;
using EMS.Application.Features.Payroll.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class GetPayrollRunsQueryHandler : IRequestHandler<GetPayrollRunsQuery, IEnumerable<PayrollRunDto>>
    {
        private readonly IPayrollRepository _repo;

        public GetPayrollRunsQueryHandler(IPayrollRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<PayrollRunDto>> Handle(GetPayrollRunsQuery request, CancellationToken cancellationToken)
        {
            var runs = await _repo.GetPayrollRunsAsync();
            return runs.Select(PayrollRunDto.FromEntity).ToList();
        }
    }
}
