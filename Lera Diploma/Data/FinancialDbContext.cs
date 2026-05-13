using System.Data.Entity;
using Lera_Diploma.Models;

namespace Lera_Diploma.Data
{
    public class FinancialDbContext : DbContext
    {
        public FinancialDbContext() : base("name=FinanceContext")
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Counterparty> Counterparties { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<DocumentStatus> DocumentStatuses { get; set; }
        public DbSet<BudgetItem> BudgetItems { get; set; }
        public DbSet<FinancialDocument> FinancialDocuments { get; set; }
        public DbSet<AccountingEntry> AccountingEntries { get; set; }
        public DbSet<DocumentBudgetAllocation> DocumentBudgetAllocations { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasRequired(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<UserRole>()
                .HasRequired(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Account>()
                .HasOptional(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentAccountId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<FinancialDocument>()
                .HasRequired(x => x.ResponsibleUser)
                .WithMany()
                .HasForeignKey(x => x.ResponsibleUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<FinancialDocument>()
                .HasOptional(x => x.Counterparty)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.CounterpartyId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<FinancialDocument>()
                .HasRequired(x => x.DocumentType)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.DocumentTypeId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<FinancialDocument>()
                .HasRequired(x => x.DocumentStatus)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.DocumentStatusId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<AccountingEntry>()
                .HasRequired(x => x.FinancialDocument)
                .WithMany(x => x.Entries)
                .HasForeignKey(x => x.FinancialDocumentId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<AccountingEntry>()
                .HasRequired(x => x.DebitAccount)
                .WithMany(x => x.DebitEntries)
                .HasForeignKey(x => x.DebitAccountId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<AccountingEntry>()
                .HasRequired(x => x.CreditAccount)
                .WithMany(x => x.CreditEntries)
                .HasForeignKey(x => x.CreditAccountId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DocumentBudgetAllocation>()
                .HasRequired(x => x.FinancialDocument)
                .WithMany(x => x.BudgetAllocations)
                .HasForeignKey(x => x.FinancialDocumentId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<DocumentBudgetAllocation>()
                .HasRequired(x => x.BudgetItem)
                .WithMany(x => x.Allocations)
                .HasForeignKey(x => x.BudgetItemId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<AuditLog>()
                .HasOptional(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<AccountingEntry>().Property(x => x.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<DocumentBudgetAllocation>().Property(x => x.Amount).HasPrecision(18, 2);
        }
    }
}
