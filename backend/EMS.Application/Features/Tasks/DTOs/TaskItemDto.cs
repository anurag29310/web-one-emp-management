using EMS.Domain.Entities;
using System;
using System.Linq;

namespace EMS.Application.Features.Tasks.DTOs
{
    public class TaskItemDto
    {
        public Guid Id { get; set; }
        public string TaskNumber { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public Guid? ClientId { get; set; }
        public string? ClientName { get; set; }
        public string? ClientAddress { get; set; }
        public decimal? ClientLatitude { get; set; }
        public decimal? ClientLongitude { get; set; }
        public Guid AssignedEmployeeId { get; set; }
        public string? AssignedEmployeeName { get; set; }
        public Guid AssignedByUserId { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static TaskItemDto FromEntity(TaskItem t) => new()
        {
            Id = t.Id,
            TaskNumber = t.TaskNumber,
            Title = t.Title,
            Description = t.Description,
            ClientId = t.ClientId,
            ClientName = t.Client?.ClientName,
            ClientAddress = t.Client == null ? null : string.Join(", ", new[]
            {
                t.Client.AddressLine1, t.Client.AddressLine2, t.Client.City, t.Client.State, t.Client.Country
            }.Where(s => !string.IsNullOrWhiteSpace(s))),
            ClientLatitude = t.Client?.Latitude,
            ClientLongitude = t.Client?.Longitude,
            AssignedEmployeeId = t.AssignedEmployeeId,
            AssignedEmployeeName = t.AssignedEmployee == null ? null : $"{t.AssignedEmployee.FirstName} {t.AssignedEmployee.LastName}".Trim(),
            AssignedByUserId = t.AssignedByUserId,
            AssignedDate = t.AssignedDate,
            DueDate = t.DueDate,
            Priority = t.Priority.ToString(),
            Status = t.Status.ToString(),
            Notes = t.Notes,
            CompletedAtUtc = t.CompletedAtUtc,
            CreatedAtUtc = t.CreatedAtUtc,
            UpdatedAtUtc = t.UpdatedAtUtc
        };
    }
}
