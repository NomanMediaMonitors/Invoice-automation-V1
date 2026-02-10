namespace InvoiceAutomation.Core.Entities;

public class ChartOfAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }

    // Indraaj Data
    public int Recno { get; set; }  // Indraaj primary key
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AccountType { get; set; }  // Asset, Liability, Expense, Revenue, Equity
    public string? ParentCode { get; set; }
    public bool IsActive { get; set; } = true;

    // Sync tracking
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Company Company { get; set; } = null!;

    // Helper property
    public string DisplayName => $"{Code} - {Name}";
}
