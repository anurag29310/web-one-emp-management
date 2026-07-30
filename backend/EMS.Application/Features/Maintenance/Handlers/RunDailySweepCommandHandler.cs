using EMS.Application.Features.Maintenance.Commands;
using EMS.Application.Features.Maintenance.DTOs;
using EMS.Application.Features.Performance;
using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Maintenance.Handlers
{
    public class RunDailySweepCommandHandler : IRequestHandler<RunDailySweepCommand, DailySweepResult>
    {
        private readonly IRecruitmentRepository _recruitmentRepo;
        private readonly IPerformanceRepository _performanceRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RunDailySweepCommandHandler> _logger;

        public RunDailySweepCommandHandler(IRecruitmentRepository recruitmentRepo, IPerformanceRepository performanceRepo, IEmployeeRepository employeeRepo, IAuditLogger auditLogger, ILogger<RunDailySweepCommandHandler> logger)
        {
            _recruitmentRepo = recruitmentRepo;
            _performanceRepo = performanceRepo;
            _employeeRepo = employeeRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<DailySweepResult> Handle(RunDailySweepCommand request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var result = new DailySweepResult();

            var expiredOffers = await _recruitmentRepo.GetSentOffersPastExpiryAsync(now, cancellationToken);
            foreach (var offer in expiredOffers)
            {
                offer.Status = OfferStatus.Expired;
                offer.UpdatedAtUtc = now;
                await _recruitmentRepo.UpdateOfferAsync(offer, cancellationToken);
                await _auditLogger.LogAsync("Offer", offer.Id, "Expired", ct: cancellationToken);
                result.OffersExpired++;
            }
            if (result.OffersExpired > 0)
                await _recruitmentRepo.SaveChangesAsync(cancellationToken);

            var duePromotions = await _performanceRepo.GetApprovedPromotionsDueForApplicationAsync(now, cancellationToken);
            foreach (var promotion in duePromotions)
            {
                await PromotionApplier.ApplyAsync(_employeeRepo, promotion, now, cancellationToken);
                await _performanceRepo.UpdatePromotionAsync(promotion, cancellationToken);
                await _auditLogger.LogAsync("Promotion", promotion.Id, "Applied", newValues: new { promotion.ToDesignationId, promotion.ToDepartmentId }, ct: cancellationToken);
                result.PromotionsApplied++;
            }
            if (result.PromotionsApplied > 0)
                await _performanceRepo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Daily sweep: {OffersExpired} offer(s) expired, {PromotionsApplied} promotion(s) applied", result.OffersExpired, result.PromotionsApplied);

            return result;
        }
    }
}
