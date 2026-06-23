using EMS.Application.DTOs.Reports;
using MediatR;
using System;

namespace EMS.Application.Features.Reports.Queries
{
    public class GetLeaveSummaryQuery : IRequest<LeaveSummaryReportDto>
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}
