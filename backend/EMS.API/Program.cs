using EMS.Application.Interfaces;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using EMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

// Bootstrap logger: captures anything that happens before the host's own
// Serilog pipeline (built from appsettings) is available, including failures
// during configuration/DI setup.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting EMS API host");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "EMS.API")
        .Enrich.WithProperty("MachineName", Environment.MachineName)
        .Enrich.WithProperty("EnvironmentName", context.HostingEnvironment.EnvironmentName));

    // Add services to the container.
    builder.Services.AddControllers();
// DbContext (in-memory for dev)
builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase("EMS"));

// Repositories
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IUserRepository, EMS.Persistence.Repositories.UserRepository>();
builder.Services.AddScoped<IRoleRepository, EMS.Persistence.Repositories.RoleRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<EMS.Application.Interfaces.IAnnouncementRepository, EMS.Persistence.Repositories.AnnouncementRepository>();
builder.Services.AddScoped<EMS.Application.Interfaces.IAuditLogRepository, EMS.Persistence.Repositories.AuditLogRepository>();
builder.Services.AddScoped<EMS.Application.Interfaces.IHealthCheckRepository, EMS.Persistence.Repositories.HealthCheckRepository>();
// Payroll services
builder.Services.AddScoped<EMS.Application.Interfaces.IPayrollRepository, EMS.Persistence.Repositories.PayrollRepository>();
builder.Services.AddSingleton<EMS.Application.Interfaces.IPdfService, EMS.Infrastructure.Pdf.PdfSharpDocumentService>();
builder.Services.AddScoped<EMS.Application.Interfaces.IReportRepository, EMS.Persistence.Repositories.ReportRepository>();
builder.Services.AddSingleton<EMS.Application.Interfaces.IExportService, EMS.Infrastructure.Export.CsvExportService>();
builder.Services.AddSingleton<EMS.Application.Interfaces.IExcelExportService, EMS.Infrastructure.Export.ClosedXmlExportService>();

// Application handlers — Auth
builder.Services.AddScoped<EMS.Application.Features.Auth.LoginCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Auth.RegisterUserCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Auth.RefreshTokenCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Auth.LogoutCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Auth.LogoutAllCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Auth.ForgotPasswordCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Auth.ResetPasswordCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Auth.ChangePasswordCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Auth.GetCurrentUserQueryHandler>();
builder.Services.AddScoped<EMS.Application.Features.Payroll.Handlers.CreateSalaryStructureCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Payroll.Handlers.GetSalaryStructuresQueryHandler>();
builder.Services.AddScoped<EMS.Application.Features.Payroll.Handlers.GetSalaryStructureByIdQueryHandler>();
builder.Services.AddScoped<EMS.Application.Features.Payroll.Handlers.UpdateSalaryStructureCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Payroll.Handlers.DeleteSalaryStructureCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Payroll.Handlers.DryRunPayrollQueryHandler>();
builder.Services.AddScoped<EMS.Application.Features.Payroll.Handlers.GetPayrollRunsQueryHandler>();
builder.Services.AddScoped<EMS.Application.Features.Payroll.Handlers.ApprovePayrollRunCommandHandler>();

// Employees and Departments handlers are registered by MediatR; repositories registered above

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(EMS.Application.Features.Employees.Commands.CreateEmployeeCommand).Assembly);
    cfg.AddOpenBehavior(typeof(EMS.Application.Common.Behaviors.ValidationBehavior<,>));
});

// Validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Employees.Commands.CreateEmployeeCommand>, EMS.Application.Features.Employees.Validators.EmployeeCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Employees.Commands.UpdateEmployeeCommand>, EMS.Application.Features.Employees.Validators.UpdateEmployeeCommandValidator>();
// Department validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Departments.CreateDepartmentCommand>, EMS.Application.Features.Departments.Validators.CreateDepartmentCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Departments.UpdateDepartmentCommand>, EMS.Application.Features.Departments.Validators.UpdateDepartmentCommandValidator>();
// Payroll validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Payroll.Commands.CreateSalaryStructureCommand>, EMS.Application.Features.Payroll.Validators.CreateSalaryStructureCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Payroll.Commands.UpdateSalaryStructureCommand>, EMS.Application.Features.Payroll.Validators.UpdateSalaryStructureCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Payroll.Commands.ApprovePayrollRunCommand>, EMS.Application.Features.Payroll.Validators.ApprovePayrollRunCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Payroll.Queries.DryRunPayrollQuery>, EMS.Application.Features.Payroll.Validators.DryRunPayrollQueryValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Payroll.Commands.ProcessPayrollCommand>, EMS.Application.Features.Payroll.Validators.ProcessPayrollCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Payroll.Queries.GetPayslipsForEmployeeQuery>, EMS.Application.Features.Payroll.Validators.GetPayslipsForEmployeeQueryValidator>();
// Dashboard validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Dashboard.Queries.GetDashboardSummaryQuery>, EMS.Application.Features.Dashboard.Validators.GetDashboardSummaryQueryValidator>();
// Announcement validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Announcements.Commands.CreateAnnouncementCommand>, EMS.Application.Features.Announcements.Validators.CreateAnnouncementCommandValidator>();
// Export validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Exports.Queries.ExportAttendanceQuery>, EMS.Application.Features.Exports.Validators.ExportAttendanceQueryValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Exports.Queries.ExportDashboardSummaryQuery>, EMS.Application.Features.Exports.Validators.ExportDashboardSummaryQueryValidator>();
// Leave validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Leave.Commands.CreateLeaveRequestCommand>, EMS.Application.Features.Leave.Validators.CreateLeaveRequestCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Leave.Commands.UpdateLeaveRequestCommand>, EMS.Application.Features.Leave.Validators.UpdateLeaveRequestCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Leave.Commands.AdjustLeaveBalanceCommand>, EMS.Application.Features.Leave.Validators.AdjustLeaveBalanceCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Leave.Commands.CreateLeaveTypeCommand>, EMS.Application.Features.Leave.Validators.CreateLeaveTypeCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Leave.Commands.UpdateLeaveTypeCommand>, EMS.Application.Features.Leave.Validators.UpdateLeaveTypeCommandValidator>();
// Attendance validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Attendance.Commands.CheckInCommand>, EMS.Application.Features.Attendance.Validators.CheckInCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Attendance.Commands.CheckOutCommand>, EMS.Application.Features.Attendance.Validators.CheckOutCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Attendance.Commands.CreateAttendanceRecordCommand>, EMS.Application.Features.Attendance.Validators.CreateAttendanceRecordCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Attendance.Commands.UpdateAttendanceRecordCommand>, EMS.Application.Features.Attendance.Validators.UpdateAttendanceRecordCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Attendance.Commands.CreateAttendanceCorrectionCommand>, EMS.Application.Features.Attendance.Validators.CreateAttendanceCorrectionCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Attendance.Commands.CreateShiftCommand>, EMS.Application.Features.Attendance.Validators.CreateShiftCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Attendance.Commands.UpdateShiftCommand>, EMS.Application.Features.Attendance.Validators.UpdateShiftCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Attendance.Commands.AssignEmployeeShiftCommand>, EMS.Application.Features.Attendance.Validators.AssignEmployeeShiftCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Attendance.Commands.UpdateEmployeeShiftCommand>, EMS.Application.Features.Attendance.Validators.UpdateEmployeeShiftCommandValidator>();
// Reports validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Reports.Queries.GetLeaveSummaryQuery>, EMS.Application.Features.Reports.Validators.GetLeaveSummaryQueryValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Reports.Queries.GetEmployeeJoinExitQuery>, EMS.Application.Features.Reports.Validators.GetEmployeeJoinExitQueryValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Reports.Queries.ExportEmployeeJoinExitQuery>, EMS.Application.Features.Reports.Validators.ExportEmployeeJoinExitQueryValidator>();
// Audit log validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.AuditLogs.Queries.GetAuditLogsQuery>, EMS.Application.Features.AuditLogs.Validators.GetAuditLogsQueryValidator>();
// User validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Users.Commands.CreateUserCommand>, EMS.Application.Features.Users.Validators.CreateUserCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Users.Commands.UpdateUserCommand>, EMS.Application.Features.Users.Validators.UpdateUserCommandValidator>();
// Role validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Roles.Commands.CreateRoleCommand>, EMS.Application.Features.Roles.Validators.CreateRoleCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Roles.Commands.UpdateRoleCommand>, EMS.Application.Features.Roles.Validators.UpdateRoleCommandValidator>();

// Infrastructure services
builder.Services.AddSingleton<IPasswordHashService, PasswordHashService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<EMS.Application.Interfaces.ICurrentUserService, EMS.Infrastructure.Services.CurrentUserService>();
builder.Services.AddScoped<EMS.Application.Interfaces.IAuditLogger, EMS.Infrastructure.Services.AuditLogger>();
// File storage - local implementation for development
builder.Services.AddSingleton<EMS.Application.Interfaces.IFileStorageService>(sp => new EMS.Infrastructure.Storage.LocalFileStorageService(builder.Environment.ContentRootPath));
builder.Services.AddSingleton<EMS.Application.Interfaces.IEmailSender>(sp => new EMS.Infrastructure.Email.LocalEmailSender(builder.Environment.ContentRootPath, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<EMS.Infrastructure.Email.LocalEmailSender>>()));

builder.Services.AddSwaggerGen();

// CORS: the frontend (a React SPA on a different origin) authenticates with a Bearer token in
// the Authorization header, not cookies, so credentials are never needed here. Falls back to
// Vite's default dev server ports if Cors:AllowedOrigins isn't configured; production must set
// it explicitly (e.g. via the Cors__AllowedOrigins__0 environment variable) to its real domain.
const string CorsPolicyName = "Frontend";
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOrigins == null || corsOrigins.Length == 0)
{
    corsOrigins = new[] { "http://localhost:5173", "https://localhost:5173" };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
EMS.Infrastructure.Services.JwtKeyValidator.EnsureValid(jwtKey);
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ems";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
        };
    });

builder.Services.AddAuthorization();

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageEmployees", policy => policy.RequireRole("Admin", "HR"));
    options.AddPolicy("CanViewEmployees", policy => policy.RequireRole("Admin", "HR", "Manager"));
    options.AddPolicy("CanManageLeaves", policy => policy.RequireRole("Admin", "HR", "Manager"));
    options.AddPolicy("CanManageDepartments", policy => policy.RequireRole("Admin", "HR"));
    options.AddPolicy("CanApproveLeave", policy => policy.RequireRole("Admin", "HR", "Manager"));
    options.AddPolicy("CanManageLeaveTypes", policy => policy.RequireRole("Admin", "HR"));
    options.AddPolicy("CanManageHolidays", policy => policy.RequireRole("Admin", "HR"));
    options.AddPolicy("CanManageAttendanceRecords", policy => policy.RequireRole("Admin", "HR"));
    options.AddPolicy("CanReviewAttendanceCorrections", policy => policy.RequireRole("Admin", "HR", "Manager"));
    options.AddPolicy("CanManageShifts", policy => policy.RequireRole("Admin", "HR"));
    options.AddPolicy("CanViewDashboard", policy => policy.RequireRole("Admin", "HR", "Manager"));
    options.AddPolicy("CanManagePayroll", policy => policy.RequireRole("Admin", "HR"));
    options.AddPolicy("CanApprovePayroll", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanViewReports", policy => policy.RequireRole("Admin", "HR", "Manager"));
    options.AddPolicy("CanViewAuditLogs", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanManageUsers", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanViewRoles", policy => policy.RequireRole("Admin", "HR"));
});

// Rate limiting: login and register are the two unauthenticated endpoints an attacker can hammer
// to brute-force credentials or mass-create accounts, so each gets its own fixed-window budget
// keyed by client IP. Separate policies (rather than one shared "auth" bucket) so a user
// exhausting the login limit with bad passwords can't also lock themselves out of registering,
// and vice versa. Limits are configurable per environment; defaults are deliberately strict.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    AddFixedWindowIpPolicy(options, "LoginPolicy", "Login", defaultPermitLimit: 5, defaultWindowSeconds: 60);
    AddFixedWindowIpPolicy(options, "RegisterPolicy", "Register", defaultPermitLimit: 5, defaultWindowSeconds: 60);

    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }

        context.HttpContext.Response.ContentType = "application/json";
        var error = new EMS.API.Controllers.ApiErrorResponse
        {
            Status = StatusCodes.Status429TooManyRequests,
            Code = "RATE_LIMIT_EXCEEDED",
            Message = "Too many requests. Please try again later."
        };
        await context.HttpContext.Response.WriteAsJsonAsync(error, cancellationToken);
    };

    void AddFixedWindowIpPolicy(RateLimiterOptions rateLimiterOptions, string policyName, string configSection, int defaultPermitLimit, int defaultWindowSeconds)
    {
        var permitLimit = builder.Configuration.GetValue<int?>($"RateLimiting:{configSection}:PermitLimit") ?? defaultPermitLimit;
        var windowSeconds = builder.Configuration.GetValue<int?>($"RateLimiting:{configSection}:WindowSeconds") ?? defaultWindowSeconds;

        rateLimiterOptions.AddPolicy(policyName, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    }
});

var app = builder.Build();

// The in-memory provider never creates its store (or applies HasData seeds, e.g. the RBAC
// roles) on its own — it has no migrations to run. EnsureCreated() triggers both. This must be
// replaced with a real Database.Migrate() call once a persistent provider is configured.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<EMS.API.Middleware.ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            diagnosticContext.Set("UserId", userId);
        }
    };
});

app.UseHttpsRedirection();

app.UseCors(CorsPolicyName);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "EMS API host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Exposed so Microsoft.AspNetCore.Mvc.Testing's WebApplicationFactory<Program> can host this
// app in-process for integration tests.
public partial class Program { }
