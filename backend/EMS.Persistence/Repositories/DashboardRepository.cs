using EMS.Application.DTOs;
using EMS.Application.Interfaces;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly ApplicationDbContext _db;

        public DashboardRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardSummaryDto> GetSummaryAsync()
        {
            var total = await _db.Employees.CountAsync();
            var active = await _db.Employees.CountAsync(e => e.IsActive);
            var inactive = total - active;

            var leaveGroups = await _db.LeaveRequests
                .GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var deptCounts = await _db.Departments
                .GroupJoin(_db.Employees,
                    d => d.Id,
                    e => e.DepartmentId,
                    (d, emps) => new DepartmentSummaryDto
                    {
                        DepartmentId = d.Id,
                        DepartmentName = d.Name,
                        EmployeeCount = emps.Count()
                    })
                .ToListAsync();

            var summary = new DashboardSummaryDto
            {
                TotalEmployees = total,
                ActiveEmployees = active,
                InactiveEmployees = inactive,
                DepartmentSummaries = deptCounts
            };

            // populate leave summary
            foreach (var g in leaveGroups)
            {
                switch (g.Status)
                {
                    case EMS.Domain.Enums.LeaveStatus.Pending:
                        summary.LeaveSummary.Pending = g.Count;
                        break;
                    case EMS.Domain.Enums.LeaveStatus.Approved:
                        summary.LeaveSummary.Approved = g.Count;
                        break;
                    case EMS.Domain.Enums.LeaveStatus.Rejected:
                        summary.LeaveSummary.Rejected = g.Count;
                        break;
                    default:
                        // other statuses may be Cancelled, etc.
                        break;
                }
            }

            // Attendance is not implemented yet; keep zeros

            return summary;
        }
    }
}
