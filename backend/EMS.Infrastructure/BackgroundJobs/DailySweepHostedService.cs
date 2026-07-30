using EMS.Application.Features.Maintenance.Commands;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Infrastructure.BackgroundJobs
{
    /// <summary>Runs RunDailySweepCommand once at startup and then on a recurring interval (default
    /// 24h, configurable via BackgroundJobs:DailySweepIntervalHours). The only background job in this
    /// app so far — expires Sent offers past ExpiresAtUtc and applies Approved promotions whose
    /// EffectiveDate has arrived. A new DI scope is created per run since this service itself is
    /// registered as a singleton but the command handler depends on scoped repositories/DbContext.</summary>
    public class DailySweepHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailySweepHostedService> _logger;
        private readonly TimeSpan _interval;

        public DailySweepHostedService(IServiceScopeFactory scopeFactory, ILogger<DailySweepHostedService> logger, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            var hours = configuration.GetValue<double?>("BackgroundJobs:DailySweepIntervalHours") ?? 24;
            _interval = TimeSpan.FromHours(hours > 0 ? hours : 24);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunOnceAsync(stoppingToken);

                try
                {
                    await timer.WaitForNextTickAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task RunOnceAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(new RunDailySweepCommand(), ct);
                _logger.LogInformation("Daily sweep run complete: {OffersExpired} offer(s) expired, {PromotionsApplied} promotion(s) applied", result.OffersExpired, result.PromotionsApplied);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Daily sweep run failed");
            }
        }
    }
}
