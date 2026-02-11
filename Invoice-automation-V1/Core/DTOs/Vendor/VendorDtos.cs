using System.ComponentModel.DataAnnotations;
using InvoiceAutomation.Core.Entities;

namespace InvoiceAutomation.Core.DTOs.Vendor;

public class CreateVendorDto
{
    [Required(ErrorMessage = "Vendor name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(255)]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20)]
    public string? Phone { get; set; }

    public string? MobilePhone { get; set; }

    [Url(ErrorMessage = "Invalid website URL")]
    public string? Website { get; set; }

    [Required(ErrorMessage = "Vendor type is required")]
    public VendorType Type { get; set; }

    [Required(ErrorMessage = "NTN is required")]
    [StringLength(50)]
    public string Ntn { get; set; } = string.Empty;

    public string? Strn { get; set; }

    public string? RegistrationNumber { get; set; }

    [Required(ErrorMessage = "Address is required")]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "State is required")]
    public string State { get; set; } = string.Empty;

    public string? PostalCode { get; set; }

    [Required(ErrorMessage = "Country is required")]
    public string Country { get; set; } = "Pakistan";

    [Required(ErrorMessage = "Contact person name is required")]
    public string ContactPersonName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid contact person email")]
    public string? ContactPersonEmail { get; set; }

    [Required(ErrorMessage = "Contact person phone is required")]
    public string ContactPersonPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bank name is required")]
    [StringLength(200)]
    public string BankName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bank account number is required")]
    public string BankAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bank account title is required")]
    public string BankAccountTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "IBAN is required")]
    public string Iban { get; set; } = string.Empty;

    public string? SwiftCode { get; set; }

    public int? PaymentTermDays { get; set; }

    public string? PaymentTermsNotes { get; set; }

    public string? Notes { get; set; }

    public Guid CompanyId { get; set; }
}

public class UpdateVendorDto
{
    [Required(ErrorMessage = "Vendor name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(255)]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20)]
    public string? Phone { get; set; }

    public string? MobilePhone { get; set; }

    [Url(ErrorMessage = "Invalid website URL")]
    public string? Website { get; set; }

    [Required(ErrorMessage = "Vendor type is required")]
    public VendorType Type { get; set; }

    [Required(ErrorMessage = "NTN is required")]
    [StringLength(50)]
    public string Ntn { get; set; } = string.Empty;

    public string? Strn { get; set; }

    public string? RegistrationNumber { get; set; }

    [Required(ErrorMessage = "Address is required")]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "State is required")]
    public string State { get; set; } = string.Empty;

    public string? PostalCode { get; set; }

    [Required(ErrorMessage = "Country is required")]
    public string Country { get; set; } = "Pakistan";

    [Required(ErrorMessage = "Contact person name is required")]
    public string ContactPersonName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid contact person email")]
    public string? ContactPersonEmail { get; set; }

    [Required(ErrorMessage = "Contact person phone is required")]
    public string ContactPersonPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bank name is required")]
    [StringLength(200)]
    public string BankName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bank account number is required")]
    public string BankAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bank account title is required")]
    public string BankAccountTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "IBAN is required")]
    public string Iban { get; set; } = string.Empty;

    public string? SwiftCode { get; set; }

    public int? PaymentTermDays { get; set; }

    public string? PaymentTermsNotes { get; set; }

    public string? Notes { get; set; }
}

public class VendorListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public VendorType Type { get; set; }
    public string City { get; set; } = string.Empty;
    public string Ntn { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int? PaymentTermDays { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VendorDetailsDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? Website { get; set; }
    public VendorType Type { get; set; }
    public string Ntn { get; set; } = string.Empty;
    public string? Strn { get; set; }
    public string? RegistrationNumber { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string Country { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;
    public string? ContactPersonEmail { get; set; }
    public string ContactPersonPhone { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankAccountTitle { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string? SwiftCode { get; set; }
    public int? PaymentTermDays { get; set; }
    public string? PaymentTermsNotes { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}
