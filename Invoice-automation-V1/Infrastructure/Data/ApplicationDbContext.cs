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
    public DbSet<Vendor> Vendors { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; }
    public DbSet<VendorInvoiceTemplate> VendorInvoiceTemplates { get; set; }

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

            entity.Property(e => e.City)
                .HasColumnName("City")
                .HasColumnType("LONGTEXT");

            entity.Property(e => e.Website)
                .HasColumnName("Website")
                .HasColumnType("LONGTEXT");

            entity.Property(e => e.LogoUrl)
                .HasColumnName("LogoUrl")
                .HasColumnType("LONGTEXT");

            entity.Property(e => e.FiscalYearStart)
                .HasColumnName("FiscalYearStart")
                .HasColumnType("DATETIME(6)");

            entity.Property(e => e.DefaultCurrency)
                .HasColumnName("DefaultCurrency")
                .HasColumnType("LONGTEXT");

            entity.Property(e => e.IndraajAccessToken)
                .HasMaxLength(500)
                .HasColumnName("indraaj_access_token");

            entity.Property(e => e.IndraajConnectedAt)
                .HasColumnName("IndraajConnectedAt")
                .HasColumnType("DATETIME(6)");

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

            entity.Property(e => e.IsActive)
                .HasColumnName("IsActive")
                .HasColumnType("TINYINT(1)")
                .HasDefaultValue(true);

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

        // Vendor Configuration
        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.ToTable("vendors");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("CHAR(36)");
            entity.Property(e => e.CompanyId).HasColumnName("company_id").HasColumnType("CHAR(36)");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255).HasColumnName("email");
            entity.Property(e => e.Phone).HasMaxLength(20).HasColumnName("phone");
            entity.Property(e => e.Type).IsRequired().HasColumnName("type").HasConversion<string>();
            entity.Property(e => e.Ntn).HasMaxLength(50).HasColumnName("ntn");
            entity.Property(e => e.City).HasMaxLength(100).HasColumnName("city");
            entity.Property(e => e.Address).HasMaxLength(500).HasColumnName("address");
            entity.Property(e => e.BankName).HasMaxLength(200).HasColumnName("bank_name");
            entity.Property(e => e.PaymentTermDays).HasColumnName("payment_term_days").HasDefaultValue(30);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasColumnType("TINYINT(1)").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME").HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasColumnType("CHAR(36)");

            entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.CompanyId, e.Email }).IsUnique();
        });

        // Invoice Configuration
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("invoices");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("CHAR(36)");
            entity.Property(e => e.CompanyId).HasColumnName("company_id").HasColumnType("CHAR(36)");
            entity.Property(e => e.VendorId).HasColumnName("vendor_id").HasColumnType("CHAR(36)");
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(100).HasColumnName("invoice_number");
            entity.Property(e => e.InvoiceDate).HasColumnName("invoice_date").HasColumnType("DATE");
            entity.Property(e => e.DueDate).HasColumnName("due_date").HasColumnType("DATE");
            entity.Property(e => e.SubTotal).HasColumnName("sub_total").HasColumnType("DECIMAL(18,2)");
            entity.Property(e => e.TaxAmount).HasColumnName("tax_amount").HasColumnType("DECIMAL(18,2)");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount").HasColumnType("DECIMAL(18,2)");
            entity.Property(e => e.Currency).HasMaxLength(10).HasColumnName("currency").HasDefaultValue("PKR");
            entity.Property(e => e.Status).IsRequired().HasColumnName("status").HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000).HasColumnName("description");
            entity.Property(e => e.Notes).HasColumnName("notes").HasColumnType("TEXT");

            // OCR Fields
            entity.Property(e => e.OriginalFileName).HasMaxLength(255).HasColumnName("original_file_name");
            entity.Property(e => e.FileStoragePath).HasMaxLength(500).HasColumnName("file_storage_path");
            entity.Property(e => e.FileUrl).HasMaxLength(500).HasColumnName("file_url");
            entity.Property(e => e.FileType).HasMaxLength(50).HasColumnName("file_type");
            entity.Property(e => e.FileSize).HasColumnName("file_size");
            entity.Property(e => e.IsOcrProcessed).HasColumnName("is_ocr_processed").HasColumnType("TINYINT(1)").HasDefaultValue(false);
            entity.Property(e => e.OcrProcessedAt).HasColumnName("ocr_processed_at").HasColumnType("DATETIME");
            entity.Property(e => e.OcrConfidenceScore).HasColumnName("ocr_confidence_score").HasColumnType("DECIMAL(5,2)");
            entity.Property(e => e.OcrRawData).HasColumnName("ocr_raw_data").HasColumnType("LONGTEXT");
            entity.Property(e => e.OcrErrorMessage).HasMaxLength(1000).HasColumnName("ocr_error_message");

            // Approval Fields
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by").HasColumnType("CHAR(36)");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at").HasColumnType("DATETIME");
            entity.Property(e => e.ApprovalNotes).HasMaxLength(1000).HasColumnName("approval_notes");

            // Payment Fields
            entity.Property(e => e.PaidAt).HasColumnName("paid_at").HasColumnType("DATETIME");
            entity.Property(e => e.PaymentReference).HasMaxLength(100).HasColumnName("payment_reference");
            entity.Property(e => e.PaidBy).HasColumnName("paid_by").HasColumnType("CHAR(36)");

            // GL Account Fields
            entity.Property(e => e.AdvanceTaxAccountId).HasColumnName("advance_tax_account_id").HasColumnType("CHAR(36)");
            entity.Property(e => e.SalesTaxInputAccountId).HasColumnName("sales_tax_input_account_id").HasColumnType("CHAR(36)");
            entity.Property(e => e.PayableVendorsAccountId).HasColumnName("payable_vendors_account_id").HasColumnType("CHAR(36)");

            // GL Posting Fields
            entity.Property(e => e.IsPostedToGL).HasColumnName("is_posted_to_gl").HasColumnType("TINYINT(1)").HasDefaultValue(false);
            entity.Property(e => e.PostedToGLAt).HasColumnName("posted_to_gl_at").HasColumnType("DATETIME");
            entity.Property(e => e.PostedToGLBy).HasColumnName("posted_to_gl_by").HasColumnType("CHAR(36)");

            // Indraaj Fields
            entity.Property(e => e.IndraajVoucherNo).HasMaxLength(100).HasColumnName("indraaj_voucher_no");
            entity.Property(e => e.SyncedToIndraajAt).HasColumnName("synced_to_indraaj_at").HasColumnType("DATETIME");

            // Audit Fields
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasColumnType("CHAR(36)");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME").HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasColumnType("CHAR(36)");

            // Relationships
            entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Vendor).WithMany().HasForeignKey(e => e.VendorId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AdvanceTaxAccount).WithMany().HasForeignKey(e => e.AdvanceTaxAccountId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.SalesTaxInputAccount).WithMany().HasForeignKey(e => e.SalesTaxInputAccountId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.PayableVendorsAccount).WithMany().HasForeignKey(e => e.PayableVendorsAccountId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);

            // Indexes
            entity.HasIndex(e => new { e.CompanyId, e.InvoiceNumber }).IsUnique();
            entity.HasIndex(e => e.VendorId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.InvoiceDate);
            entity.HasIndex(e => e.DueDate);
        });

        // InvoiceLineItem Configuration
        modelBuilder.Entity<InvoiceLineItem>(entity =>
        {
            entity.ToTable("invoice_line_items");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("CHAR(36)");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id").HasColumnType("CHAR(36)");
            entity.Property(e => e.LineNumber).HasColumnName("line_number");
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500).HasColumnName("description");
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasColumnType("DECIMAL(18,4)");
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price").HasColumnType("DECIMAL(18,2)");
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("DECIMAL(18,2)");
            entity.Property(e => e.TaxRate).HasColumnName("tax_rate").HasColumnType("DECIMAL(5,2)");
            entity.Property(e => e.TaxAmount).HasColumnName("tax_amount").HasColumnType("DECIMAL(18,2)");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount").HasColumnType("DECIMAL(18,2)");
            entity.Property(e => e.ChartOfAccountId).HasColumnName("chart_of_account_id").HasColumnType("CHAR(36)");
            entity.Property(e => e.AccountCode).HasMaxLength(50).HasColumnName("account_code");
            entity.Property(e => e.IsOcrExtracted).HasColumnName("is_ocr_extracted").HasColumnType("TINYINT(1)").HasDefaultValue(false);
            entity.Property(e => e.OcrConfidenceScore).HasColumnName("ocr_confidence_score").HasColumnType("DECIMAL(5,2)");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME").HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

            // Relationships
            entity.HasOne(e => e.Invoice).WithMany(i => i.LineItems).HasForeignKey(e => e.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ChartOfAccount).WithMany().HasForeignKey(e => e.ChartOfAccountId).OnDelete(DeleteBehavior.SetNull);

            // Indexes
            entity.HasIndex(e => new { e.InvoiceId, e.LineNumber }).IsUnique();
        });

        // VendorInvoiceTemplate Configuration
        modelBuilder.Entity<VendorInvoiceTemplate>(entity =>
        {
            entity.ToTable("vendor_invoice_templates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("CHAR(36)");
            entity.Property(e => e.VendorId).HasColumnName("vendor_id").HasColumnType("CHAR(36)");
            entity.Property(e => e.TemplateName).IsRequired().HasMaxLength(200).HasColumnName("template_name");

            // Field configuration
            entity.Property(e => e.HasInvoiceNumber).HasColumnName("has_invoice_number").HasColumnType("TINYINT(1)").HasDefaultValue(true);
            entity.Property(e => e.HasInvoiceDate).HasColumnName("has_invoice_date").HasColumnType("TINYINT(1)").HasDefaultValue(true);
            entity.Property(e => e.HasDueDate).HasColumnName("has_due_date").HasColumnType("TINYINT(1)").HasDefaultValue(true);
            entity.Property(e => e.HasDescription).HasColumnName("has_description").HasColumnType("TINYINT(1)").HasDefaultValue(true);
            entity.Property(e => e.HasLineItems).HasColumnName("has_line_items").HasColumnType("TINYINT(1)").HasDefaultValue(true);
            entity.Property(e => e.HasTaxRate).HasColumnName("has_tax_rate").HasColumnType("TINYINT(1)").HasDefaultValue(true);
            entity.Property(e => e.HasSubTotal).HasColumnName("has_sub_total").HasColumnType("TINYINT(1)").HasDefaultValue(true);

            // OCR field mapping labels
            entity.Property(e => e.InvoiceNumberLabel).HasMaxLength(100).HasColumnName("invoice_number_label");
            entity.Property(e => e.InvoiceDateLabel).HasMaxLength(100).HasColumnName("invoice_date_label");
            entity.Property(e => e.DueDateLabel).HasMaxLength(100).HasColumnName("due_date_label");
            entity.Property(e => e.SubTotalLabel).HasMaxLength(100).HasColumnName("sub_total_label");
            entity.Property(e => e.TaxLabel).HasMaxLength(100).HasColumnName("tax_label");
            entity.Property(e => e.TotalLabel).HasMaxLength(100).HasColumnName("total_label");

            // GL Account configuration
            entity.Property(e => e.HasAdvanceTaxAccount).HasColumnName("has_advance_tax_account").HasColumnType("TINYINT(1)").HasDefaultValue(true);
            entity.Property(e => e.HasSalesTaxInputAccount).HasColumnName("has_sales_tax_input_account").HasColumnType("TINYINT(1)").HasDefaultValue(true);
            entity.Property(e => e.HasPayableVendorsAccount).HasColumnName("has_payable_vendors_account").HasColumnType("TINYINT(1)").HasDefaultValue(true);

            // Default GL accounts
            entity.Property(e => e.DefaultAdvanceTaxAccountId).HasColumnName("default_advance_tax_account_id").HasColumnType("CHAR(36)");
            entity.Property(e => e.DefaultSalesTaxInputAccountId).HasColumnName("default_sales_tax_input_account_id").HasColumnType("CHAR(36)");
            entity.Property(e => e.DefaultPayableVendorsAccountId).HasColumnName("default_payable_vendors_account_id").HasColumnType("CHAR(36)");

            // Defaults
            entity.Property(e => e.DefaultTaxRate).HasColumnName("default_tax_rate").HasColumnType("DECIMAL(5,2)");
            entity.Property(e => e.DefaultChartOfAccountId).HasColumnName("default_chart_of_account_id").HasColumnType("CHAR(36)");

            entity.Property(e => e.Notes).HasMaxLength(1000).HasColumnName("notes");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasColumnType("TINYINT(1)").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME").HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

            // Relationships
            entity.HasOne(e => e.Vendor).WithMany().HasForeignKey(e => e.VendorId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.DefaultChartOfAccount).WithMany().HasForeignKey(e => e.DefaultChartOfAccountId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.DefaultAdvanceTaxAccount).WithMany().HasForeignKey(e => e.DefaultAdvanceTaxAccountId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.DefaultSalesTaxInputAccount).WithMany().HasForeignKey(e => e.DefaultSalesTaxInputAccountId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.DefaultPayableVendorsAccount).WithMany().HasForeignKey(e => e.DefaultPayableVendorsAccountId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);

            // Indexes
            entity.HasIndex(e => e.VendorId);
        });
    }
}
