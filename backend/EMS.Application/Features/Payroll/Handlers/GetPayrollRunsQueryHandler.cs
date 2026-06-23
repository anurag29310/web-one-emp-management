using EMS.Application.Features.Payroll.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class GetPayrollRunsQueryHandler : IRequestHandler<GetPayrollRunsQuery, IEnumerable<Domain.Entities.PayrollRun>>
    {
        private readonly IPayrollRepository _repo;

        public GetPayrollRunsQueryHandler(IPayrollRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Domain.Entities.PayrollRun>> Handle(GetPayrollRunsQuery request, CancellationToken cancellationToken)
        {
            return await _repo.GetPayrollRunsAsync();
        }
    }
}
