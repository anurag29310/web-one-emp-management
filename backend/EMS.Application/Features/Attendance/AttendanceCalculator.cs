using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Attendance
{
    /// <summary>Derives late-arrival, early-leave, and worked-minutes facts from a shift assignment.
    /// Night shifts (end time past midnight) are not specially handled — grace/early comparisons
    /// use time-of-day only, which is adequate for day shifts but can misfire across midnight.</summary>
    public static class AttendanceCalculator
    {
        public static bool IsLateArrival(DateTime checkInAtUtc, Shift? shift)
        {
            if (shift == null) return false;
            var threshold = shift.StartTime.Add(TimeSpan.FromMinutes(shift.GraceMinutes));
            return checkInAtUtc.TimeOfDay > threshold;
        }

        public static bool IsEarlyLeave(DateTime checkOutAtUtc, Shift? shift)
        {
            if (shift == null) return false;
            return checkOutAtUtc.TimeOfDay < shift.EndTime;
        }

        public static int? WorkMinutes(DateTime? checkInAtUtc, DateTime? checkOutAtUtc)
        {
            if (checkInAtUtc == null || checkOutAtUtc == null) return null;
            var minutes = (int)(checkOutAtUtc.Value - checkInAtUtc.Value).TotalMinutes;
            return minutes > 0 ? minutes : 0;
        }
    }
}
