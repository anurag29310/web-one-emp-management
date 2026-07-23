using EMS.API.Controllers;
using EMS.Application.DTOs;
using EMS.Application.Features.Health.Handlers;
using EMS.Application.Features.Health.Queries;
using EMS.Application.Interfaces;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class HealthCheckTests
    {
        private static ApplicationDbContext CreateDb(string name) =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options);

        [Fact]
        public async Task HealthCheckRepository_CanConnectToDatabaseAsync_ReturnsTrue_WhenDatabaseIsReachable()
        {
            using var db = CreateDb("ems_health_test_" + Guid.NewGuid());
            var repo = new HealthCheckRepository(db);

            var connected = await repo.CanConnectToDatabaseAsync(CancellationToken.None);

            Assert.True(connected);
        }

        [Fact]
        public async Task GetReadinessQueryHandler_ReturnsHealthy_WhenDatabaseConnected()
        {
            var repoMock = new Mock<IHealthCheckRepository>();
            repoMock.Setup(r => r.CanConnectToDatabaseAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var handler = new GetReadinessQueryHandler(repoMock.Object, NullLogger<GetReadinessQueryHandler>.Instance);

            var result = await handler.Handle(new GetReadinessQuery(), CancellationToken.None);

            Assert.Equal("Healthy", result.Status);
            Assert.True(result.DatabaseConnected);
        }

        [Fact]
        public async Task GetReadinessQueryHandler_ReturnsUnhealthy_WhenDatabaseNotConnected()
        {
            var repoMock = new Mock<IHealthCheckRepository>();
            repoMock.Setup(r => r.CanConnectToDatabaseAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            var handler = new GetReadinessQueryHandler(repoMock.Object, NullLogger<GetReadinessQueryHandler>.Instance);

            var result = await handler.Handle(new GetReadinessQuery(), CancellationToken.None);

            Assert.Equal("Unhealthy", result.Status);
            Assert.False(result.DatabaseConnected);
        }

        [Fact]
        public void HealthController_GetHealth_ReturnsOkWithHealthyStatus()
        {
            var controller = new HealthController(Mock.Of<IMediator>());

            var response = Assert.IsType<OkObjectResult>(controller.GetHealth());
            var body = Assert.IsType<ApiResponse<HealthStatusDto>>(response.Value);

            Assert.Equal("Healthy", body.Data.Status);
        }

        [Fact]
        public void HealthController_GetLiveness_ReturnsOkWithHealthyStatus()
        {
            var controller = new HealthController(Mock.Of<IMediator>());

            var response = Assert.IsType<OkObjectResult>(controller.GetLiveness());
            var body = Assert.IsType<ApiResponse<HealthStatusDto>>(response.Value);

            Assert.Equal("Healthy", body.Data.Status);
        }

        [Fact]
        public async Task HealthController_GetReadiness_ReturnsOk_WhenDatabaseConnected()
        {
            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(m => m.Send(It.IsAny<GetReadinessQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReadinessStatusDto { Status = "Healthy", DatabaseConnected = true, TimestampUtc = DateTime.UtcNow });
            var controller = new HealthController(mediatorMock.Object);

            var result = await controller.GetReadiness(CancellationToken.None);

            var response = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ApiResponse<ReadinessStatusDto>>(response.Value);
            Assert.True(body.Data.DatabaseConnected);
        }

        [Fact]
        public async Task HealthController_GetReadiness_Returns503_WhenDatabaseNotConnected()
        {
            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(m => m.Send(It.IsAny<GetReadinessQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReadinessStatusDto { Status = "Unhealthy", DatabaseConnected = false, TimestampUtc = DateTime.UtcNow });
            var controller = new HealthController(mediatorMock.Object);

            var result = await controller.GetReadiness(CancellationToken.None);

            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, response.StatusCode);
        }
    }
}
