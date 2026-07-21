using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Attendance.Handlers
{
    public class EndEmployeeShiftCommandHandler : IRequestHandler<Commands.EndEmployeeShiftCommand>
    {
        private readonly IAttendanceRepository _repo;
        private readonly ILogger<EndEmployeeShiftCommandHandler> _logger;

        public EndEmployeeShiftCommandHandler(IAttendanceRepository repo, ILogger<EndEmployeeShiftCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Handle(Commands.EndEmployeeShiftCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _repo.GetEmployeeShiftByIdAsync(request.AssignmentId, cancellationToken);
            if (assignment == null || assignment.EmployeeId != request.EmployeeId)
                throw new InvalidOperationException($"Shift assignment {request.AssignmentId} not found.");

            await _repo.DeleteEmployeeShiftAsync(assignment, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Ended shift assignment {AssignmentId} for employee {EmployeeId}", assignment.Id, assignment.EmployeeId);
        }
    }
}
