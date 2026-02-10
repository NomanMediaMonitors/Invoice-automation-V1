using InvoiceAutomation.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserToken> UserTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("CHAR(36)");

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("email");

            entity.Property(e => e.NormalizedEmail)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("normalized_email");

            entity.Property(e => e.EmailConfirmed)
                .HasColumnName("email_confirmed")
                .HasColumnType("TINYINT(1)");

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("password_hash");

            entity.Property(e => e.SecurityStamp)
                .HasMaxLength(255)
                .HasColumnName("security_stamp");

            entity.Property(e => e.ConcurrencyStamp)
                .HasMaxLength(255)
                .HasColumnName("concurrency_stamp");

            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("full_name");

            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");

            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasColumnName("avatar_url");

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasColumnType("TINYINT(1)")
                .HasDefaultValue(true);

            entity.Property(e => e.IsSuperAdmin)
                .HasColumnName("is_super_admin")
                .HasColumnType("TINYINT(1)")
                .HasDefaultValue(false);

            entity.Property(e => e.LockoutEnd)
                .HasColumnName("lockout_end")
                .HasColumnType("DATETIME");

            entity.Property(e => e.LockoutEnabled)
                .HasColumnName("lockout_enabled")
                .HasColumnType("TINYINT(1)")
                .HasDefaultValue(true);

            entity.Property(e => e.AccessFailedCount)
                .HasColumnName("access_failed_count")
                .HasDefaultValue(0);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("DATETIME")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("DATETIME")
                .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

            entity.Property(e => e.LastLoginAt)
                .HasColumnName("last_login_at")
                .HasColumnType("DATETIME");

            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.NormalizedEmail);
        });

        // UserToken Configuration
        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.ToTable("user_tokens");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("CHAR(36)");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("CHAR(36)");

            entity.Property(e => e.TokenType)
                .IsRequired()
                .HasColumnName("token_type")
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.TokenHash)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("token_hash");

            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at")
                .HasColumnType("DATETIME");

            entity.Property(e => e.UsedAt)
                .HasColumnName("used_at")
                .HasColumnType("DATETIME");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("DATETIME")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TokenHash, e.TokenType });
        });
    }
}
