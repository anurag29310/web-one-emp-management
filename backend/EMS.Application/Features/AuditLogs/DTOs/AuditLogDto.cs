using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.AuditLogs.DTOs
{
    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string EntityName { get; set; } = null!;
        public Guid? EntityId { get; set; }
        public string Action { get; set; } = null!;
        public string? OldValuesJson { get; set; }
        public string? NewValuesJson { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public static AuditLogDto FromEntity(AuditLog log) => new()
        {
            Id = log.Id,
            UserId = log.UserId,
            EntityName = log.EntityName,
            EntityId = log.EntityId,
            Action = log.Action,
            OldValuesJson = log.OldValuesJson,
            NewValuesJson = log.NewValuesJson,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            CreatedAtUtc = log.CreatedAtUtc
        };
    }
}
