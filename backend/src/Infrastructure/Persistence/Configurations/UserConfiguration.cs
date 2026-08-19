using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).IsRequired().HasMaxLength(120);

        // Stored lower-cased by the service, so a plain unique index is enough to stop "Ravi" and
        // "ravi" being two accounts.
        builder.Property(u => u.Username).IsRequired().HasMaxLength(60);
        builder.HasIndex(u => u.Username).IsUnique();

        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(200);

        // Restrict, not cascade: deleting a role must never quietly delete the people in it. The
        // service moves them first and refuses otherwise.
        builder.HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.RoleId);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(60);
        builder.Property(r => r.Description).HasMaxLength(200);

        builder.HasIndex(r => r.Name).IsUnique();

        builder.HasMany(r => r.Permissions)
            .WithOne(p => p.Role)
            .HasForeignKey(p => p.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(p => p.Id);

        // By name, not by number. Inserting a member into the enum later must not silently hand a
        // role a permission somebody never ticked.
        builder.Property(p => p.Permission).HasConversion<string>().HasMaxLength(40).IsRequired();

        // The database refuses the same permission twice on one role, so a double-click cannot
        // leave two rows that then have to be reconciled.
        builder.HasIndex(p => new { p.RoleId, p.Permission }).IsUnique();
    }
}

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");
        builder.HasKey(s => s.Id);

        // Looked up on every single request, so it earns a unique index rather than a scan.
        builder.Property(s => s.Token).IsRequired().HasMaxLength(64);
        builder.HasIndex(s => s.Token).IsUnique();

        // Cascade here, unlike everywhere else: a session is not history. When an account goes, its
        // open sessions should go with it rather than dangle.
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.UserId);
    }
}

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserName).IsRequired().HasMaxLength(120);
        builder.Property(e => e.Action).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.EntityLabel).HasMaxLength(120);
        builder.Property(e => e.Detail).HasMaxLength(1000);

        // No foreign key to users on purpose: the log outlives the account, and a cascade or a
        // restrict would either erase history or stop somebody ever being removed.
        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => e.Action);
    }
}
