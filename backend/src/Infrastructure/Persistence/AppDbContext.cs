using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// One constructor on purpose: EF refuses to choose between two that both take options. The
    /// caller is always present as a service and is simply empty when no request is behind the work
    /// — the seeder and the backfill command run that way.
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// Stamps who made each new document, in one place rather than in every service.
    /// <para>
    /// Doing it here is not a shortcut — it is the only way the guarantee holds. Threaded through
    /// each service by hand, the next document type somebody adds would simply not be stamped, and
    /// nobody would notice until the question was asked about a month that had already passed.
    /// </para>
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is { } userId)
        {
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                if (entry.State == EntityState.Added && entry.Entity.CreatedByUserId is null)
                {
                    entry.Entity.CreatedByUserId = userId;
                    entry.Entity.CreatedByName = _currentUser.Name;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<Purchase> Purchases => Set<Purchase>();

    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<ShopSettings> ShopSettings => Set<ShopSettings>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();

    public DbSet<ChequeDetail> ChequeDetails => Set<ChequeDetail>();

    public DbSet<PartyLedgerEntry> PartyLedgerEntries => Set<PartyLedgerEntry>();

    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<DayClose> DayCloses => Set<DayClose>();

    public DbSet<User> Users => Set<User>();

    public DbSet<MoneyMovement> MoneyMovements => Set<MoneyMovement>();

    public DbSet<DocumentCounter> DocumentCounters => Set<DocumentCounter>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();

    public DbSet<CreditNoteItem> CreditNoteItems => Set<CreditNoteItem>();

    public DbSet<DebitNote> DebitNotes => Set<DebitNote>();

    public DbSet<DebitNoteItem> DebitNoteItems => Set<DebitNoteItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
