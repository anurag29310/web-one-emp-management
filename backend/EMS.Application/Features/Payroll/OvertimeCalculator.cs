using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Payroll
{
    /// <summary>Derives per-day overtime minutes and payable overtime amount from attendance and
    /// salary data. Auto-calculation is a default, not a source of truth — Payroll always allows a
    /// per-employee manual override (see ProcessPayrollCommand.Adjustments) for when it's wrong.</summary>
    public static class OvertimeCalculator
    {
        /// <summary>Minutes in the employee's assigned shift for that day, or <paramref name="defaultDailyMinutes"/>
        /// when no shift is assigned. Handles night shifts (end time past midnight) by wrapping the
        /// duration across 24 hours instead of producing a negative span.</summary>
        public static int StandardDailyMinutes(Shift? shift, int defaultDailyMinutes)
        {
            if (shift == null) return defaultDailyMinutes;
            var duration = shift.EndTime - shift.StartTime;
            if (duration < TimeSpan.Zero) duration = duration.Add(TimeSpan.FromHours(24));
            return (int)duration.TotalMinutes;
        }

        /// <summary>Minutes worked beyond the standard shift for one day. Zero if the employee never
        /// checked out (no TotalWorkMinutes) or worked at/under the standard.</summary>
        public static int OvertimeMinutesForDay(int? totalWorkMinutes, int standardDailyMinutes)
            => totalWorkMinutes.HasValue ? Math.Max(0, totalWorkMinutes.Value - standardDailyMinutes) : 0;

        /// <summary>Hourly rate derived from a monthly Basic Salary and a configured standard-hours
        /// divisor (RateLimiting-style config knob: Payroll:StandardMonthlyHours). Not specified by
        /// requirements.md, which lists "Overtime" with no formula — documented here and in
        /// database-design.md rather than invented silently.</summary>
        public static decimal HourlyRate(decimal basicSalary, decimal standardMonthlyHours)
            => standardMonthlyHours <= 0 ? 0m : basicSalary / standardMonthlyHours;

        public static decimal OvertimeAmount(decimal overtimeHours, decimal hourlyRate, decimal multiplier)
            => Math.Round(overtimeHours * hourlyRate * multiplier, 2, MidpointRounding.AwayFromZero);
    }
}
