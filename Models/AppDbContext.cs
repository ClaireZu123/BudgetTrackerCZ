using Microsoft.EntityFrameworkCore;
namespace BudgetTrackerCZ.Models
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<FinancialTarget> FinancialTargets { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<TaxWarning> TaxWarnings { get; set; }
        public DbSet<Notification> Notifications { get; set; }


    }
}
