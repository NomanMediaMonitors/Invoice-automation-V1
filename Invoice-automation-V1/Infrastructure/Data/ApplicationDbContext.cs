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
    public DbSet<Company> Companies { get; set; }
    public DbSet<UserCompany> UserCompanies { get; set; }
    public DbSet<ChartOfAccount> ChartOfAccounts { get; set; }

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

        // Company Configuration
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("CHAR(36)");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            entity.Property(e => e.Ntn)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("ntn");

            entity.Property(e => e.Strn)
                .HasMaxLength(50)
                .HasColumnName("strn");

            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");

            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");

            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");

            entity.Property(e => e.IndraajAccessToken)
                .HasMaxLength(500)
                .HasColumnName("indraaj_access_token");

            entity.Property(e => e.LastCoaSyncAt)
                .HasColumnName("last_coa_sync_at")
                .HasColumnType("DATETIME");

            entity.Property(e => e.IsDefault)
                .HasColumnName("is_default")
                .HasColumnType("TINYINT(1)")
                .HasDefaultValue(false);

            entity.Property(e => e.DisplayOrder)
                .HasColumnName("display_order")
                .HasDefaultValue(0);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("DATETIME")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("DATETIME")
                .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Ntn).IsUnique();
        });

        // UserCompany Configuration
        modelBuilder.Entity<UserCompany>(entity =>
        {
            entity.ToTable("user_companies");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("CHAR(36)");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("CHAR(36)");

            entity.Property(e => e.CompanyId)
                .HasColumnName("company_id")
                .HasColumnType("CHAR(36)");

            entity.Property(e => e.Role)
                .IsRequired()
                .HasColumnName("role")
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.IsUserDefault)
                .HasColumnName("is_user_default")
                .HasColumnType("TINYINT(1)")
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("DATETIME")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("DATETIME")
                .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.CompanyId }).IsUnique();
        });

        // ChartOfAccount Configuration
        modelBuilder.Entity<ChartOfAccount>(entity =>
        {
            entity.ToTable("chart_of_accounts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("CHAR(36)");

            entity.Property(e => e.CompanyId)
                .HasColumnName("company_id")
                .HasColumnType("CHAR(36)");

            entity.Property(e => e.Recno)
                .HasColumnName("recno");

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("code");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");

            entity.Property(e => e.AccountType)
                .HasMaxLength(100)
                .HasColumnName("account_type");

            entity.Property(e => e.ParentCode)
                .HasMaxLength(50)
                .HasColumnName("parent_code");

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasColumnType("TINYINT(1)")
                .HasDefaultValue(true);

            entity.Property(e => e.SyncedAt)
                .HasColumnName("synced_at")
                .HasColumnType("DATETIME")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("DATETIME")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("DATETIME")
                .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.CompanyId, e.Recno }).IsUnique();
            entity.HasIndex(e => new { e.CompanyId, e.Code });
        });
    }
}
