namespace InvoiceAutomation.Core.Entities;

public enum VendorType
{
    Supplier,
    ServiceProvider,
    Contractor,
    Consultant,
    Other
}

public class Vendor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }

    // Basic Information
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? Website { get; set; }

    // Business Information
    public VendorType Type { get; set; } = VendorType.Supplier;
    public string? Ntn { get; set; }  // National Tax Number
    public string? Strn { get; set; } // Sales Tax Registration Number
    public string? RegistrationNumber { get; set; }

    // Address Information
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    // Contact Person
    public string? ContactPersonName { get; set; }
    public string? ContactPersonEmail { get; set; }
    public string? ContactPersonPhone { get; set; }

    // Banking Information
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountTitle { get; set; }
    public string? Iban { get; set; }
    public string? SwiftCode { get; set; }

    // Payment Terms
    public int PaymentTermDays { get; set; } = 30; // Default 30 days
    public string? PaymentTermsNotes { get; set; }

    // Additional Information
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }

    // Navigation Properties
    public virtual Company Company { get; set; } = null!;
}
