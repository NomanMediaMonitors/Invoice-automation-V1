namespace InvoiceAutomation.Core.Entities;

public class InvoiceLineItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public decimal AdvanceTaxRate { get; set; }
    public decimal AdvanceTaxAmount { get; set; }
    public decimal SalesTaxRate { get; set; }
    public decimal SalesTaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Chart of Account for expense categorization
    public Guid? ChartOfAccountId { get; set; }
    public string? AccountCode { get; set; }

    // OCR Extracted
    public bool IsOcrExtracted { get; set; }
    public decimal? OcrConfidenceScore { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public Invoice? Invoice { get; set; }
    public ChartOfAccount? ChartOfAccount { get; set; }
}
