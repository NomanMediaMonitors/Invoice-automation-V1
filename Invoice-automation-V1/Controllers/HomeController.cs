using System.Diagnostics;
using System.Security.Claims;
using Invoice_automation_V1.Models;
using Invoice_automation_V1.ViewModels;
using InvoiceAutomation.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Invoice_automation_V1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // Redirect to dashboard if authenticated
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Dashboard));
            }
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new DashboardViewModel();

            // Get user info
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                viewModel.UserFullName = user.FullName;
            }

            // Get user's companies
            var userCompanyIds = await _context.UserCompanies
                .Where(uc => uc.UserId == userId)
                .Select(uc => uc.CompanyId)
                .ToListAsync();

            viewModel.HasAnyCompanies = userCompanyIds.Any();
            viewModel.DefaultCompanyId = userCompanyIds.FirstOrDefault();

            if (viewModel.HasAnyCompanies)
            {
                // Count companies
                viewModel.Stats.TotalCompanies = userCompanyIds.Count;

                // Count vendors across all user's companies
                viewModel.Stats.TotalVendors = await _context.Vendors
                    .Where(v => userCompanyIds.Contains(v.CompanyId) && v.IsActive)
                    .CountAsync();

                // Get recent activities (last 10 companies and vendors created)
                var recentCompanies = await _context.Companies
                    .Where(c => userCompanyIds.Contains(c.Id))
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .Select(c => new { Type = "Company", Name = c.Name, c.CreatedAt })
                    .ToListAsync();

                var recentVendors = await _context.Vendors
                    .Where(v => userCompanyIds.Contains(v.CompanyId))
                    .OrderByDescending(v => v.CreatedAt)
                    .Take(5)
                    .Select(v => new { Type = "Vendor", Name = v.Name, v.CreatedAt })
                    .ToListAsync();

                // Combine and sort recent activities
                var allActivities = new List<RecentActivityItem>();

                foreach (var company in recentCompanies)
                {
                    allActivities.Add(new RecentActivityItem
                    {
                        Icon = "building",
                        IconColor = "primary",
                        Title = "Company Created",
                        Description = company.Name,
                        Timestamp = company.CreatedAt,
                        TimeAgo = GetTimeAgo(company.CreatedAt)
                    });
                }

                foreach (var vendor in recentVendors)
                {
                    allActivities.Add(new RecentActivityItem
                    {
                        Icon = "truck",
                        IconColor = "success",
                        Title = "Vendor Added",
                        Description = vendor.Name,
                        Timestamp = vendor.CreatedAt,
                        TimeAgo = GetTimeAgo(vendor.CreatedAt)
                    });
                }

                viewModel.RecentActivities = allActivities
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .ToList();

                // Calculate trends (dummy for now - would need historical data)
                viewModel.Stats.CompaniesTrend = 0;
                viewModel.Stats.VendorsTrend = viewModel.Stats.TotalVendors > 0 ? 15 : 0;
                viewModel.Stats.InvoicesTrend = 0;
                viewModel.Stats.ApprovedTrend = 0;
            }

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
