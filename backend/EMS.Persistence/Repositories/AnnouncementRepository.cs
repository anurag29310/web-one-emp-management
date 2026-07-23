using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class AnnouncementRepository : IAnnouncementRepository
    {
        private readonly ApplicationDbContext _db;

        public AnnouncementRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Announcement announcement)
        {
            await _db.Announcements.AddAsync(announcement);
        }

        public async Task<Announcement?> GetByIdAsync(Guid id)
            => await _db.Announcements.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        public async Task<Guid?> GetUserDepartmentIdAsync(Guid userId)
        {
            var employeeId = await _db.Users.Where(u => u.Id == userId).Select(u => u.EmployeeId).FirstOrDefaultAsync();
            if (employeeId == null) return null;
            return await _db.Employees.Where(e => e.Id == employeeId).Select(e => (Guid?)e.DepartmentId).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<(Announcement Announcement, bool IsRead)>> GetVisibleForUserAsync(Guid userId, string roleName, int page, int pageSize, bool onlyUnread)
        {
            var departmentId = await GetUserDepartmentIdAsync(userId);
            var now = DateTime.UtcNow;

            var announcements = await _db.Announcements.AsNoTracking()
                .Where(a => !a.IsDeleted
                    && (a.ExpiresAtUtc == null || a.ExpiresAtUtc > now)
                    && (a.AudienceType == "All"
                        || (a.AudienceType == "Department" && a.DepartmentId == departmentId)
                        || (a.AudienceType == "Role" && a.TargetRole == roleName)))
                .OrderByDescending(a => a.CreatedAtUtc)
                .ToListAsync();

            var announcementIds = announcements.Select(a => a.Id).ToList();
            var readIds = (await _db.AnnouncementReads.AsNoTracking()
                .Where(r => r.UserId == userId && announcementIds.Contains(r.AnnouncementId))
                .Select(r => r.AnnouncementId)
                .ToListAsync())
                .ToHashSet();

            IEnumerable<(Announcement, bool)> result = announcements.Select(a => (a, readIds.Contains(a.Id)));
            if (onlyUnread) result = result.Where(x => !x.Item2);

            return result.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        public async Task<(Announcement Announcement, bool IsRead)?> GetVisibleByIdForUserAsync(Guid id, Guid userId, string roleName)
        {
            var departmentId = await GetUserDepartmentIdAsync(userId);
            var now = DateTime.UtcNow;

            var announcement = await _db.Announcements.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id
                && !a.IsDeleted
                && (a.ExpiresAtUtc == null || a.ExpiresAtUtc > now)
                && (a.AudienceType == "All"
                    || (a.AudienceType == "Department" && a.DepartmentId == departmentId)
                    || (a.AudienceType == "Role" && a.TargetRole == roleName)));

            if (announcement == null) return null;

            var isRead = await _db.AnnouncementReads.AsNoTracking().AnyAsync(r => r.AnnouncementId == id && r.UserId == userId);
            return (announcement, isRead);
        }

        public async Task MarkReadAsync(Guid announcementId, Guid userId)
        {
            var alreadyRead = await _db.AnnouncementReads.AnyAsync(r => r.AnnouncementId == announcementId && r.UserId == userId);
            if (alreadyRead) return;

            await _db.AnnouncementReads.AddAsync(new AnnouncementRead
            {
                Id = Guid.NewGuid(),
                AnnouncementId = announcementId,
                UserId = userId,
                ReadAtUtc = DateTime.UtcNow
            });
        }

        public Task SoftDeleteAsync(Announcement announcement)
        {
            announcement.IsDeleted = true;
            announcement.DeletedAtUtc = DateTime.UtcNow;
            _db.Announcements.Update(announcement);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
