using Application.Interfaces;
using Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditLog, AuditLog>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPeriodLock, PeriodLock>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IStockInsightService, StockInsightService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductImportService, ProductImportService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<ICashService, CashService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IStockLedger, StockLedger>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IShopSettingsService, ShopSettingsService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPartyAccountService, PartyAccountService>();
        services.AddScoped<IChequeService, ChequeService>();
        services.AddScoped<IPaymentLedger, PaymentLedger>();
        services.AddScoped<IPartyLedger, PartyLedger>();
        services.AddScoped<ICreditNoteService, CreditNoteService>();
        services.AddScoped<IDebitNoteService, DebitNoteService>();

        return services;
    }
}
