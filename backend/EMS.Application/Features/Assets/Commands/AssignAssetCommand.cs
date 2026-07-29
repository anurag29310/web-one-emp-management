using MediatR;
using System;

namespace EMS.Application.Features.Assets.Commands
{
    public class AssignAssetCommand : IRequest<Guid>
    {
        public Guid AssetId { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public string? ConditionAtAssignment { get; set; }
        public string? Notes { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }
    }
}
