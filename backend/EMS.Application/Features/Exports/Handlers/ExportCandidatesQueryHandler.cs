using EMS.Application.Common.DTOs;
using EMS.Application.Features.Exports.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Exports.Handlers
{
    public class ExportCandidatesQueryHandler : IRequestHandler<ExportCandidatesQuery, ExportFileResult>
    {
        private static readonly string[] Headers =
        {
            "Candidate Number", "First Name", "Last Name", "Email", "Phone Number",
            "Designation", "Department", "Source", "Applied Date", "Status", "Notes"
        };

        private readonly IRecruitmentRepository _repo;
        private readonly IExcelExportService _excelService;

        public ExportCandidatesQueryHandler(IRecruitmentRepository repo, IExcelExportService excelService)
        {
            _repo = repo;
            _excelService = excelService;
        }

        public async Task<ExportFileResult> Handle(ExportCandidatesQuery request, CancellationToken cancellationToken)
        {
            var candidates = await _repo.GetAllForExportAsync(request.Status, request.DesignationId, request.Search, cancellationToken);

            var rows = new List<IReadOnlyList<object?>>();
            foreach (var c in candidates)
            {
                rows.Add(new List<object?>
                {
                    c.CandidateNumber, c.FirstName, c.LastName, c.Email, c.PhoneNumber,
                    c.Designation?.Name, c.Department?.Name, c.Source, c.AppliedDate, c.Status.ToString(), c.Notes
                });
            }

            var bytes = await _excelService.GenerateAsync("Candidates", Headers, rows);
            var fileName = $"candidates_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

            return new ExportFileResult
            {
                Content = bytes,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = fileName
            };
        }
    }
}
