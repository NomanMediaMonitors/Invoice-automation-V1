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
    public bool HasSubTotal { get; set; } = true;

    // OCR field mapping - labels/keywords to look for in OCR text for each field
    // These help the OCR parser identify the correct values on this vendor's invoices
    public string? InvoiceNumberLabel { get; set; }   // e.g., "Invoice No", "Inv#", "Bill Number"
    public string? InvoiceDateLabel { get; set; }      // e.g., "Invoice Date", "Date", "Bill Date"
    public string? DueDateLabel { get; set; }          // e.g., "Due Date", "Payment Due", "Pay By"
    public string? SubTotalLabel { get; set; }         // e.g., "Sub Total", "Subtotal", "Net Amount"
    public string? TaxLabel { get; set; }              // e.g., "GST", "Tax", "VAT", "Sales Tax"
    public string? TotalLabel { get; set; }            // e.g., "Total", "Grand Total", "Amount Due"
    public string? AdvanceTaxAmountLabel { get; set; }  // e.g., "Advance Tax", "WHT", "Withholding Tax"
    public string? SalesTaxInputAmountLabel { get; set; } // e.g., "Sales Tax", "GST", "Input Tax"

    // GL Account configuration - which account categories does this vendor use?
    public bool HasAdvanceTaxAccount { get; set; } = true;
    public bool HasSalesTaxInputAccount { get; set; } = true;
    // Default GL accounts for this vendor's invoices
    public Guid? DefaultAdvanceTaxAccountId { get; set; }
    public Guid? DefaultSalesTaxInputAccountId { get; set; }
    public Guid? DefaultPayableVendorsAccountId { get; set; }

    // Default tax rates for this vendor (if not extracted from OCR)
    public decimal? DefaultAdvanceTaxRate { get; set; }
    public decimal? DefaultSalesTaxRate { get; set; }

    // Default chart of account for this vendor's invoices (legacy - for line items)
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
    public virtual ChartOfAccount? DefaultAdvanceTaxAccount { get; set; }
    public virtual ChartOfAccount? DefaultSalesTaxInputAccount { get; set; }
    public virtual ChartOfAccount? DefaultPayableVendorsAccount { get; set; }
}
