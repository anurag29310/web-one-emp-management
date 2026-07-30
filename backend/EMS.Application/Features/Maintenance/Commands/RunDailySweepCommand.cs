using EMS.Application.Features.Maintenance.DTOs;
using MediatR;

namespace EMS.Application.Features.Maintenance.Commands
{
    /// <summary>Runs the daily maintenance sweep: expires Sent offers past their ExpiresAtUtc, and
    /// applies Approved promotions whose EffectiveDate has arrived. Dispatched on a timer by
    /// DailySweepHostedService (EMS.Infrastructure); no request fields — every call sweeps as of
    /// DateTime.UtcNow at execution time.</summary>
    public class RunDailySweepCommand : IRequest<DailySweepResult>
    {
    }
}
