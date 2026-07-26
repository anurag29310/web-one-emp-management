using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Tasks.DTOs
{
    public class TaskCommentDto
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid AuthorUserId { get; set; }
        public string Comment { get; set; } = null!;
        public DateTime CreatedAtUtc { get; set; }

        public static TaskCommentDto FromEntity(TaskComment c) => new()
        {
            Id = c.Id,
            TaskId = c.TaskId,
            AuthorUserId = c.AuthorUserId,
            Comment = c.Comment,
            CreatedAtUtc = c.CreatedAtUtc
        };
    }
}
