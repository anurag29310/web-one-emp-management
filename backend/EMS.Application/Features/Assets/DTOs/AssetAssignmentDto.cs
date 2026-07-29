using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Assets.DTOs
{
    public class AssetAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public string? AssetTag { get; set; }
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public Guid AssignedByUserId { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public string? ConditionAtAssignment { get; set; }
        public DateTime? ReturnedDate { get; set; }
        public string? ConditionAtReturn { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public static AssetAssignmentDto FromEntity(AssetAssignment a) => new()
        {
            Id = a.Id,
            AssetId = a.AssetId,
            AssetTag = a.Asset?.AssetTag,
            EmployeeId = a.EmployeeId,
            EmployeeName = a.Employee != null ? $"{a.Employee.FirstName} {a.Employee.LastName}".Trim() : null,
            AssignedByUserId = a.AssignedByUserId,
            AssignedDate = a.AssignedDate,
            ExpectedReturnDate = a.ExpectedReturnDate,
            ConditionAtAssignment = a.ConditionAtAssignment,
            ReturnedDate = a.ReturnedDate,
            ConditionAtReturn = a.ConditionAtReturn,
            Notes = a.Notes,
            CreatedAtUtc = a.CreatedAtUtc
        };
    }
}
