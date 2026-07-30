using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance
{
    /// <summary>Applies an approved Promotion's designation/department change to its Employee row and
    /// stamps AppliedAtUtc. Shared by ApprovePromotionCommandHandler (immediate case, EffectiveDate
    /// already arrived) and RunDailySweepCommandHandler (deferred case, EffectiveDate arrives later)
    /// so the two paths can't drift apart.</summary>
    internal static class PromotionApplier
    {
        public static async Task ApplyAsync(IEmployeeRepository employeeRepo, Promotion promotion, DateTime appliedAtUtc, CancellationToken ct)
        {
            var employee = await employeeRepo.GetByIdAsync(promotion.EmployeeId, ct)
                ?? throw new InvalidOperationException($"Employee {promotion.EmployeeId} not found.");

            employee.DesignationId = promotion.ToDesignationId;
            if (promotion.ToDepartmentId.HasValue)
                employee.DepartmentId = promotion.ToDepartmentId;
            employee.UpdatedAtUtc = appliedAtUtc;
            await employeeRepo.UpdateAsync(employee, ct);

            promotion.AppliedAtUtc = appliedAtUtc;
        }
    }
}
