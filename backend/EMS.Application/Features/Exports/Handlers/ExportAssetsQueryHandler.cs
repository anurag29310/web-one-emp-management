using EMS.Application.Common.DTOs;
using EMS.Application.Features.Exports.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Exports.Handlers
{
    public class ExportAssetsQueryHandler : IRequestHandler<ExportAssetsQuery, ExportFileResult>
    {
        private static readonly string[] Headers =
        {
            "Asset Tag", "Category", "Brand", "Model", "Serial Number", "Purchase Date",
            "Purchase Cost", "Status", "Currently Assigned To", "Notes"
        };

        private readonly IAssetRepository _repo;
        private readonly IExcelExportService _excelService;

        public ExportAssetsQueryHandler(IAssetRepository repo, IExcelExportService excelService)
        {
            _repo = repo;
            _excelService = excelService;
        }

        public async Task<ExportFileResult> Handle(ExportAssetsQuery request, CancellationToken cancellationToken)
        {
            var assets = (await _repo.GetAllForExportAsync(request.Status, request.Category, request.Search, cancellationToken)).ToList();

            var activeAssignments = await _repo.GetActiveAssignmentsByAssetIdsAsync(assets.Select(a => a.Id), cancellationToken);
            var assigneeByAssetId = activeAssignments
                .Where(a => a.Employee != null)
                .ToDictionary(a => a.AssetId, a => $"{a.Employee!.FirstName} {a.Employee.LastName}".Trim());

            var rows = new List<IReadOnlyList<object?>>();
            foreach (var asset in assets)
            {
                assigneeByAssetId.TryGetValue(asset.Id, out var assigneeName);
                rows.Add(new List<object?>
                {
                    asset.AssetTag, asset.Category, asset.Brand, asset.Model, asset.SerialNumber, asset.PurchaseDate,
                    asset.PurchaseCost, asset.Status.ToString(), assigneeName, asset.Notes
                });
            }

            var bytes = await _excelService.GenerateAsync("Assets", Headers, rows);
            var fileName = $"assets_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

            return new ExportFileResult
            {
                Content = bytes,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = fileName
            };
        }
    }
}
