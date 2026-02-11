using System.ComponentModel.DataAnnotations;

namespace Invoice_automation_V1.ViewModels
{
    public class ProfileViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Display(Name = "Avatar URL")]
        public string? AvatarUrl { get; set; }

        public bool EmailConfirmed { get; set; }
        public bool IsSuperAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Statistics
        public ProfileStats Stats { get; set; } = new();

        // Recent Activity
        public List<ProfileActivityItem> RecentActivity { get; set; } = new();
    }

    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(200, ErrorMessage = "Full name cannot exceed 200 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Url(ErrorMessage = "Invalid URL")]
        [Display(Name = "Avatar URL")]
        public string? AvatarUrl { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ProfileStats
    {
        public int TotalCompanies { get; set; }
        public int ActiveCompanies { get; set; }
        public int CompaniesAsAdmin { get; set; }
        public int CompaniesAsManager { get; set; }
        public int TotalVendorsCreated { get; set; }
        public int TotalInvoicesUploaded { get; set; }
        public int DaysActive { get; set; }
    }

    public class ProfileActivityItem
    {
        public string Icon { get; set; } = string.Empty;
        public string IconColor { get; set; } = "primary";
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }
}
