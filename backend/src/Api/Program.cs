using Api.Features.Auth;
using Api.Features.Cash;
using Api.Features.Customers;
using Api.Features.Dashboard;
using Api.Features.Expenses;
using Api.Features.Payments;
using Api.Features.Products;
using Api.Features.Returns;
using Api.Features.Purchases;
using Api.Features.Reports;
using Api.Features.Sales;
using Api.Features.Settings;
using Api.Features.Stock;
using Api.Features.Suppliers;
using Api.Middleware;
using Api.Startup;
using Amazon.Lambda.AspNetCoreServer.Hosting;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Application;
using Application.Interfaces;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Turns the app into a Lambda handler when it is running in Lambda, and does nothing when it is
// not — so the same build runs under `dotnet run` locally and behind a Function URL in AWS.
//
// HttpApi is the right payload here even though there is no API Gateway: a Function URL sends the
// same v2 request shape. Picking RestApi would leave every route unmatched.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

// Scoped: one per request, filled in by SessionMiddleware. AppDbContext takes it too, so every
// document is stamped with its author without each service having to remember to do it.
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Set in appsettings locally and through Cors__AllowedOrigins__0 in Lambda. Left empty the browser
// blocks every call, which looks exactly like the API being down — so it is called out at startup
// rather than left to be discovered from a console error.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// One-off history rebuild, run with the app stopped: `dotnet run -- --backfill-payments`.
// Deliberately not an endpoint — it rewrites the party ledger wholesale, which is safe exactly once,
// while nobody is billing.
if (args.Contains("--backfill-payments"))
{
    using var scope = app.Services.CreateScope();
    var backfill = scope.ServiceProvider.GetRequiredService<Infrastructure.Backfill.PaymentsBackfill>();

    Infrastructure.Backfill.PaymentsBackfillReport report;

    try
    {
        report = await backfill.RunAsync();
    }
    catch (InvalidOperationException ex)
    {
        // The refusals this command raises are addressed to whoever typed the command. A stack
        // trace buries the one sentence that tells them what to do instead.
        Console.Error.WriteLine(ex.Message);
        return 2;
    }

    foreach (var warning in report.Warnings)
    {
        Console.WriteLine(warning);
    }

    Console.WriteLine($"Payments synthesised : {report.PaymentsSynthesised}");
    Console.WriteLine($"Ledger entries       : {report.LedgerEntriesWritten}");
    Console.WriteLine($"Receivable before    : {report.ReceivableBefore:0.00}");
    Console.WriteLine($"Receivable after     : {report.ReceivableAfter:0.00}");
    Console.WriteLine($"Opening balances     : {report.OpeningBalancesSeeded:0.00}");
    Console.WriteLine(report.Reconciles
        ? "RECONCILED — the receivable moved by exactly the opening balances brought forward."
        : "DOES NOT RECONCILE — do not unfreeze; the difference is not explained by opening balances.");

    return report.Reconciles ? 0 : 1;
}

// Applies pending migrations and stops: `dotnet run -- --migrate`.
//
// A deploy step, never something the app does to itself on the way up. Under Lambda, several cold
// containers start at once and would race each other through the same migration; and a schema
// change that fails should stop a deploy, not take a running shop down with it.
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();

    if (pending.Count == 0)
    {
        Console.WriteLine("Nothing to apply — the database is already up to date.");
        return 0;
    }

    Console.WriteLine($"Applying {pending.Count} migration(s):");
    foreach (var migration in pending)
    {
        Console.WriteLine($"  {migration}");
    }

    await context.Database.MigrateAsync();
    Console.WriteLine("Done.");
    return 0;
}

// Creates the first account: `dotnet run -- --create-owner`.
//
// Run by hand, by whoever is deploying, against a terminal they are looking at. It used to happen
// on startup and print the password to the log — which is fine when the log is a terminal window
// and quite different when it is CloudWatch, where the password would sit for as long as the log
// retention says.
if (args.Contains("--create-owner"))
{
    using var scope = app.Services.CreateScope();

    return await OwnerSeeder.CreateAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>())
        ? 0
        : 1;
}

if (allowedOrigins.Length == 0)
{
    app.Logger.LogWarning(
        "Cors:AllowedOrigins is empty, so a browser on any other domain will be refused. "
        + "Set it to where the UI is served from.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors("Frontend");

// After CORS so a pre-flight is answered, and after error handling so a 401 is shaped like every
// other failure the frontend already knows how to read.
app.UseMiddleware<SessionMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapProductEndpoints();
app.MapCustomerEndpoints();
app.MapSupplierEndpoints();
app.MapPurchaseEndpoints();
app.MapInvoiceEndpoints();
app.MapStockEndpoints();
app.MapPaymentEndpoints();
app.MapChequeEndpoints();
app.MapExpenseEndpoints();
app.MapAuthEndpoints();
app.MapCashEndpoints();
app.MapCreditNoteEndpoints();
app.MapDebitNoteEndpoints();
app.MapSettingsEndpoints();
app.MapReportEndpoints();
app.MapDashboardEndpoints();

app.Run();

// Top-level statements: the backfill branch above returns an exit code, so this one must too.
return 0;
