namespace InvoiceAutomation.Core.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Authentication
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string? SecurityStamp { get; set; }
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    // Profile
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public bool IsSuperAdmin { get; set; }  // First user = true
    public DateTime? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; } = true;
    public int AccessFailedCount { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation (will be added in Module 2)
    // public virtual ICollection<UserCompany> UserCompanies { get; set; }
}
