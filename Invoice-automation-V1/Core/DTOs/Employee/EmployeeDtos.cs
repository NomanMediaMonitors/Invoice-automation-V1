using System.ComponentModel.DataAnnotations;
using InvoiceAutomation.Core.Entities;

namespace InvoiceAutomation.Core.DTOs.Employee;

public class InviteEmployeeDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Role is required")]
    public UserRole Role { get; set; }

    public Guid CompanyId { get; set; }
}

public class EmployeeListDto
{
    public Guid UserId { get; set; }
    public Guid UserCompanyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool IsUserDefault { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? AvatarUrl { get; set; }
}

public class UpdateEmployeeRoleDto
{
    [Required(ErrorMessage = "Role is required")]
    public UserRole Role { get; set; }
}

public class EmployeeDetailsDto
{
    public Guid UserId { get; set; }
    public Guid UserCompanyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool IsUserDefault { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? InvitedAt { get; set; }
    public Guid? InvitedBy { get; set; }
    public string? InvitedByName { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
