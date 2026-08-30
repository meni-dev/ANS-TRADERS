using Application.Common;
using Application.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        // Singleton: the zone is looked up once and does not change while the app is running.
        services.AddSingleton<IShopClock>(provider =>
        {
            var id = configuration["Shop:TimeZone"];

            if (!ShopClock.TryResolve(id, out var zone))
            {
                provider.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("ShopClock")
                    .LogError(
                        "Shop:TimeZone '{TimeZone}' was not recognised, so dates will be UTC. "
                        + "Set it to something like {Default}.", id, ShopClock.DefaultTimeZone);
            }

            return new ShopClock(zone);
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IDocumentNumbers, DocumentNumbers>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IShopSettingsRepository, ShopSettingsRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPartyLedgerRepository, PartyLedgerRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ICashRepository, CashRepository>();
        services.AddScoped<IMoneyMovementRepository, MoneyMovementRepository>();
        services.AddScoped<ICreditNoteRepository, CreditNoteRepository>();
        services.AddScoped<IDebitNoteRepository, DebitNoteRepository>();
        services.AddScoped<Backfill.PaymentsBackfill>();

        return services;
    }
}
