using EMS.Application.Features.Payroll.Commands;
using EMS.Application.Interfaces;
using Microsoft.Extensions.Logging;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class RestoreSalaryStructureCommandHandler : IRequestHandler<RestoreSalaryStructureCommand>
    {
        private readonly IPayrollRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RestoreSalaryStructureCommandHandler> _logger;

        public RestoreSalaryStructureCommandHandler(IPayrollRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<RestoreSalaryStructureCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RestoreSalaryStructureCommand request, CancellationToken cancellationToken)
        {
            var s = await _repo.GetSalaryStructureByIdIncludingDeletedAsync(request.Id)
                ?? throw new InvalidOperationException($"Salary structure {request.Id} not found.");

            s.IsDeleted = false;
            s.DeletedAtUtc = null;
            s.DeletedBy = null;
            s.UpdatedAtUtc = DateTime.UtcNow;
            s.UpdatedBy = _currentUser.UserId;

            // Deliberately does NOT call UpdateSalaryStructureAsync: that method force-marks every
            // entity in s.Allowances/s.Deductions as Added (needed for the Update handler's
            // "replace children with brand-new objects" pattern) — here the children are the
            // existing, already-persisted rows untouched by Restore, so forcing them to Added would
            // try to re-INSERT them and violate their primary key. `s` is already tracked (fetched
            // from this same DbContext), so its scalar changes are picked up by SaveChanges alone.
            await _repo.SaveChangesAsync();
            _logger.LogInformation("Restored salary structure {SalaryStructureId}", s.Id);

            await _auditLogger.LogAsync("SalaryStructure", s.Id, "Restored", ct: cancellationToken);
        }
    }
}
