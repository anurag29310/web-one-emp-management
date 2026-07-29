using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class ProposePromotionCommandHandler : IRequestHandler<ProposePromotionCommand, Guid>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<ProposePromotionCommandHandler> _logger;

        public ProposePromotionCommandHandler(IPerformanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo, IAuditLogger auditLogger, ILogger<ProposePromotionCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Guid> Handle(ProposePromotionCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepo.GetByIdAsync(request.EmployeeId, cancellationToken)
                ?? throw new InvalidOperationException($"Employee {request.EmployeeId} not found.");

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (!await PerformanceScopeHelper.IsManagerOfAsync(_employeeRepo, requesterEmployeeId, request.EmployeeId, request.IsManager, cancellationToken))
                    throw new UnauthorizedAccessException("You can only propose promotions for your own direct reports.");
            }

            if (request.ToDesignationId == employee.DesignationId && request.ToDepartmentId == employee.DepartmentId)
                throw new InvalidOperationException("The target designation/department must differ from the employee's current one.");

            var id = Guid.NewGuid();
            var promotion = new Promotion
            {
                Id = id,
                PromotionNumber = $"PRO-{id:N}".Substring(0, 12).ToUpperInvariant(),
                EmployeeId = employee.Id,
                FromDesignationId = employee.DesignationId,
                ToDesignationId = request.ToDesignationId,
                FromDepartmentId = employee.DepartmentId,
                ToDepartmentId = request.ToDepartmentId,
                EffectiveDate = request.EffectiveDate,
                Reason = request.Reason,
                Status = PromotionStatus.Proposed,
                ProposedByUserId = request.RequestingUserId,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = request.RequestingUserId,
                IsDeleted = false
            };

            await _repo.AddPromotionAsync(promotion, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Proposed promotion {PromotionNumber} ({PromotionId}) for employee {EmployeeId}", promotion.PromotionNumber, promotion.Id, promotion.EmployeeId);

            await _auditLogger.LogAsync("Promotion", promotion.Id, "Proposed", ct: cancellationToken);

            return promotion.Id;
        }
    }
}
