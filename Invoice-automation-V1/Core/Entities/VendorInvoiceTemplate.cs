namespace InvoiceAutomation.Core.Entities;

public class VendorInvoiceTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VendorId { get; set; }

    public string TemplateName { get; set; } = string.Empty;

    // Field configuration - which fields does this vendor use?
    public bool HasInvoiceNumber { get; set; } = true;
    public bool HasInvoiceDate { get; set; } = true;
    public bool HasDueDate { get; set; } = true;
    public bool HasDescription { get; set; } = true;
    public bool HasLineItems { get; set; } = true;
    public bool HasTaxRate { get; set; } = true;
    public bool HasSubTotal { get; set; } = true;

    // OCR field mapping - labels/keywords to look for in OCR text for each field
    // These help the OCR parser identify the correct values on this vendor's invoices
    public string? InvoiceNumberLabel { get; set; }   // e.g., "Invoice No", "Inv#", "Bill Number"
    public string? InvoiceDateLabel { get; set; }      // e.g., "Invoice Date", "Date", "Bill Date"
    public string? DueDateLabel { get; set; }          // e.g., "Due Date", "Payment Due", "Pay By"
    public string? SubTotalLabel { get; set; }         // e.g., "Sub Total", "Subtotal", "Net Amount"
    public string? TaxLabel { get; set; }              // e.g., "GST", "Tax", "VAT", "Sales Tax"
    public string? TotalLabel { get; set; }            // e.g., "Total", "Grand Total", "Amount Due"

    // Default tax rate for this vendor (if not extracted from OCR)
    public decimal? DefaultTaxRate { get; set; }

    // Default chart of account for this vendor's invoices
    public Guid? DefaultChartOfAccountId { get; set; }

    // Notes about this template
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Vendor Vendor { get; set; } = null!;
    public virtual ChartOfAccount? DefaultChartOfAccount { get; set; }
}
