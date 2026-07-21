using EMS.Application.Features.Payroll.Dtos;
using EMS.Application.Features.Payroll.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class GetPayrollRunByIdQueryHandler : IRequestHandler<GetPayrollRunByIdQuery, PayrollRunDto?>
    {
        private readonly IPayrollRepository _repo;

        public GetPayrollRunByIdQueryHandler(IPayrollRepository repo)
        {
            _repo = repo;
        }

        public async Task<PayrollRunDto?> Handle(GetPayrollRunByIdQuery request, CancellationToken cancellationToken)
        {
            var run = await _repo.GetPayrollRunByIdAsync(request.Id);
            return run == null ? null : PayrollRunDto.FromEntity(run);
        }
    }
}
