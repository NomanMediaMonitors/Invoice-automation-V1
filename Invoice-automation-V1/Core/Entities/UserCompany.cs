namespace InvoiceAutomation.Core.Entities;

public enum UserRole
{
    SuperAdmin,
    Admin,
    Manager,
    Accountant,
    Approver,
    Viewer
}

public class UserCompany
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }

    // Role in this company
    public UserRole Role { get; set; }

    // User's default preference
    public bool IsUserDefault { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public Guid? InvitedBy { get; set; }
    public DateTime? InvitedAt { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Company Company { get; set; } = null!;
}
