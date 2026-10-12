using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.School;
using SMIS.Domain.Entities.Students;
using SMIS.Domain.Entities.Finance;
using SMIS.Domain.Entities.Academic;
using SMIS.Domain.Entities.Library;
using SMIS.Domain.Entities.Security;
namespace SMIS.Infrastructure.Data;
using SMIS.Domain.Entities.Accounting;
/// <summary>
/// Main database context for the SMIS application.
/// This is the bridge between C# code and SQL Server.
/// Inherits from IdentityDbContext to include login/user tables automatically.
/// </summary>
public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
{
}

protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.ConfigureWarnings(w =>
        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics
            .RelationalEventId.PendingModelChangesWarning));
}

    // ── School Setup Tables ──────────────────────────────────────
    public DbSet<SchoolProfile> SchoolProfiles => Set<SchoolProfile>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<AcademicTerm> AcademicTerms => Set<AcademicTerm>();
    public DbSet<ClassLevel> ClassLevels => Set<ClassLevel>();
    public DbSet<ClassStream> ClassStreams => Set<ClassStream>();
    public DbSet<Subject> Subjects => Set<Subject>();

    // ── Student Tables ───────────────────────────────────────────
    public DbSet<Student> Students => Set<Student>();
public DbSet<StudentClassHistory> StudentClassHistories => Set<StudentClassHistory>();
public DbSet<SMIS.Domain.Entities.Students.ParentStudentLink> ParentStudentLinks { get; set; }
public DbSet<SMIS.Domain.Entities.Communication.StudentFeedback> StudentFeedbacks { get; set; }
public DbSet<SMIS.Domain.Entities.Communication.Newsletter> Newsletters { get; set; }

    // ── Academic Tables ──────────────────────────────────────────
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    // ── Academic Tables (CBC) ────────────────────────────────────
    public DbSet<CBCAssessment> CBCAssessments => Set<CBCAssessment>();
    public DbSet<CBCReportCard> CBCReportCards => Set<CBCReportCard>();

    // ── Library Tables ───────────────────────────────────────────
    public DbSet<BookCategory> BookCategories => Set<BookCategory>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookIssue> BookIssues => Set<BookIssue>();

    // ── Security Tables ──────────────────────────────────────────
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

// ── Accounting Tables ─────────────────────────────────────────
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();

// ── Supplier Tables ───────────────────────────────────────────
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierInvoice> SupplierInvoices => Set<SupplierInvoice>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
// ─
// ─ Payroll Tables ───────────────────────────────────────────
public DbSet<StaffPayroll> StaffPayrolls => Set<StaffPayroll>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollRunLine> PayrollRunLines => Set<PayrollRunLine>();

    // ── Expense Tables ───────────────────────────────────────────
    public DbSet<Expense> Expenses => Set<Expense>();

    // ── Cashbook Tables ──────────────────────────────────────────
public DbSet<CashbookEntry> CashbookEntries => Set<CashbookEntry>();

    // ── Finance Tables ───────────────────────────────────────────
    public DbSet<FeeCategory> FeeCategories => Set<FeeCategory>();
    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<StudentInvoice> StudentInvoices => Set<StudentInvoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<FeePayment> FeePayments => Set<FeePayment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    
        // SchoolProfile configuration
        builder.Entity<SchoolProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.KemisCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.PrimaryColor).HasMaxLength(10);
            entity.Property(e => e.SecondaryColor).HasMaxLength(10);
        });// Attendance configuration
        builder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StudentId, e.AttendanceDate }).IsUnique();
            entity.HasOne(e => e.Student)
                  .WithMany()
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ClassLevel)
                  .WithMany()
                  .HasForeignKey(e => e.ClassLevelId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AcademicTerm)
                  .WithMany()
                  .HasForeignKey(e => e.AcademicTermId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // AcademicYear configuration
        builder.Entity<AcademicYear>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(20);
            entity.HasMany(e => e.Terms)
                  .WithOne(t => t.AcademicYear)
                  .HasForeignKey(t => t.AcademicYearId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ClassLevel configuration
        builder.Entity<ClassLevel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasMany(e => e.Streams)
                  .WithOne(s => s.ClassLevel)
                  .HasForeignKey(s => s.ClassLevelId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Subject configuration
        builder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(10);
        });
        // Student configuration
        builder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AdmissionNumber).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.AdmissionNumber).IsUnique();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MiddleName).HasMaxLength(50);
            entity.HasOne(e => e.ClassLevel)
                  .WithMany()
                  .HasForeignKey(e => e.ClassLevelId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ClassStream)
                  .WithMany()
                  .HasForeignKey(e => e.ClassStreamId)
                  .OnDelete(DeleteBehavior.NoAction);
        });
        // Finance configurations
        builder.Entity<FeeStructure>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
    entity.HasOne(e => e.FeeCategory)
          .WithMany(c => c.FeeStructures)
          .HasForeignKey(e => e.FeeCategoryId)
          .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.ClassLevel)
          .WithMany()
          .HasForeignKey(e => e.ClassLevelId)
          .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.AcademicYear)
          .WithMany()
          .HasForeignKey(e => e.AcademicYearId)
          .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.AcademicTerm)
          .WithMany()
          .HasForeignKey(e => e.AcademicTermId)
          .OnDelete(DeleteBehavior.Restrict);
});

        builder.Entity<StudentInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalPaid).HasColumnType("decimal(18,2)");
            entity.Ignore(e => e.Balance);
            entity.HasOne(e => e.Student)
                  .WithMany()
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AcademicYear)
                  .WithMany()
                  .HasForeignKey(e => e.AcademicYearId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AcademicTerm)
                  .WithMany()
                  .HasForeignKey(e => e.AcademicTermId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Discount).HasColumnType("decimal(18,2)");
            entity.Ignore(e => e.NetAmount);
            entity.HasOne(e => e.StudentInvoice)
                  .WithMany(i => i.Items)
                  .HasForeignKey(e => e.StudentInvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.FeeCategory)
                  .WithMany()
                  .HasForeignKey(e => e.FeeCategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FeePayment>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
    entity.HasOne(e => e.StudentInvoice)
          .WithMany(i => i.Payments)
          .HasForeignKey(e => e.StudentInvoiceId)
          .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.Student)
          .WithMany()
          .HasForeignKey(e => e.StudentId)
          .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.JournalEntry)
          .WithMany()
          .HasForeignKey(e => e.JournalEntryId)
          .OnDelete(DeleteBehavior.Restrict);
});
        // CBC Assessment configuration
        builder.Entity<CBCAssessment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Score).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaxScore).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Student)
                  .WithMany()
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Subject)
                  .WithMany()
                  .HasForeignKey(e => e.SubjectId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AcademicTerm)
                  .WithMany()
                  .HasForeignKey(e => e.AcademicTermId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AcademicYear)
                  .WithMany()
                  .HasForeignKey(e => e.AcademicYearId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ClassLevel)
                  .WithMany()
                  .HasForeignKey(e => e.ClassLevelId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // CBC Report Card configuration
        builder.Entity<CBCReportCard>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Student)
                  .WithMany()
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AcademicTerm)
                  .WithMany()
                  .HasForeignKey(e => e.AcademicTermId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AcademicYear)
                  .WithMany()
                  .HasForeignKey(e => e.AcademicYearId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ClassLevel)
                  .WithMany()
                  .HasForeignKey(e => e.ClassLevelId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Assessments)
                  .WithOne()
                  .OnDelete(DeleteBehavior.Restrict);
        });// Library configurations
        builder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Books)
                  .HasForeignKey(e => e.BookCategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<BookIssue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FineAmount).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Book)
                  .WithMany(b => b.Issues)
                  .HasForeignKey(e => e.BookId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Student)
                  .WithMany()
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });// Security configurations
        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoleName, e.Module }).IsUnique();
            entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Module).IsRequired().HasMaxLength(50);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserEmail).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Module).IsRequired().HasMaxLength(50);
        });
        // Accounting configurations
        builder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OpeningBalance)
                  .HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.ParentAccount)
                  .WithMany(a => a.SubAccounts)
                  .HasForeignKey(e => e.ParentAccountId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FeeCategory>(entity =>
{
    entity.HasOne(e => e.IncomeAccount)
          .WithMany()
          .HasForeignKey(e => e.IncomeAccountId)
          .OnDelete(DeleteBehavior.Restrict);
});

        builder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntryNumber)
                  .IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.EntryNumber).IsUnique();
            entity.Ignore(e => e.TotalDebits);
            entity.Ignore(e => e.TotalCredits);
            entity.Ignore(e => e.IsBalanced);
            entity.HasOne(e => e.AcademicTerm)
                  .WithMany()
                  .HasForeignKey(e => e.AcademicTermId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<JournalEntryLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DebitAmount)
                  .HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreditAmount)
                  .HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.JournalEntry)
                  .WithMany(j => j.Lines)
                  .HasForeignKey(e => e.JournalEntryId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Account)
                  .WithMany(a => a.JournalLines)
                  .HasForeignKey(e => e.AccountId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<CashbookEntry>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
    entity.Property(e => e.EntryNumber).IsRequired().HasMaxLength(20);
    entity.HasIndex(e => e.EntryNumber).IsUnique();
    entity.HasOne(e => e.CashAccountRef)
          .WithMany()
          .HasForeignKey(e => e.CashAccountId)
          .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.ContraAccount)
          .WithMany()
          .HasForeignKey(e => e.ContraAccountId)
          .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.JournalEntry)
          .WithMany()
          .HasForeignKey(e => e.JournalEntryId)
          .OnDelete(DeleteBehavior.Restrict);
});
    // SupplierInvoice configuration
    builder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreditLimit)
                  .HasColumnType("decimal(18,2)");
        });

        builder.Entity<SupplierInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalAmount)
                  .HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalPaid)
                  .HasColumnType("decimal(18,2)");
            entity.Ignore(e => e.Balance);
            entity.HasOne(e => e.Supplier)
                  .WithMany(s => s.Invoices)
                  .HasForeignKey(e => e.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ExpenseAccount)
                  .WithMany()
                  .HasForeignKey(e => e.ExpenseAccountId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SupplierPayment>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Amount)
          .HasColumnType("decimal(18,2)");
    entity.HasOne(e => e.SupplierInvoice)
          .WithMany(i => i.Payments)
          .HasForeignKey(e => e.SupplierInvoiceId)
          .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.Supplier)
          .WithMany()
          .HasForeignKey(e => e.SupplierId)
          .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.PaidFromAccount)
          .WithMany()
          .HasForeignKey(e => e.PaidFromAccountId)
          .OnDelete(DeleteBehavior.Restrict);
});

        builder.Entity<Expense>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Amount)
          .HasColumnType("decimal(18,2)");
    entity.HasOne(e => e.ExpenseAccount)
          .WithMany()
          .HasForeignKey(e => e.ExpenseAccountId)
          .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.PaidFromAccount)
          .WithMany()
          .HasForeignKey(e => e.PaidFromAccountId)
          .OnDelete(DeleteBehavior.Restrict);
});
        // StaffPayroll configuration
        builder.Entity<StaffPayroll>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BasicSalary)
                  .HasColumnType("decimal(18,2)");
            entity.Property(e => e.HouseAllowance)
                  .HasColumnType("decimal(18,2)");
            entity.Property(e => e.TransportAllowance)
                  .HasColumnType("decimal(18,2)");
            entity.Property(e => e.MedicalAllowance)
                  .HasColumnType("decimal(18,2)");
            entity.Property(e => e.OtherAllowances)
                  .HasColumnType("decimal(18,2)");
            entity.Property(e => e.SACCODeduction)
                  .HasColumnType("decimal(18,2)");
            entity.Property(e => e.LoanDeduction)
                  .HasColumnType("decimal(18,2)");
            entity.Property(e => e.OtherDeductions)
                  .HasColumnType("decimal(18,2)");
            entity.Ignore(e => e.GrossSalary);
        });

        builder.Entity<PayrollRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalGross).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalPAYE).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalNSSF).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalSHA).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalDeductions).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalNetPay).HasColumnType("decimal(18,2)");
            entity.Ignore(e => e.MonthName);
        });

        builder.Entity<PayrollRunLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BasicSalary).HasColumnType("decimal(18,2)");
            entity.Property(e => e.HouseAllowance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TransportAllowance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MedicalAllowance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OtherAllowances).HasColumnType("decimal(18,2)");
            entity.Property(e => e.GrossSalary).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PAYE).HasColumnType("decimal(18,2)");
            entity.Property(e => e.NSSFEmployee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.NSSFEmployer).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SHAEmployee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SHAEmployer).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SACCODeduction).HasColumnType("decimal(18,2)");
            entity.Property(e => e.LoanDeduction).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OtherDeductions).HasColumnType("decimal(18,2)");
            entity.Ignore(e => e.TotalDeductions);
            entity.Ignore(e => e.NetPay);
            entity.HasOne(e => e.PayrollRun)
                  .WithMany(r => r.Lines)
                  .HasForeignKey(e => e.PayrollRunId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.StaffPayroll)
                  .WithMany()
                  .HasForeignKey(e => e.StaffPayrollId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<StudentClassHistory>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasOne(e => e.Student)
          .WithMany()
          .HasForeignKey(e => e.StudentId)
          .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.AcademicYear)
          .WithMany()
          .HasForeignKey(e => e.AcademicYearId)
          .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.ClassLevel)
          .WithMany()
          .HasForeignKey(e => e.ClassLevelId)
          .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(e => e.ClassStream)
          .WithMany()
          .HasForeignKey(e => e.ClassStreamId)
          .OnDelete(DeleteBehavior.Restrict);
});
    
    // ── Parent Portal configurations ─────────────────────────────
builder.Entity<SMIS.Domain.Entities.Students.ParentStudentLink>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.ParentUserId).IsRequired().HasMaxLength(450); // matches IdentityUser.Id length
    entity.Property(e => e.Relationship).HasMaxLength(50);
    entity.HasIndex(e => new { e.ParentUserId, e.StudentId }).IsUnique();
    entity.HasOne(e => e.Student)
          .WithMany()
          .HasForeignKey(e => e.StudentId)
          .OnDelete(DeleteBehavior.Cascade);
});

builder.Entity<SMIS.Domain.Entities.Communication.StudentFeedback>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.SenderUserId).IsRequired().HasMaxLength(450);
    entity.Property(e => e.Message).IsRequired();
    entity.HasOne(e => e.Student)
          .WithMany()
          .HasForeignKey(e => e.StudentId)
          .OnDelete(DeleteBehavior.Cascade);
});

builder.Entity<SMIS.Domain.Entities.Communication.Newsletter>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
    entity.Property(e => e.Content).IsRequired();
    entity.Property(e => e.PublishedBy).HasMaxLength(256);
});
}
}