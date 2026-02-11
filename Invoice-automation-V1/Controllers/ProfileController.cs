using System.Security.Claims;
using Invoice_automation_V1.ViewModels;
using InvoiceAutomation.Core.Entities;
using InvoiceAutomation.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Invoice_automation_V1.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            ApplicationDbContext context,
            ILogger<ProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new ProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                EmailConfirmed = user.EmailConfirmed,
                IsSuperAdmin = user.IsSuperAdmin,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            // Get user's companies
            var userCompanies = await _context.UserCompanies
                .Where(uc => uc.UserId == userId)
                .Include(uc => uc.Company)
                .ToListAsync();

            viewModel.Stats.TotalCompanies = userCompanies.Count;
            viewModel.Stats.ActiveCompanies = userCompanies.Count(uc => uc.Company.IsActive);
            viewModel.Stats.CompaniesAsAdmin = userCompanies.Count(uc =>
                uc.Role == UserRole.SuperAdmin || uc.Role == UserRole.Admin);
            viewModel.Stats.CompaniesAsManager = userCompanies.Count(uc => uc.Role == UserRole.Manager);
            viewModel.Stats.DaysActive = (DateTime.UtcNow - user.CreatedAt).Days;

            // Get vendors created by user
            var companyIds = userCompanies.Select(uc => uc.CompanyId).ToList();
            viewModel.Stats.TotalVendorsCreated = await _context.Vendors
                .Where(v => companyIds.Contains(v.CompanyId) && v.CreatedBy == userId)
                .CountAsync();

            // Build recent activity
            var recentActivity = new List<ProfileActivityItem>();

            // Add company joins
            foreach (var uc in userCompanies.OrderByDescending(uc => uc.CreatedAt).Take(5))
            {
                recentActivity.Add(new ProfileActivityItem
                {
                    Icon = "building",
                    IconColor = "primary",
                    Action = "Joined Company",
                    Details = uc.Company.Name,
                    Timestamp = uc.CreatedAt,
                    TimeAgo = GetTimeAgo(uc.CreatedAt)
                });
            }

            // Add vendors created
            var recentVendors = await _context.Vendors
                .Where(v => companyIds.Contains(v.CompanyId) && v.CreatedBy == userId)
                .OrderByDescending(v => v.CreatedAt)
                .Take(5)
                .ToListAsync();

            foreach (var vendor in recentVendors)
            {
                recentActivity.Add(new ProfileActivityItem
                {
                    Icon = "truck",
                    IconColor = "success",
                    Action = "Created Vendor",
                    Details = vendor.Name,
                    Timestamp = vendor.CreatedAt,
                    TimeAgo = GetTimeAgo(vendor.CreatedAt)
                });
            }

            viewModel.RecentActivity = recentActivity
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .ToList();

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new EditProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Check if email changed and if new email already exists
            if (user.Email != model.Email)
            {
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == model.Email && u.Id != userId);

                if (emailExists)
                {
                    ModelState.AddModelError("Email", "This email address is already in use");
                    return View(model);
                }

                user.Email = model.Email;
                user.NormalizedEmail = model.Email.ToUpperInvariant();
                user.EmailConfirmed = false; // Require re-verification
            }

            user.FullName = model.FullName;
            user.Phone = model.Phone;
            user.AvatarUrl = model.AvatarUrl;
            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Profile updated successfully";

                if (!user.EmailConfirmed)
                {
                    TempData["Warning"] = "Email changed. Please verify your new email address.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                ModelState.AddModelError("", "An error occurred while updating your profile");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                return View(model);
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Password changed successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                ModelState.AddModelError("", "An error occurred while changing your password");
                return View(model);
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)}w ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)}mo ago";

            return $"{(int)(timeSpan.TotalDays / 365)}y ago";
        }
    }
}
