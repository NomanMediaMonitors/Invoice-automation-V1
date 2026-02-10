namespace InvoiceAutomation.Core.Entities;

public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Basic Info
    public string Name { get; set; } = string.Empty;
    public string Ntn { get; set; } = string.Empty;
    public string? Strn { get; set; }

    // Contact
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }

    // Settings
    public DateTime FiscalYearStart { get; set; }
    public string DefaultCurrency { get; set; } = "PKR";

    // Indraaj Integration
    public string? IndraajAccessToken { get; set; }  // Encrypted
    public DateTime? IndraajConnectedAt { get; set; }
    public DateTime? LastCoaSyncAt { get; set; }  // Track last sync time

    // Default Company Logic
    public bool IsDefault { get; set; }
    public int DisplayOrder { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual ICollection<UserCompany> UserCompanies { get; set; } = new List<UserCompany>();
    public virtual ICollection<ChartOfAccount> ChartOfAccounts { get; set; } = new List<ChartOfAccount>();
}
