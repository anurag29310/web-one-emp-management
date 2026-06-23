using EMS.Application.Features.Payroll.Commands;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class ApprovePayrollRunCommandHandler : IRequestHandler<ApprovePayrollRunCommand>
    {
        private readonly IPayrollRepository _repo;

        public ApprovePayrollRunCommandHandler(IPayrollRepository repo)
        {
            _repo = repo;
        }

        public async Task<Unit> Handle(ApprovePayrollRunCommand request, CancellationToken cancellationToken)
        {
            var run = await _repo.GetPayrollRunByIdAsync(request.PayrollRunId);
            if (run == null) throw new System.InvalidOperationException("Payroll run not found");
            run.Status = "Approved";
            await _repo.UpdatePayrollRunAsync(run);
            await _repo.SaveChangesAsync();
            return Unit.Value;
        }

        Task IRequestHandler<ApprovePayrollRunCommand>.Handle(ApprovePayrollRunCommand request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }
    }
}
