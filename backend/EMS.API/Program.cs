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
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
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
builder.Services.AddScoped<EMS.Application.Features.Departments.Handlers.CreateDepartmentCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Departments.Handlers.UpdateDepartmentCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Departments.Handlers.DeleteDepartmentCommandHandler>();
builder.Services.AddScoped<EMS.Application.Features.Departments.Handlers.GetDepartmentsQueryHandler>();
builder.Services.AddScoped<EMS.Application.Features.Departments.Handlers.GetDepartmentByIdQueryHandler>();

// Employees handlers are registered by MediatR; repository registered above

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(EMS.Application.Features.Employees.Commands.CreateEmployeeCommand).Assembly);
});

// Validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Employees.Commands.CreateEmployeeCommand>, EMS.Application.Features.Employees.Validators.EmployeeCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Employees.Commands.UpdateEmployeeCommand>, EMS.Application.Features.Employees.Validators.UpdateEmployeeCommandValidator>();
// Payroll validators
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Payroll.Commands.CreateSalaryStructureCommand>, EMS.Application.Features.Payroll.Validators.CreateSalaryStructureCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Payroll.Commands.UpdateSalaryStructureCommand>, EMS.Application.Features.Payroll.Validators.UpdateSalaryStructureCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Payroll.Commands.ApprovePayrollRunCommand>, EMS.Application.Features.Payroll.Validators.ApprovePayrollRunCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<EMS.Application.Features.Payroll.Queries.DryRunPayrollQuery>, EMS.Application.Features.Payroll.Validators.DryRunPayrollQueryValidator>();

// Infrastructure services
builder.Services.AddSingleton<IPasswordHashService, PasswordHashService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
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
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
