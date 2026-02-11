namespace Invoice_automation_V1.ViewModels
{
    public class DashboardViewModel
    {
        public DashboardStats Stats { get; set; } = new();
        public List<RecentActivityItem> RecentActivities { get; set; } = new();
        public string UserFullName { get; set; } = string.Empty;
        public bool HasAnyCompanies { get; set; }
    }

    public class DashboardStats
    {
        public int TotalCompanies { get; set; }
        public int TotalVendors { get; set; }
        public int TotalInvoices { get; set; }
        public int ApprovedInvoices { get; set; }
        public int PendingApprovals { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ThisMonthAmount { get; set; }

        // Trend indicators (percentage change from last month)
        public decimal CompaniesTrend { get; set; }
        public decimal VendorsTrend { get; set; }
        public decimal InvoicesTrend { get; set; }
        public decimal ApprovedTrend { get; set; }
    }

    public class RecentActivityItem
    {
        public string Icon { get; set; } = string.Empty;
        public string IconColor { get; set; } = "primary";
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }
}
