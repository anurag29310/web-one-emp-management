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
// DbContext (PostgreSQL)
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IUserRepository, EMS.Persistence.Repositories.UserRepository>();
builder.Services.AddScoped<IRoleRepository, EMS.Persistence.Repositories.RoleRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<EMS.Application.Interfaces.ITeamRepository, EMS.Persistence.Repositories.TeamRepository>();
builder.Services.AddScoped<EMS.Application.Interfaces.IDesignationRepository, EMS.Persistence.Repositories.DesignationRepository>();
builder.Services.AddScoped<EMS.Application.Interfaces.IOfficeLocationRepository, EMS.Persistence.Repositories.OfficeLocationRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<EMS.Application.Interfaces.IAnnouncementRepository, EMS.Persistence.Repositories.AnnouncementRepository>();
builder.Services.AddScoped<EMS.Application.Interfaces.IAuditLogRepository, EMS.Persistence.Repositories.AuditLogRepository>();
builder.Services.AddScoped<EMS.Application.Interfaces.IHealthCheckRepository, EMS.Persistence.Repositories.HealthCheckRepository>();
builder.Services.AddScoped<EMS.Application.Interfaces.IClientRepository, EMS.Persistence.Repositories.ClientRepository>();
builder.Services.AddScoped<EMS.Application.Interfaces.ITaskRepository, EMS.Persistence.Repositories.TaskRepository>();
builder.Services.AddScoped<EMS.Application.Interfaces.IReimbursementRepository, EMS.Persistence.Repositories.ReimbursementRepository>();
builder.Services.AddScoped<EMS.Application.Interfaces.IRecruitmentRepository, EMS.Persistence.Repositories.RecruitmentRepository>();
builder.Services.AddScoped<EMS.Application.Interfaces.IAssetRepository, EMS.Persistence.Repositories.AssetRepository>();
builder.Services.AddScoped<EMS.Application.Interfaces.IPerformanceRepository, EMS.Persistence.Repositories.PerformanceRepository>();

// Background jobs
builder.Services.AddHostedService<EMS.Infrastructure.BackgroundJobs.DailySweepHostedService>();
builder.Services.AddScoped<EMS.Application.Interfaces.IMessagingRepository, EMS.Persistence.Repositories.MessagingRepository>();
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
builder.Services.AddScoped<EMS.Application.Features.Auth.MfaSetupCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Auth.EnableMfaCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Auth.DisableMfaCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Auth.RegenerateMfaRecoveryCodesCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Auth.VerifyMfaCommandHandler>();
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
// Team / Designation / Office Location validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Teams.CreateTeamCommand>, EMS.Application.Features.Teams.Validators.CreateTeamCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Teams.UpdateTeamCommand>, EMS.Application.Features.Teams.Validators.UpdateTeamCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Designations.CreateDesignationCommand>, EMS.Application.Features.Designations.Validators.CreateDesignationCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Designations.UpdateDesignationCommand>, EMS.Application.Features.Designations.Validators.UpdateDesignationCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.OfficeLocations.CreateOfficeLocationCommand>, EMS.Application.Features.OfficeLocations.Validators.CreateOfficeLocationCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.OfficeLocations.UpdateOfficeLocationCommand>, EMS.Application.Features.OfficeLocations.Validators.UpdateOfficeLocationCommandValidator>();
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
// Document validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Documents.Commands.UploadDocumentCommand>, EMS.Application.Features.Documents.Validators.UploadDocumentCommandValidator>();
// Client validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Clients.CreateClientCommand>, EMS.Application.Features.Clients.Validators.CreateClientCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Clients.UpdateClientCommand>, EMS.Application.Features.Clients.Validators.UpdateClientCommandValidator>();
// Task validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Tasks.CreateTaskCommand>, EMS.Application.Features.Tasks.Validators.CreateTaskCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Tasks.UpdateTaskCommand>, EMS.Application.Features.Tasks.Validators.UpdateTaskCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Tasks.ReassignTaskCommand>, EMS.Application.Features.Tasks.Validators.ReassignTaskCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Tasks.UpdateTaskProgressCommand>, EMS.Application.Features.Tasks.Validators.UpdateTaskProgressCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Tasks.AddTaskCommentCommand>, EMS.Application.Features.Tasks.Validators.AddTaskCommentCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Tasks.UploadTaskAttachmentCommand>, EMS.Application.Features.Tasks.Validators.UploadTaskAttachmentCommandValidator>();
// Reimbursement validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Reimbursements.CreateReimbursementCommand>, EMS.Application.Features.Reimbursements.Validators.CreateReimbursementCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Reimbursements.UpdateReimbursementCommand>, EMS.Application.Features.Reimbursements.Validators.UpdateReimbursementCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Reimbursements.RejectReimbursementCommand>, EMS.Application.Features.Reimbursements.Validators.RejectReimbursementCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Reimbursements.RequestChangesReimbursementCommand>, EMS.Application.Features.Reimbursements.Validators.RequestChangesReimbursementCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Reimbursements.UploadReimbursementAttachmentCommand>, EMS.Application.Features.Reimbursements.Validators.UploadReimbursementAttachmentCommandValidator>();

// Recruitment validators (previously missing — see AI_CONTRACT/CLAUDE.md "FluentValidation for all input
// validation": ValidationBehavior resolves IEnumerable<IValidator<T>>, so an unregistered validator is
// silently never invoked rather than erroring, which let this go unnoticed).
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Recruitment.Commands.CreateCandidateCommand>, EMS.Application.Features.Recruitment.Validators.CreateCandidateCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Recruitment.Commands.UpdateCandidateCommand>, EMS.Application.Features.Recruitment.Validators.UpdateCandidateCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Recruitment.Commands.UploadCandidateAttachmentCommand>, EMS.Application.Features.Recruitment.Validators.UploadCandidateAttachmentCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Recruitment.Commands.ScheduleInterviewCommand>, EMS.Application.Features.Recruitment.Validators.ScheduleInterviewCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Recruitment.Commands.RescheduleInterviewCommand>, EMS.Application.Features.Recruitment.Validators.RescheduleInterviewCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Recruitment.Commands.SubmitInterviewFeedbackCommand>, EMS.Application.Features.Recruitment.Validators.SubmitInterviewFeedbackCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Recruitment.Commands.CreateOfferCommand>, EMS.Application.Features.Recruitment.Validators.CreateOfferCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Recruitment.Commands.RejectOfferCommand>, EMS.Application.Features.Recruitment.Validators.RejectOfferCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Recruitment.Commands.AddChecklistItemCommand>, EMS.Application.Features.Recruitment.Validators.AddChecklistItemCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Recruitment.Commands.ConvertCandidateToEmployeeCommand>, EMS.Application.Features.Recruitment.Validators.ConvertCandidateToEmployeeCommandValidator>();

// Asset Management validators (same previously-missing registration gap as Recruitment above).
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Assets.Commands.CreateAssetCommand>, EMS.Application.Features.Assets.Validators.CreateAssetCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Assets.Commands.UpdateAssetCommand>, EMS.Application.Features.Assets.Validators.UpdateAssetCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Assets.Commands.AssignAssetCommand>, EMS.Application.Features.Assets.Validators.AssignAssetCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Assets.Commands.ReturnAssetCommand>, EMS.Application.Features.Assets.Validators.ReturnAssetCommandValidator>();

// Performance Management validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Performance.Commands.CreateGoalCommand>, EMS.Application.Features.Performance.Validators.CreateGoalCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Performance.Commands.UpdateGoalCommand>, EMS.Application.Features.Performance.Validators.UpdateGoalCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Performance.Commands.UpdateGoalProgressCommand>, EMS.Application.Features.Performance.Validators.UpdateGoalProgressCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Performance.Commands.AddGoalKpiCommand>, EMS.Application.Features.Performance.Validators.AddGoalKpiCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Performance.Commands.UpdateGoalKpiProgressCommand>, EMS.Application.Features.Performance.Validators.UpdateGoalKpiProgressCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Performance.Commands.CreateReviewCommand>, EMS.Application.Features.Performance.Validators.CreateReviewCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Performance.Commands.SubmitSelfAssessmentCommand>, EMS.Application.Features.Performance.Validators.SubmitSelfAssessmentCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Performance.Commands.SubmitManagerReviewCommand>, EMS.Application.Features.Performance.Validators.SubmitManagerReviewCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Performance.Commands.CancelReviewCommand>, EMS.Application.Features.Performance.Validators.CancelReviewCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Performance.Commands.ProposePromotionCommand>, EMS.Application.Features.Performance.Validators.ProposePromotionCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Performance.Commands.ApprovePromotionCommand>, EMS.Application.Features.Performance.Validators.ApprovePromotionCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Performance.Commands.RejectPromotionCommand>, EMS.Application.Features.Performance.Validators.RejectPromotionCommandValidator>();

// Messaging validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Messaging.Commands.CreateConversationCommand>, EMS.Application.Features.Messaging.Validators.CreateConversationCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Messaging.Commands.SendMessageCommand>, EMS.Application.Features.Messaging.Validators.SendMessageCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Messaging.Commands.AddParticipantsCommand>, EMS.Application.Features.Messaging.Validators.AddParticipantsCommandValidator>();

// Infrastructure services
builder.Services.AddSingleton<IPasswordHashService, PasswordHashService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddSingleton<EMS.Application.Interfaces.ITotpService, EMS.Infrastructure.Services.TotpService>();
// Backs the TOTP secret's encryption-at-rest. The default key ring is written to the local
// filesystem (see the ASP.NET Core Data Protection docs for the OS-specific path) — fine for a
// single instance, but a real multi-instance deployment needs it persisted to shared storage
// (e.g. Azure Key Vault / Blob) so every instance can decrypt secrets written by any other.
builder.Services.AddDataProtection();
builder.Services.AddSingleton<EMS.Application.Interfaces.IMfaSecretProtector, EMS.Infrastructure.Services.DataProtectionMfaSecretProtector>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<EMS.Application.Interfaces.ICurrentUserService, EMS.Infrastructure.Services.CurrentUserService>();
builder.Services.AddScoped<EMS.Application.Interfaces.IAuditLogger, EMS.Infrastructure.Services.AuditLogger>();
// File storage - local implementation for development
builder.Services.AddSingleton<EMS.Application.Interfaces.IFileStorageService>(sp => new EMS.Infrastructure.Storage.LocalFileStorageService(builder.Environment.ContentRootPath));
builder.Services.AddSingleton<EMS.Application.Interfaces.IEmailSender>(sp => new EMS.Infrastructure.Email.LocalEmailSender(builder.Environment.ContentRootPath, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<EMS.Infrastructure.Email.LocalEmailSender>>()));

// Reverse geocoding for Attendance GPS tracking (Nominatim/OpenStreetMap — no API key required,
// but its usage policy requires a descriptive User-Agent identifying the calling application).
builder.Services.AddHttpClient<EMS.Application.Interfaces.IGeocodingService, EMS.Infrastructure.Services.NominatimGeocodingService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Geocoding:BaseUrl"] ?? "https://nominatim.openstreetmap.org/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd(builder.Configuration["Geocoding:UserAgent"] ?? "EMS-Attendance/1.0 (contact: admin@example.com)");
    client.Timeout = TimeSpan.FromSeconds(5);
});

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
    options.AddPolicy("CanManageClients", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanManageTasks", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanManageReimbursements", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanManageRecruitment", policy => policy.RequireRole("Admin", "HR"));
    options.AddPolicy("CanManageAssets", policy => policy.RequireRole("Admin", "HR"));
    options.AddPolicy("CanManagePerformance", policy => policy.RequireRole("Admin", "HR", "Manager"));
    options.AddPolicy("CanApprovePromotions", policy => policy.RequireRole("Admin", "HR"));
    options.AddPolicy("CanManageMessaging", policy => policy.RequireRole("Admin", "HR"));
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
    // A TOTP code is only 6 digits (1,000,000 possibilities) and valid for ~30-90s, so this
    // endpoint needs its own tighter budget — without one, brute-forcing a single challenge
    // within its validity window is a realistic attack, not just a theoretical one.
    AddFixedWindowIpPolicy(options, "MfaVerifyPolicy", "MfaVerify", defaultPermitLimit: 10, defaultWindowSeconds: 60);

    // Client Master / Task Management / Reimbursement Management are the newest, least battle-tested
    // controllers, so they get a shared budget too — defense in depth against a compromised/buggy
    // authenticated client hammering create/update/status-transition endpoints (e.g. a retry-loop bug
    // spamming duplicate records), not just the two unauthenticated auth endpoints above.
    AddFixedWindowIpPolicy(options, "WriteActionPolicy", "WriteAction", defaultPermitLimit: 100, defaultWindowSeconds: 60);
    // File uploads (task/reimbursement attachments) are disk- and storage-I/O-heavy per request, so
    // they get a tighter budget than other writes — same reasoning as MfaVerify's tighter budget above.
    AddFixedWindowIpPolicy(options, "AttachmentUploadPolicy", "AttachmentUpload", defaultPermitLimit: 20, defaultWindowSeconds: 60);

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

// Applies any pending EF Core migrations (including HasData seeds, e.g. the RBAC roles) on
// startup so the PostgreSQL schema is always up to date with the compiled model. The InMemory
// provider (used by WebApplicationFactory-based tests) has no migrations to run and rejects
// Migrate() outright, so it still goes through EnsureCreated().
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
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
