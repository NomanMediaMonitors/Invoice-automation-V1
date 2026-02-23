using System.ComponentModel.DataAnnotations;
using InvoiceAutomation.Core.Entities;
using Microsoft.AspNetCore.Http;

namespace Invoice_automation_V1.ViewModels;

public class InvoiceListViewModel
{
    public List<InvoiceListItemViewModel> Invoices { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string? StatusFilter { get; set; }
    public string? SearchQuery { get; set; }
}

public class InvoiceListItemViewModel
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "PKR";
    public string Status { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
    public bool IsOcrProcessed { get; set; }
    public int DaysUntilDue { get; set; }
}

public class CreateInvoiceViewModel
{
    public Guid CompanyId { get; set; }

    [Required(ErrorMessage = "Please select a vendor")]
    public Guid VendorId { get; set; }

    // Invoice number is optional at upload time - OCR will extract it
    [StringLength(100)]
    public string? InvoiceNumber { get; set; }

    [DataType(DataType.Date)]
    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public string? Notes { get; set; }

    [Required(ErrorMessage = "Please upload an invoice file")]
    public IFormFile? InvoiceFile { get; set; }

    // For manual entry (if OCR fails or user wants to enter manually)
    public List<InvoiceLineItemViewModel> LineItems { get; set; } = new();

    public bool ProcessWithOcr { get; set; } = true;
}

public class EditInvoiceViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }

    public Guid? VendorId { get; set; }

    [Required]
    [StringLength(100)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime InvoiceDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public string? Notes { get; set; }

    public string Status { get; set; } = "Draft";

    public List<InvoiceLineItemViewModel> LineItems { get; set; } = new();

    // File info (read-only in edit)
    public string? OriginalFileName { get; set; }
    public string? FileUrl { get; set; }

    // For replacing the file
    public IFormFile? NewInvoiceFile { get; set; }
}

public class InvoiceDetailsViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string VendorEmail { get; set; } = string.Empty;
    public string? VendorPhone { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "PKR";
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Notes { get; set; }

    // File Information
    public string? OriginalFileName { get; set; }
    public string? FileUrl { get; set; }
    public string? FileType { get; set; }
    public long? FileSize { get; set; }

    // OCR Information
    public bool IsOcrProcessed { get; set; }
    public DateTime? OcrProcessedAt { get; set; }
    public decimal? OcrConfidenceScore { get; set; }
    public string? OcrErrorMessage { get; set; }

    // Approval Information
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }

    // Payment Information
    public DateTime? PaidAt { get; set; }
    public string? PaymentReference { get; set; }
    public string? PaidByName { get; set; }

    // GL Account Assignments
    public Guid? AdvanceTaxAccountId { get; set; }
    public string? AdvanceTaxAccountName { get; set; }
    public decimal AdvanceTaxAmount { get; set; }
    public Guid? SalesTaxInputAccountId { get; set; }
    public string? SalesTaxInputAccountName { get; set; }
    public decimal SalesTaxInputAmount { get; set; }
    public Guid? PayableVendorsAccountId { get; set; }
    public string? PayableVendorsAccountName { get; set; }

    // GL Posting
    public bool IsPostedToGL { get; set; }
    public DateTime? PostedToGLAt { get; set; }
    public string? PostedToGLByName { get; set; }

    // Vendor Template visibility flags
    public bool HasDueDate { get; set; } = true;
    public bool HasDescription { get; set; } = true;
    public bool HasLineItems { get; set; } = true;
    public bool HasTaxRate { get; set; } = true;
    public bool HasSubTotal { get; set; } = true;
    public bool HasAdvanceTaxAccount { get; set; } = true;
    public bool HasSalesTaxInputAccount { get; set; } = true;
    public bool HasPayableVendorsAccount { get; set; } = true;

    // Audit
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Line Items
    public List<InvoiceLineItemViewModel> LineItems { get; set; } = new();

    // Calculated fields
    public bool IsOverdue { get; set; }
    public int DaysUntilDue { get; set; }
}

public class InvoiceLineItemViewModel
{
    public Guid? Id { get; set; }
    public int LineNumber { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; } = 1;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    [Range(0, 100)]
    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public Guid? ChartOfAccountId { get; set; }
    public string? AccountCode { get; set; }
    public string? AccountName { get; set; }

    public bool IsOcrExtracted { get; set; }
    public decimal? OcrConfidenceScore { get; set; }
}

public class ApproveInvoiceViewModel
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    [StringLength(1000)]
    public string? ApprovalNotes { get; set; }

    public bool Approve { get; set; }
}

public class PayInvoiceViewModel
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Required]
    [StringLength(100)]
    public string PaymentReference { get; set; } = string.Empty;

}

public class OcrResultViewModel
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal ConfidenceScore { get; set; }
    public OcrExtractedData? ExtractedData { get; set; }
}

public class OcrExtractedData
{
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? VendorName { get; set; }
    public decimal? TotalAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? SubTotal { get; set; }
    public List<OcrLineItem> LineItems { get; set; } = new();
    public string? RawText { get; set; }
}

public class OcrLineItem
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public decimal ConfidenceScore { get; set; }
}
