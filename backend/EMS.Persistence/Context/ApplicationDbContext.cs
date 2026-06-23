using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EMS.Persistence.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<EMS.Domain.Entities.Department> Departments => Set<EMS.Domain.Entities.Department>();
        public DbSet<EMS.Domain.Entities.Employee> Employees => Set<EMS.Domain.Entities.Employee>();
        public DbSet<EMS.Domain.Entities.LeaveType> LeaveTypes => Set<EMS.Domain.Entities.LeaveType>();
        public DbSet<EMS.Domain.Entities.LeaveRequest> LeaveRequests => Set<EMS.Domain.Entities.LeaveRequest>();
        public DbSet<EMS.Domain.Entities.LeaveBalance> LeaveBalances => Set<EMS.Domain.Entities.LeaveBalance>();
        public DbSet<EMS.Domain.Entities.Holiday> Holidays => Set<EMS.Domain.Entities.Holiday>();
        public DbSet<EMS.Domain.Entities.EmployeeDocument> EmployeeDocuments => Set<EMS.Domain.Entities.EmployeeDocument>();
        public DbSet<EMS.Domain.Entities.Notification> Notifications => Set<EMS.Domain.Entities.Notification>();
        public DbSet<EMS.Domain.Entities.SalaryStructure> SalaryStructures => Set<EMS.Domain.Entities.SalaryStructure>();
        public DbSet<EMS.Domain.Entities.Allowance> Allowances => Set<EMS.Domain.Entities.Allowance>();
        public DbSet<EMS.Domain.Entities.Deduction> Deductions => Set<EMS.Domain.Entities.Deduction>();
        public DbSet<EMS.Domain.Entities.PayrollRun> PayrollRuns => Set<EMS.Domain.Entities.PayrollRun>();
        public DbSet<EMS.Domain.Entities.Payslip> Payslips => Set<EMS.Domain.Entities.Payslip>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new Configurations.UserConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.RoleConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.RefreshTokenConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.DepartmentConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.EmployeeConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.LeaveTypeConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.LeaveRequestConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.LeaveBalanceConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.HolidayConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.EmployeeDocumentConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.NotificationConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.SalaryStructureConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.AllowanceConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.DeductionConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.PayrollRunConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.PayslipConfiguration());
        }
    }
}
