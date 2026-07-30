using EMS.Application.Common.DTOs;
using EMS.Domain.Enums;
using MediatR;

namespace EMS.Application.Features.Exports.Queries
{
    /// <summary>Export assets matching the same filters as <see cref="Assets.Queries.GetAssetsQuery"/> to Excel, including each asset's current assignee (if any).</summary>
    public class ExportAssetsQuery : IRequest<ExportFileResult>
    {
        public AssetStatus? Status { get; set; }
        public string? Category { get; set; }
        public string? Search { get; set; }
    }
}
