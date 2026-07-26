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
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _db;

        public TaskRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        private IQueryable<TaskItem> IncludeRelated(IQueryable<TaskItem> q) =>
            q.Include(t => t.Client).Include(t => t.AssignedEmployee);

        public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await IncludeRelated(_db.Tasks).FirstOrDefaultAsync(t => t.Id == id, ct);

        private IQueryable<TaskItem> BuildFilterQuery(Guid? assignedEmployeeId, Guid? clientId, TaskItemStatus? status, TaskItemPriority? priority)
        {
            var q = _db.Tasks.AsNoTracking();

            if (assignedEmployeeId.HasValue)
                q = q.Where(t => t.AssignedEmployeeId == assignedEmployeeId.Value);
            if (clientId.HasValue)
                q = q.Where(t => t.ClientId == clientId.Value);
            if (status.HasValue)
                q = q.Where(t => t.Status == status.Value);
            if (priority.HasValue)
                q = q.Where(t => t.Priority == priority.Value);

            return q;
        }

        public async Task<IEnumerable<TaskItem>> GetAllAsync(
            int page, int pageSize, Guid? assignedEmployeeId, Guid? clientId,
            TaskItemStatus? status, TaskItemPriority? priority, CancellationToken ct = default) =>
            await IncludeRelated(BuildFilterQuery(assignedEmployeeId, clientId, status, priority))
                .OrderByDescending(t => t.AssignedDate)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<int> CountAsync(
            Guid? assignedEmployeeId, Guid? clientId,
            TaskItemStatus? status, TaskItemPriority? priority, CancellationToken ct = default) =>
            await BuildFilterQuery(assignedEmployeeId, clientId, status, priority).CountAsync(ct);

        public async Task AddAsync(TaskItem task, CancellationToken ct = default) =>
            await _db.Tasks.AddAsync(task, ct);

        public Task UpdateAsync(TaskItem task, CancellationToken ct = default)
        {
            _db.Tasks.Update(task);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);

        public async Task<IEnumerable<TaskComment>> GetCommentsAsync(Guid taskId, CancellationToken ct = default) =>
            await _db.TaskComments.AsNoTracking()
                .Where(c => c.TaskId == taskId)
                .OrderBy(c => c.CreatedAtUtc)
                .ToListAsync(ct);

        public async Task AddCommentAsync(TaskComment comment, CancellationToken ct = default) =>
            await _db.TaskComments.AddAsync(comment, ct);

        public async Task<IEnumerable<TaskAttachment>> GetAttachmentsAsync(Guid taskId, CancellationToken ct = default) =>
            await _db.TaskAttachments.AsNoTracking()
                .Where(a => a.TaskId == taskId)
                .OrderBy(a => a.UploadedAtUtc)
                .ToListAsync(ct);

        public async Task<TaskAttachment?> GetAttachmentByIdAsync(Guid attachmentId, CancellationToken ct = default) =>
            await _db.TaskAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

        public async Task AddAttachmentAsync(TaskAttachment attachment, CancellationToken ct = default) =>
            await _db.TaskAttachments.AddAsync(attachment, ct);
    }
}
