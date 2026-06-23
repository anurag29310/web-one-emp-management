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
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _db;

        public NotificationRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Notification notification)
        {
            await _db.Notifications.AddAsync(notification);
        }

        public async Task<Notification?> GetByIdAsync(Guid id)
            => await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);

        public async Task<IEnumerable<Notification>> GetForUserAsync(Guid userId, int page, int pageSize, bool onlyUnread)
        {
            var q = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId && !n.IsDeleted);
            if (onlyUnread) q = q.Where(n => !n.IsRead);
            return await q.OrderByDescending(n => n.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
            => await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted && (n.ExpiresAtUtc == null || n.ExpiresAtUtc > DateTime.UtcNow));

        public async Task UpdateAsync(Notification notification)
        {
            _db.Notifications.Update(notification);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
