using EMS.Domain.Entities;
using EMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface ITaskRepository
    {
        Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<TaskItem>> GetAllAsync(
            int page, int pageSize, Guid? assignedEmployeeId, Guid? clientId,
            TaskItemStatus? status, TaskItemPriority? priority, CancellationToken ct = default);
        Task<int> CountAsync(
            Guid? assignedEmployeeId, Guid? clientId,
            TaskItemStatus? status, TaskItemPriority? priority, CancellationToken ct = default);
        Task AddAsync(TaskItem task, CancellationToken ct = default);
        Task UpdateAsync(TaskItem task, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);

        Task<IEnumerable<TaskComment>> GetCommentsAsync(Guid taskId, CancellationToken ct = default);
        Task AddCommentAsync(TaskComment comment, CancellationToken ct = default);

        Task<IEnumerable<TaskAttachment>> GetAttachmentsAsync(Guid taskId, CancellationToken ct = default);
        Task<TaskAttachment?> GetAttachmentByIdAsync(Guid attachmentId, CancellationToken ct = default);
        Task AddAttachmentAsync(TaskAttachment attachment, CancellationToken ct = default);
    }
}
