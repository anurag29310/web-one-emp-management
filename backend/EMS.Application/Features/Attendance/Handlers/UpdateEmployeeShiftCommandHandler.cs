using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Attendance.Handlers
{
    public class UpdateEmployeeShiftCommandHandler : IRequestHandler<Commands.UpdateEmployeeShiftCommand, EmployeeShift>
    {
        private readonly IAttendanceRepository _repo;
        private readonly ILogger<UpdateEmployeeShiftCommandHandler> _logger;

        public UpdateEmployeeShiftCommandHandler(IAttendanceRepository repo, ILogger<UpdateEmployeeShiftCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<EmployeeShift> Handle(Commands.UpdateEmployeeShiftCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _repo.GetEmployeeShiftByIdAsync(request.AssignmentId, cancellationToken);
            if (assignment == null || assignment.EmployeeId != request.EmployeeId)
                throw new InvalidOperationException($"Shift assignment {request.AssignmentId} not found.");

            var shift = await _repo.GetShiftByIdAsync(request.ShiftId, cancellationToken)
                ?? throw new InvalidOperationException("Shift not found.");

            assignment.ShiftId = shift.Id;
            assignment.EffectiveFrom = request.EffectiveFrom.Date;
            assignment.EffectiveTo = request.EffectiveTo?.Date;
            assignment.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateEmployeeShiftAsync(assignment, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated shift assignment {AssignmentId} for employee {EmployeeId}", assignment.Id, assignment.EmployeeId);
            return assignment;
        }
    }
}
