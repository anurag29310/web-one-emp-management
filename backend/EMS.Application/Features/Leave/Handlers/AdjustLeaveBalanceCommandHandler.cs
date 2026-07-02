using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class AdjustLeaveBalanceCommandHandler : IRequestHandler<Commands.AdjustLeaveBalanceCommand>
    {
        private readonly ILeaveRepository _repo;
        private readonly ILogger<AdjustLeaveBalanceCommandHandler> _logger;

        public AdjustLeaveBalanceCommandHandler(ILeaveRepository repo, ILogger<AdjustLeaveBalanceCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Handle(Commands.AdjustLeaveBalanceCommand request, CancellationToken cancellationToken)
        {
            var balance = await _repo.GetLeaveBalanceAsync(request.EmployeeId, request.LeaveTypeId, request.Year, cancellationToken)
                ?? throw new InvalidOperationException($"Leave balance not found for employee {request.EmployeeId}, type {request.LeaveTypeId}, year {request.Year}.");

            balance.Adjusted += request.Adjusted;
            balance.Available = balance.OpeningBalance + balance.Accrued + balance.Adjusted - balance.Used;

            await _repo.UpdateLeaveBalanceAsync(balance, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Leave balance adjusted for Employee {EmployeeId} LeaveType {LeaveTypeId} Year {Year} by {Amount}",
                request.EmployeeId, request.LeaveTypeId, request.Year, request.Adjusted);
        }
    }
}
