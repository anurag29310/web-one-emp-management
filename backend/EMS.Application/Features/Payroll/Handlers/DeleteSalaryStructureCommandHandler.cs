using EMS.Application.Features.Payroll.Commands;
using EMS.Application.Interfaces;
using Microsoft.Extensions.Logging;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class DeleteSalaryStructureCommandHandler : IRequestHandler<DeleteSalaryStructureCommand>
    {
        private readonly IPayrollRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<DeleteSalaryStructureCommandHandler> _logger;

        public DeleteSalaryStructureCommandHandler(IPayrollRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<DeleteSalaryStructureCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(DeleteSalaryStructureCommand request, CancellationToken cancellationToken)
        {
            var s = await _repo.GetSalaryStructureByIdAsync(request.Id)
                ?? throw new InvalidOperationException("Salary structure not found.");

            s.DeletedAtUtc = DateTime.UtcNow;
            s.DeletedBy = _currentUser.UserId;

            await _repo.DeleteSalaryStructureAsync(s);
            await _repo.SaveChangesAsync();
            _logger.LogInformation("Deleted (soft) salary structure {SalaryStructureId}", s.Id);

            await _auditLogger.LogAsync("SalaryStructure", s.Id, "Deleted", ct: cancellationToken);
        }
    }
}
