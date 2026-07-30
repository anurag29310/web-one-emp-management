using EMS.Domain.Entities;
using EMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IPerformanceRepository
    {
        // ─── Goals ─────────────────────────────────────────────────────────────────
        Task<PerformanceGoal?> GetGoalByIdAsync(Guid id, CancellationToken ct = default);
        Task<PerformanceGoal?> GetGoalByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<PerformanceGoal>> GetGoalsAsync(int page, int pageSize, Guid? employeeId, GoalStatus? status, string? category, IEnumerable<Guid>? employeeScope, CancellationToken ct = default);
        Task<int> CountGoalsAsync(Guid? employeeId, GoalStatus? status, string? category, IEnumerable<Guid>? employeeScope, CancellationToken ct = default);
        Task AddGoalAsync(PerformanceGoal goal, CancellationToken ct = default);
        Task UpdateGoalAsync(PerformanceGoal goal, CancellationToken ct = default);
        Task DeleteGoalAsync(PerformanceGoal goal, CancellationToken ct = default);
        Task RestoreGoalAsync(PerformanceGoal goal, CancellationToken ct = default);

        // ─── Goal KPIs ─────────────────────────────────────────────────────────────
        Task<PerformanceGoalKpi?> GetKpiByIdAsync(Guid id, CancellationToken ct = default);
        Task AddKpiAsync(PerformanceGoalKpi kpi, CancellationToken ct = default);
        Task UpdateKpiAsync(PerformanceGoalKpi kpi, CancellationToken ct = default);

        // ─── Reviews ───────────────────────────────────────────────────────────────
        Task<PerformanceReview?> GetReviewByIdAsync(Guid id, CancellationToken ct = default);
        Task<PerformanceReview?> GetReviewByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<PerformanceReview>> GetReviewsAsync(int page, int pageSize, Guid? employeeId, Guid? reviewerEmployeeId, ReviewStatus? status, IEnumerable<Guid>? employeeScope, Guid? participantEmployeeId, CancellationToken ct = default);
        Task<int> CountReviewsAsync(Guid? employeeId, Guid? reviewerEmployeeId, ReviewStatus? status, IEnumerable<Guid>? employeeScope, Guid? participantEmployeeId, CancellationToken ct = default);
        Task AddReviewAsync(PerformanceReview review, CancellationToken ct = default);
        Task UpdateReviewAsync(PerformanceReview review, CancellationToken ct = default);
        Task DeleteReviewAsync(PerformanceReview review, CancellationToken ct = default);
        Task RestoreReviewAsync(PerformanceReview review, CancellationToken ct = default);

        // ─── Promotions ────────────────────────────────────────────────────────────
        Task<Promotion?> GetPromotionByIdAsync(Guid id, CancellationToken ct = default);
        Task<Promotion?> GetPromotionByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Promotion>> GetPromotionsAsync(int page, int pageSize, Guid? employeeId, PromotionStatus? status, IEnumerable<Guid>? employeeScope, CancellationToken ct = default);
        Task<int> CountPromotionsAsync(Guid? employeeId, PromotionStatus? status, IEnumerable<Guid>? employeeScope, CancellationToken ct = default);
        Task<IEnumerable<Promotion>> GetApprovedPromotionsDueForApplicationAsync(DateTime asOfUtc, CancellationToken ct = default);
        Task AddPromotionAsync(Promotion promotion, CancellationToken ct = default);
        Task UpdatePromotionAsync(Promotion promotion, CancellationToken ct = default);
        Task DeletePromotionAsync(Promotion promotion, CancellationToken ct = default);
        Task RestorePromotionAsync(Promotion promotion, CancellationToken ct = default);

        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
