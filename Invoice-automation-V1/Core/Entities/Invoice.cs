using System.ComponentModel.DataAnnotations;

namespace InvoiceAutomation.Core.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? VendorId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "PKR";
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string? Description { get; set; }
    public string? Notes { get; set; }

    // OCR Related Fields
    public string? OriginalFileName { get; set; }
    public string? FileStoragePath { get; set; }
    public string? FileUrl { get; set; }
    public string? FileType { get; set; }
    public long? FileSize { get; set; }
    public bool IsOcrProcessed { get; set; }
    public DateTime? OcrProcessedAt { get; set; }
    public decimal? OcrConfidenceScore { get; set; }
    public string? OcrRawData { get; set; }
    public string? OcrErrorMessage { get; set; }

    // Approval Workflow
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }

    // Payment Information
    public DateTime? PaidAt { get; set; }
    public string? PaymentReference { get; set; }
    public Guid? PaidBy { get; set; }

    // Indraaj Integration
    public string? IndraajVoucherNo { get; set; }
    public DateTime? SyncedToIndraajAt { get; set; }

    // Audit Fields
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation Properties
    public Company? Company { get; set; }
    public Vendor? Vendor { get; set; }
    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
}

public enum InvoiceStatus
{
    Draft,
    PendingApproval,
    Approved,
    Rejected,
    Paid,
    Cancelled,
    Overdue
}
