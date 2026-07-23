using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IAnnouncementRepository
    {
        Task AddAsync(Announcement announcement);
        Task<Announcement?> GetByIdAsync(Guid id);
        Task<Guid?> GetUserDepartmentIdAsync(Guid userId);
        Task<IEnumerable<(Announcement Announcement, bool IsRead)>> GetVisibleForUserAsync(Guid userId, string roleName, int page, int pageSize, bool onlyUnread);
        Task<(Announcement Announcement, bool IsRead)?> GetVisibleByIdForUserAsync(Guid id, Guid userId, string roleName);
        Task MarkReadAsync(Guid announcementId, Guid userId);
        Task SoftDeleteAsync(Announcement announcement);
        Task SaveChangesAsync();
    }
}
