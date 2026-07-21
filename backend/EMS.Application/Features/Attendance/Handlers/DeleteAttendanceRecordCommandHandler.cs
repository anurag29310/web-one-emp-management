using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Attendance.Handlers
{
    public class DeleteAttendanceRecordCommandHandler : IRequestHandler<Commands.DeleteAttendanceRecordCommand>
    {
        private readonly IAttendanceRepository _repo;
        private readonly ILogger<DeleteAttendanceRecordCommandHandler> _logger;

        public DeleteAttendanceRecordCommandHandler(IAttendanceRepository repo, ILogger<DeleteAttendanceRecordCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Handle(Commands.DeleteAttendanceRecordCommand request, CancellationToken cancellationToken)
        {
            var record = await _repo.GetRecordByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Attendance record {request.Id} not found.");

            await _repo.DeleteRecordAsync(record, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (soft) attendance record {RecordId}", record.Id);
        }
    }
}
