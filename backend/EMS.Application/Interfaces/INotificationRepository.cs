using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification);
        Task<IEnumerable<Notification>> GetForUserAsync(Guid userId, int page, int pageSize, bool onlyUnread);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<Notification?> GetByIdAsync(Guid id);
        Task UpdateAsync(Notification notification);
        Task SaveChangesAsync();
    }
}
