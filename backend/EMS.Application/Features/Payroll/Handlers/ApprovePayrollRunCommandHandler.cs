using EMS.Application.Features.Payroll.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class ApprovePayrollRunCommandHandler : IRequestHandler<ApprovePayrollRunCommand>
    {
        private readonly IPayrollRepository _repo;
        private readonly ILogger<ApprovePayrollRunCommandHandler> _logger;

        public ApprovePayrollRunCommandHandler(IPayrollRepository repo, ILogger<ApprovePayrollRunCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Unit> Handle(ApprovePayrollRunCommand request, CancellationToken cancellationToken)
        {
            var run = await _repo.GetPayrollRunByIdAsync(request.PayrollRunId);
            if (run == null) throw new InvalidOperationException("Payroll run not found.");
            if (run.Status == "Approved") throw new InvalidOperationException("Payroll run has already been approved.");
            if (run.Status != "Completed") throw new InvalidOperationException("Only completed payroll runs can be approved.");

            run.Status = "Approved";
            await _repo.UpdatePayrollRunAsync(run);
            await _repo.SaveChangesAsync();

            _logger.LogInformation("Payroll run {PayrollRunId} approved by user {ApprovedBy}.", request.PayrollRunId, request.ApprovedBy);

            return Unit.Value;
        }

        Task IRequestHandler<ApprovePayrollRunCommand>.Handle(ApprovePayrollRunCommand request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }
    }
}
