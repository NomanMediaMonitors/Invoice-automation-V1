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
    public string Name { get; set; } = string.Empty; // Mandatory
    public string? Email { get; set; } // Non-mandatory
    public string? Phone { get; set; } // Non-mandatory
    public string? MobilePhone { get; set; } // Non-mandatory
    public string? Website { get; set; } // Non-mandatory

    // Business Information
    public VendorType Type { get; set; } = VendorType.Supplier; // Mandatory
    public string Ntn { get; set; } = string.Empty; // Mandatory
    public string? Strn { get; set; } // Non-mandatory
    public string? RegistrationNumber { get; set; } // Non-mandatory

    // Address Information
    public string Address { get; set; } = string.Empty; // Mandatory
    public string City { get; set; } = string.Empty; // Mandatory
    public string State { get; set; } = string.Empty; // Mandatory
    public string? PostalCode { get; set; } // Non-mandatory
    public string Country { get; set; } = string.Empty; // Mandatory

    // Contact Person
    public string ContactPersonName { get; set; } = string.Empty; // Mandatory
    public string? ContactPersonEmail { get; set; } // Non-mandatory
    public string ContactPersonPhone { get; set; } = string.Empty; // Mandatory

    // Banking Information
    public string BankName { get; set; } = string.Empty; // Mandatory
    public string BankAccountNumber { get; set; } = string.Empty; // Mandatory
    public string BankAccountTitle { get; set; } = string.Empty; // Mandatory
    public string Iban { get; set; } = string.Empty; // Mandatory
    public string? SwiftCode { get; set; } // Non-mandatory

    // Payment Terms
    public int? PaymentTermDays { get; set; } // Non-mandatory
    public string? PaymentTermsNotes { get; set; } // Non-mandatory

    // Additional Information
    public string? Notes { get; set; } // Non-mandatory
    public bool IsActive { get; set; } = true; // Mandatory

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }

    // Navigation Properties
    public virtual Company Company { get; set; } = null!;
}
