using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Attendance.Handlers
{
    public class AssignEmployeeShiftCommandHandler : IRequestHandler<Commands.AssignEmployeeShiftCommand, EmployeeShift>
    {
        private readonly IAttendanceRepository _repo;
        private readonly ILogger<AssignEmployeeShiftCommandHandler> _logger;

        public AssignEmployeeShiftCommandHandler(IAttendanceRepository repo, ILogger<AssignEmployeeShiftCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<EmployeeShift> Handle(Commands.AssignEmployeeShiftCommand request, CancellationToken cancellationToken)
        {
            var shift = await _repo.GetShiftByIdAsync(request.ShiftId, cancellationToken)
                ?? throw new InvalidOperationException("Shift not found.");

            var assignment = new EmployeeShift
            {
                Id = Guid.NewGuid(),
                EmployeeId = request.EmployeeId,
                ShiftId = shift.Id,
                EffectiveFrom = request.EffectiveFrom.Date,
                EffectiveTo = request.EffectiveTo?.Date,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repo.AddEmployeeShiftAsync(assignment, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Assigned shift {ShiftId} to employee {EmployeeId}", shift.Id, request.EmployeeId);
            return assignment;
        }
    }
}
