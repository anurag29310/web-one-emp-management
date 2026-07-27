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
        public DbSet<EMS.Domain.Entities.MfaChallenge> MfaChallenges => Set<EMS.Domain.Entities.MfaChallenge>();
        public DbSet<EMS.Domain.Entities.MfaRecoveryCode> MfaRecoveryCodes => Set<EMS.Domain.Entities.MfaRecoveryCode>();
        public DbSet<EMS.Domain.Entities.Department> Departments => Set<EMS.Domain.Entities.Department>();
        public DbSet<EMS.Domain.Entities.Team> Teams => Set<EMS.Domain.Entities.Team>();
        public DbSet<EMS.Domain.Entities.Designation> Designations => Set<EMS.Domain.Entities.Designation>();
        public DbSet<EMS.Domain.Entities.OfficeLocation> OfficeLocations => Set<EMS.Domain.Entities.OfficeLocation>();
        public DbSet<EMS.Domain.Entities.Employee> Employees => Set<EMS.Domain.Entities.Employee>();
        public DbSet<EMS.Domain.Entities.LeaveType> LeaveTypes => Set<EMS.Domain.Entities.LeaveType>();
        public DbSet<EMS.Domain.Entities.LeaveRequest> LeaveRequests => Set<EMS.Domain.Entities.LeaveRequest>();
        public DbSet<EMS.Domain.Entities.LeaveBalance> LeaveBalances => Set<EMS.Domain.Entities.LeaveBalance>();
        public DbSet<EMS.Domain.Entities.Holiday> Holidays => Set<EMS.Domain.Entities.Holiday>();
        public DbSet<EMS.Domain.Entities.EmployeeDocument> EmployeeDocuments => Set<EMS.Domain.Entities.EmployeeDocument>();
        public DbSet<EMS.Domain.Entities.Notification> Notifications => Set<EMS.Domain.Entities.Notification>();
        public DbSet<EMS.Domain.Entities.Announcement> Announcements => Set<EMS.Domain.Entities.Announcement>();
        public DbSet<EMS.Domain.Entities.AnnouncementRead> AnnouncementReads => Set<EMS.Domain.Entities.AnnouncementRead>();
        public DbSet<EMS.Domain.Entities.SalaryStructure> SalaryStructures => Set<EMS.Domain.Entities.SalaryStructure>();
        public DbSet<EMS.Domain.Entities.Allowance> Allowances => Set<EMS.Domain.Entities.Allowance>();
        public DbSet<EMS.Domain.Entities.Deduction> Deductions => Set<EMS.Domain.Entities.Deduction>();
        public DbSet<EMS.Domain.Entities.PayrollRun> PayrollRuns => Set<EMS.Domain.Entities.PayrollRun>();
        public DbSet<EMS.Domain.Entities.Payslip> Payslips => Set<EMS.Domain.Entities.Payslip>();
        public DbSet<EMS.Domain.Entities.AttendanceRecord> AttendanceRecords => Set<EMS.Domain.Entities.AttendanceRecord>();
        public DbSet<EMS.Domain.Entities.AuditLog> AuditLogs => Set<EMS.Domain.Entities.AuditLog>();
        public DbSet<EMS.Domain.Entities.Shift> Shifts => Set<EMS.Domain.Entities.Shift>();
        public DbSet<EMS.Domain.Entities.EmployeeShift> EmployeeShifts => Set<EMS.Domain.Entities.EmployeeShift>();
        public DbSet<EMS.Domain.Entities.AttendanceCorrection> AttendanceCorrections => Set<EMS.Domain.Entities.AttendanceCorrection>();
        public DbSet<EMS.Domain.Entities.Client> Clients => Set<EMS.Domain.Entities.Client>();
        public DbSet<EMS.Domain.Entities.TaskItem> Tasks => Set<EMS.Domain.Entities.TaskItem>();
        public DbSet<EMS.Domain.Entities.TaskComment> TaskComments => Set<EMS.Domain.Entities.TaskComment>();
        public DbSet<EMS.Domain.Entities.TaskAttachment> TaskAttachments => Set<EMS.Domain.Entities.TaskAttachment>();
        public DbSet<EMS.Domain.Entities.Reimbursement> Reimbursements => Set<EMS.Domain.Entities.Reimbursement>();
        public DbSet<EMS.Domain.Entities.ReimbursementAttachment> ReimbursementAttachments => Set<EMS.Domain.Entities.ReimbursementAttachment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new Configurations.UserConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.RoleConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.RefreshTokenConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.MfaChallengeConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.MfaRecoveryCodeConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.DepartmentConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.TeamConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.DesignationConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.OfficeLocationConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.EmployeeConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.LeaveTypeConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.LeaveRequestConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.LeaveBalanceConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.HolidayConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.EmployeeDocumentConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.NotificationConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.AnnouncementConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.AnnouncementReadConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.SalaryStructureConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.AllowanceConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.DeductionConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.PayrollRunConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.PayslipConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.AttendanceRecordConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.AuditLogConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.ShiftConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.EmployeeShiftConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.AttendanceCorrectionConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.ClientConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.TaskItemConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.TaskCommentConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.TaskAttachmentConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.ReimbursementConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.ReimbursementAttachmentConfiguration());
        }
    }
}
