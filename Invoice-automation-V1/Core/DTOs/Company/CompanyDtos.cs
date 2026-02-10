using System.ComponentModel.DataAnnotations;

namespace InvoiceAutomation.Core.DTOs.Company;

public class CreateCompanyDto
{
    [Required(ErrorMessage = "Company name is required")]
    [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "NTN is required")]
    [StringLength(20, ErrorMessage = "NTN cannot exceed 20 characters")]
    public string Ntn { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Strn { get; set; }

    [Required(ErrorMessage = "Address is required")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Url(ErrorMessage = "Invalid website URL")]
    public string? Website { get; set; }

    [Required(ErrorMessage = "Fiscal year start date is required")]
    public DateTime FiscalYearStart { get; set; } = new DateTime(DateTime.Now.Year, 7, 1);

    public string DefaultCurrency { get; set; } = "PKR";
}

public class UpdateCompanyDto : CreateCompanyDto
{
    public Guid Id { get; set; }
}

public class ConnectIndraajDto
{
    [Required(ErrorMessage = "Access token is required")]
    public string AccessToken { get; set; } = string.Empty;
}

public class CompanyDropdownDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Ntn { get; set; } = string.Empty;
    public bool IsUserDefault { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime? LastCoaSyncAt { get; set; }
    public bool HasIndraajConnection { get; set; }

    public string DisplayText => IsUserDefault ? $"⭐ {Name}" : Name;
    public bool CanSync => !LastCoaSyncAt.HasValue ||
                           (DateTime.UtcNow - LastCoaSyncAt.Value).TotalDays >= 7;
}
