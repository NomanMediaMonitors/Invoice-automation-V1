using System.ComponentModel.DataAnnotations;
using InvoiceAutomation.Core.Entities;

namespace InvoiceAutomation.Core.DTOs.Vendor;

public class CreateVendorDto
{
    [Required(ErrorMessage = "Vendor name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Phone(ErrorMessage = "Invalid mobile phone number")]
    [StringLength(20)]
    public string? MobilePhone { get; set; }

    [Url(ErrorMessage = "Invalid website URL")]
    [StringLength(500)]
    public string? Website { get; set; }

    [Required(ErrorMessage = "Vendor type is required")]
    public VendorType Type { get; set; }

    [StringLength(50)]
    public string? Ntn { get; set; }

    [StringLength(50)]
    public string? Strn { get; set; }

    [StringLength(100)]
    public string? RegistrationNumber { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(200)]
    public string? ContactPersonName { get; set; }

    [EmailAddress(ErrorMessage = "Invalid contact person email")]
    [StringLength(255)]
    public string? ContactPersonEmail { get; set; }

    [Phone(ErrorMessage = "Invalid contact person phone")]
    [StringLength(20)]
    public string? ContactPersonPhone { get; set; }

    [StringLength(200)]
    public string? BankName { get; set; }

    [StringLength(50)]
    public string? BankAccountNumber { get; set; }

    [StringLength(200)]
    public string? BankAccountTitle { get; set; }

    [StringLength(50)]
    public string? Iban { get; set; }

    [StringLength(20)]
    public string? SwiftCode { get; set; }

    [Range(0, 365, ErrorMessage = "Payment term days must be between 0 and 365")]
    public int PaymentTermDays { get; set; } = 30;

    [StringLength(500)]
    public string? PaymentTermsNotes { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public Guid CompanyId { get; set; }
}

public class UpdateVendorDto
{
    [Required(ErrorMessage = "Vendor name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Phone(ErrorMessage = "Invalid mobile phone number")]
    [StringLength(20)]
    public string? MobilePhone { get; set; }

    [Url(ErrorMessage = "Invalid website URL")]
    [StringLength(500)]
    public string? Website { get; set; }

    [Required(ErrorMessage = "Vendor type is required")]
    public VendorType Type { get; set; }

    [StringLength(50)]
    public string? Ntn { get; set; }

    [StringLength(50)]
    public string? Strn { get; set; }

    [StringLength(100)]
    public string? RegistrationNumber { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(200)]
    public string? ContactPersonName { get; set; }

    [EmailAddress(ErrorMessage = "Invalid contact person email")]
    [StringLength(255)]
    public string? ContactPersonEmail { get; set; }

    [Phone(ErrorMessage = "Invalid contact person phone")]
    [StringLength(20)]
    public string? ContactPersonPhone { get; set; }

    [StringLength(200)]
    public string? BankName { get; set; }

    [StringLength(50)]
    public string? BankAccountNumber { get; set; }

    [StringLength(200)]
    public string? BankAccountTitle { get; set; }

    [StringLength(50)]
    public string? Iban { get; set; }

    [StringLength(20)]
    public string? SwiftCode { get; set; }

    [Range(0, 365, ErrorMessage = "Payment term days must be between 0 and 365")]
    public int PaymentTermDays { get; set; } = 30;

    [StringLength(500)]
    public string? PaymentTermsNotes { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class VendorListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public VendorType Type { get; set; }
    public string? City { get; set; }
    public string? Ntn { get; set; }
    public bool IsActive { get; set; }
    public int PaymentTermDays { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VendorDetailsDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? Website { get; set; }
    public VendorType Type { get; set; }
    public string? Ntn { get; set; }
    public string? Strn { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? ContactPersonName { get; set; }
    public string? ContactPersonEmail { get; set; }
    public string? ContactPersonPhone { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountTitle { get; set; }
    public string? Iban { get; set; }
    public string? SwiftCode { get; set; }
    public int PaymentTermDays { get; set; }
    public string? PaymentTermsNotes { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}
