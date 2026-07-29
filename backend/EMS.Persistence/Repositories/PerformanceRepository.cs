using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class PerformanceRepository : IPerformanceRepository
    {
        private readonly ApplicationDbContext _db;

        public PerformanceRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        // ─── Goals ─────────────────────────────────────────────────────────────────

        public async Task<PerformanceGoal?> GetGoalByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.PerformanceGoals.Include(g => g.Kpis).Include(g => g.Employee).FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, ct);

        public async Task<PerformanceGoal?> GetGoalByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await _db.PerformanceGoals.Include(g => g.Kpis).Include(g => g.Employee).FirstOrDefaultAsync(g => g.Id == id, ct);

        private IQueryable<PerformanceGoal> BuildGoalFilterQuery(Guid? employeeId, GoalStatus? status, string? category, IEnumerable<Guid>? employeeScope)
        {
            var q = _db.PerformanceGoals.AsNoTracking().Include(g => g.Kpis).Include(g => g.Employee).Where(g => !g.IsDeleted);

            if (employeeId.HasValue)
                q = q.Where(g => g.EmployeeId == employeeId.Value);
            else if (employeeScope != null)
            {
                var scopeList = employeeScope.ToList();
                q = q.Where(g => scopeList.Contains(g.EmployeeId));
            }

            if (status.HasValue)
                q = q.Where(g => g.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(category))
                q = q.Where(g => g.Category == category);

            return q;
        }

        public async Task<IEnumerable<PerformanceGoal>> GetGoalsAsync(int page, int pageSize, Guid? employeeId, GoalStatus? status, string? category, IEnumerable<Guid>? employeeScope, CancellationToken ct = default) =>
            await BuildGoalFilterQuery(employeeId, status, category, employeeScope)
                .OrderByDescending(g => g.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<int> CountGoalsAsync(Guid? employeeId, GoalStatus? status, string? category, IEnumerable<Guid>? employeeScope, CancellationToken ct = default) =>
            await BuildGoalFilterQuery(employeeId, status, category, employeeScope).CountAsync(ct);

        public async Task AddGoalAsync(PerformanceGoal goal, CancellationToken ct = default) =>
            await _db.PerformanceGoals.AddAsync(goal, ct);

        public Task UpdateGoalAsync(PerformanceGoal goal, CancellationToken ct = default)
        {
            _db.PerformanceGoals.Update(goal);
            return Task.CompletedTask;
        }

        public Task DeleteGoalAsync(PerformanceGoal goal, CancellationToken ct = default)
        {
            goal.IsDeleted = true;
            _db.PerformanceGoals.Update(goal);
            return Task.CompletedTask;
        }

        public Task RestoreGoalAsync(PerformanceGoal goal, CancellationToken ct = default)
        {
            goal.IsDeleted = false;
            _db.PerformanceGoals.Update(goal);
            return Task.CompletedTask;
        }

        // ─── Goal KPIs ─────────────────────────────────────────────────────────────

        public async Task<PerformanceGoalKpi?> GetKpiByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.PerformanceGoalKpis.Include(k => k.Goal).FirstOrDefaultAsync(k => k.Id == id, ct);

        public async Task AddKpiAsync(PerformanceGoalKpi kpi, CancellationToken ct = default) =>
            await _db.PerformanceGoalKpis.AddAsync(kpi, ct);

        public Task UpdateKpiAsync(PerformanceGoalKpi kpi, CancellationToken ct = default)
        {
            _db.PerformanceGoalKpis.Update(kpi);
            return Task.CompletedTask;
        }

        // ─── Reviews ───────────────────────────────────────────────────────────────

        public async Task<PerformanceReview?> GetReviewByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.PerformanceReviews.Include(r => r.Employee).Include(r => r.ReviewerEmployee)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);

        public async Task<PerformanceReview?> GetReviewByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await _db.PerformanceReviews.Include(r => r.Employee).Include(r => r.ReviewerEmployee)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

        private IQueryable<PerformanceReview> BuildReviewFilterQuery(Guid? employeeId, Guid? reviewerEmployeeId, ReviewStatus? status, IEnumerable<Guid>? employeeScope, Guid? participantEmployeeId)
        {
            var q = _db.PerformanceReviews.AsNoTracking().Include(r => r.Employee).Include(r => r.ReviewerEmployee).Where(r => !r.IsDeleted);

            if (employeeId.HasValue)
                q = q.Where(r => r.EmployeeId == employeeId.Value);
            if (reviewerEmployeeId.HasValue)
                q = q.Where(r => r.ReviewerEmployeeId == reviewerEmployeeId.Value);
            if (status.HasValue)
                q = q.Where(r => r.Status == status.Value);

            if (employeeScope != null || participantEmployeeId.HasValue)
            {
                var scopeList = employeeScope?.ToList() ?? new List<Guid>();
                q = q.Where(r => scopeList.Contains(r.EmployeeId) || (participantEmployeeId.HasValue && r.ReviewerEmployeeId == participantEmployeeId.Value));
            }

            return q;
        }

        public async Task<IEnumerable<PerformanceReview>> GetReviewsAsync(int page, int pageSize, Guid? employeeId, Guid? reviewerEmployeeId, ReviewStatus? status, IEnumerable<Guid>? employeeScope, Guid? participantEmployeeId, CancellationToken ct = default) =>
            await BuildReviewFilterQuery(employeeId, reviewerEmployeeId, status, employeeScope, participantEmployeeId)
                .OrderByDescending(r => r.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<int> CountReviewsAsync(Guid? employeeId, Guid? reviewerEmployeeId, ReviewStatus? status, IEnumerable<Guid>? employeeScope, Guid? participantEmployeeId, CancellationToken ct = default) =>
            await BuildReviewFilterQuery(employeeId, reviewerEmployeeId, status, employeeScope, participantEmployeeId).CountAsync(ct);

        public async Task AddReviewAsync(PerformanceReview review, CancellationToken ct = default) =>
            await _db.PerformanceReviews.AddAsync(review, ct);

        public Task UpdateReviewAsync(PerformanceReview review, CancellationToken ct = default)
        {
            _db.PerformanceReviews.Update(review);
            return Task.CompletedTask;
        }

        public Task DeleteReviewAsync(PerformanceReview review, CancellationToken ct = default)
        {
            review.IsDeleted = true;
            _db.PerformanceReviews.Update(review);
            return Task.CompletedTask;
        }

        public Task RestoreReviewAsync(PerformanceReview review, CancellationToken ct = default)
        {
            review.IsDeleted = false;
            _db.PerformanceReviews.Update(review);
            return Task.CompletedTask;
        }

        // ─── Promotions ────────────────────────────────────────────────────────────

        public async Task<Promotion?> GetPromotionByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Promotions
                .Include(p => p.Employee).Include(p => p.FromDesignation).Include(p => p.ToDesignation)
                .Include(p => p.FromDepartment).Include(p => p.ToDepartment)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

        public async Task<Promotion?> GetPromotionByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await _db.Promotions
                .Include(p => p.Employee).Include(p => p.FromDesignation).Include(p => p.ToDesignation)
                .Include(p => p.FromDepartment).Include(p => p.ToDepartment)
                .FirstOrDefaultAsync(p => p.Id == id, ct);

        private IQueryable<Promotion> BuildPromotionFilterQuery(Guid? employeeId, PromotionStatus? status, IEnumerable<Guid>? employeeScope)
        {
            var q = _db.Promotions.AsNoTracking()
                .Include(p => p.Employee).Include(p => p.FromDesignation).Include(p => p.ToDesignation)
                .Include(p => p.FromDepartment).Include(p => p.ToDepartment)
                .Where(p => !p.IsDeleted);

            if (employeeId.HasValue)
                q = q.Where(p => p.EmployeeId == employeeId.Value);
            else if (employeeScope != null)
            {
                var scopeList = employeeScope.ToList();
                q = q.Where(p => scopeList.Contains(p.EmployeeId));
            }

            if (status.HasValue)
                q = q.Where(p => p.Status == status.Value);

            return q;
        }

        public async Task<IEnumerable<Promotion>> GetPromotionsAsync(int page, int pageSize, Guid? employeeId, PromotionStatus? status, IEnumerable<Guid>? employeeScope, CancellationToken ct = default) =>
            await BuildPromotionFilterQuery(employeeId, status, employeeScope)
                .OrderByDescending(p => p.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<int> CountPromotionsAsync(Guid? employeeId, PromotionStatus? status, IEnumerable<Guid>? employeeScope, CancellationToken ct = default) =>
            await BuildPromotionFilterQuery(employeeId, status, employeeScope).CountAsync(ct);

        public async Task AddPromotionAsync(Promotion promotion, CancellationToken ct = default) =>
            await _db.Promotions.AddAsync(promotion, ct);

        public Task UpdatePromotionAsync(Promotion promotion, CancellationToken ct = default)
        {
            _db.Promotions.Update(promotion);
            return Task.CompletedTask;
        }

        public Task DeletePromotionAsync(Promotion promotion, CancellationToken ct = default)
        {
            promotion.IsDeleted = true;
            _db.Promotions.Update(promotion);
            return Task.CompletedTask;
        }

        public Task RestorePromotionAsync(Promotion promotion, CancellationToken ct = default)
        {
            promotion.IsDeleted = false;
            _db.Promotions.Update(promotion);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}
