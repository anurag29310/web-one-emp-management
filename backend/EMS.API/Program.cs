using EMS.Application.Interfaces;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using EMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<EMS.Application.Interfaces.IAuditLogRepository, EMS.Persistence.Repositories.AuditLogRepository>();
// Payroll services
builder.Services.AddScoped<EMS.Application.Interfaces.IPayrollRepository, EMS.Persistence.Repositories.PayrollRepository>();
builder.Services.AddSingleton<EMS.Application.Interfaces.IPdfService, EMS.Infrastructure.Pdf.SimplePdfService>();
builder.Services.AddScoped<EMS.Application.Interfaces.IReportRepository, EMS.Persistence.Repositories.ReportRepository>();
builder.Services.AddSingleton<EMS.Application.Interfaces.IExportService, EMS.Infrastructure.Export.CsvExportService>();

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

// Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-secret-key-please-change";
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
