using System.ComponentModel.DataAnnotations;

namespace InvoiceAutomation.Core.DTOs.Company;

public class CreateCompanyDto
{
    [Required(ErrorMessage = "Company name is required")]
    [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "NTN is required")]
    [StringLength(50, ErrorMessage = "NTN cannot exceed 50 characters")]
    public string Ntn { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Strn { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(255)]
    public string? Email { get; set; }
}

public class UpdateCompanyDto
{
    [Required(ErrorMessage = "Company name is required")]
    [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "NTN is required")]
    [StringLength(50, ErrorMessage = "NTN cannot exceed 50 characters")]
    public string Ntn { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Strn { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(255)]
    public string? Email { get; set; }
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
