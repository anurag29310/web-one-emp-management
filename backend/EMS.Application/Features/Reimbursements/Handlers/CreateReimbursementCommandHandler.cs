using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reimbursements.Handlers
{
    public class CreateReimbursementCommandHandler : IRequestHandler<CreateReimbursementCommand, Reimbursement>
    {
        private readonly IReimbursementRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CreateReimbursementCommandHandler> _logger;
        private readonly decimal _mileageRatePerKm;

        public CreateReimbursementCommandHandler(IReimbursementRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, IConfiguration configuration, ILogger<CreateReimbursementCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
            _mileageRatePerKm = decimal.TryParse(configuration["Reimbursements:MileageRatePerKm"], out var rate) ? rate : 0.30m;
        }

        public async Task<Reimbursement> Handle(CreateReimbursementCommand request, CancellationToken cancellationToken)
        {
            var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
            if (requester?.EmployeeId == null)
                throw new InvalidOperationException("The caller has no linked employee record and cannot submit reimbursements.");

            var isMileageClaim = request.DistanceKm.HasValue;

            var id = Guid.NewGuid();
            var reimbursement = new Reimbursement
            {
                Id = id,
                ReimbursementNumber = $"REI-{id:N}".Substring(0, 12).ToUpperInvariant(),
                EmployeeId = requester.EmployeeId.Value,
                ExpenseTitle = request.ExpenseTitle,
                ExpenseCategory = request.ExpenseCategory,
                ExpenseDate = request.ExpenseDate,
                Amount = isMileageClaim ? MileageCalculator.CalculateAmount(request.DistanceKm!.Value, _mileageRatePerKm) : request.Amount,
                Currency = request.Currency,
                Description = request.Description,
                Notes = request.Notes,
                DistanceKm = request.DistanceKm,
                MileageRatePerKm = isMileageClaim ? _mileageRatePerKm : null,
                Status = EMS.Domain.Enums.ReimbursementStatus.Draft,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = request.RequestingUserId
            };

            await _repo.AddAsync(reimbursement, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created reimbursement {ReimbursementNumber} ({ReimbursementId}) for employee {EmployeeId}", reimbursement.ReimbursementNumber, reimbursement.Id, reimbursement.EmployeeId);

            await _auditLogger.LogAsync("Reimbursement", reimbursement.Id, "Created", ct: cancellationToken);

            return await _repo.GetByIdAsync(reimbursement.Id, cancellationToken) ?? reimbursement;
        }
    }
}
