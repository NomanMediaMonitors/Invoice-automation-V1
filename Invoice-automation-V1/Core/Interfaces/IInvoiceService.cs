using InvoiceAutomation.Core.Entities;
using Invoice_automation_V1.ViewModels;

namespace InvoiceAutomation.Core.Interfaces;

public interface IInvoiceService
{
    Task<(bool Success, string Message, Guid? InvoiceId)> CreateInvoiceAsync(CreateInvoiceViewModel model, Guid userId);
    Task<(bool Success, string Message)> UpdateInvoiceAsync(EditInvoiceViewModel model, Guid userId);
    Task<(bool Success, string Message)> DeleteInvoiceAsync(Guid invoiceId, Guid userId);
    Task<InvoiceDetailsViewModel?> GetInvoiceDetailsAsync(Guid invoiceId, Guid userId);
    Task<InvoiceListViewModel> GetInvoicesAsync(Guid companyId, int pageNumber, int pageSize, string? statusFilter, string? searchQuery);
    Task<(bool Success, string Message)> ApproveInvoiceAsync(Guid invoiceId, Guid userId, string? approvalNotes);
    Task<(bool Success, string Message)> RejectInvoiceAsync(Guid invoiceId, Guid userId, string? rejectionNotes);
    Task<(bool Success, string Message)> MarkAsPaidAsync(Guid invoiceId, Guid userId, DateTime paymentDate, string paymentReference);
    Task<(bool Success, string Message)> SyncToIndraajAsync(Guid invoiceId, Guid userId);
    Task<(bool Success, string Message)> ProcessOcrAsync(Guid invoiceId);
    Task<(bool Success, string Message)> PostToGLAsync(Guid invoiceId, Guid userId);
    Task<bool> CanUserAccessInvoiceAsync(Guid invoiceId, Guid userId);
}
