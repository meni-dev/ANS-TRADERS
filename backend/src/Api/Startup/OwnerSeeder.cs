using Application.Common;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Startup;

/// <summary>
/// Creates the first account so a fresh shop can get in.
/// <para>
/// Run by hand — <c>dotnet run -- --create-owner</c> — and never on startup. The password is
/// generated, not fixed, and printed once to whoever is deploying. A well-known default would still
/// be sitting there in six months, and this is the account that can cancel documents, build roles
/// and unlock the books.
/// </para>
/// <para>
/// It goes to standard output rather than the logger on purpose: a log line lands in CloudWatch and
/// stays there for as long as retention says, which is not where a password belongs.
/// </para>
/// </summary>
public static class OwnerSeeder
{
    public static async Task<bool> CreateAsync(AppDbContext context)
    {
        if ((await context.Database.GetPendingMigrationsAsync()).Any())
        {
            Console.Error.WriteLine("There are migrations still to apply. Run --migrate first.");
            return false;
        }

        if (await context.Users.AnyAsync())
        {
            Console.Error.WriteLine(
                "An account already exists. Reset a password from Settings > People instead.");
            return false;
        }

        var owner = await context.Roles.FirstOrDefaultAsync(r => r.IsSystem);

        if (owner is null)
        {
            Console.Error.WriteLine(
                "The built-in role is missing, so there is nothing to attach an account to. "
                + "That role is created by a migration — check that --migrate ran against this database.");
            return false;
        }

        var password = PasswordHasher.GenerateTemporary();

        context.Users.Add(new User
        {
            Name = "Owner",
            Username = "owner",
            PasswordHash = PasswordHasher.Hash(password),
            RoleId = owner.Id,
            MustChangePassword = true,
        });

        await context.SaveChangesAsync();

        Console.WriteLine();
        Console.WriteLine("  Account created.");
        Console.WriteLine("    username  owner");
        Console.WriteLine($"    password  {password}");
        Console.WriteLine();
        Console.WriteLine("  Shown once and not stored anywhere readable. Sign in and change it —");
        Console.WriteLine("  the app will ask you to before it lets you do anything else.");
        Console.WriteLine();

        return true;
    }
}
