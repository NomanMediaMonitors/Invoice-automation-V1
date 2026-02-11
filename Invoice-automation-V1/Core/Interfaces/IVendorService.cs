using InvoiceAutomation.Core.DTOs.Vendor;
using InvoiceAutomation.Core.Entities;

namespace InvoiceAutomation.Core.Interfaces;

public interface IVendorService
{
    Task<List<VendorListDto>> GetCompanyVendorsAsync(Guid companyId);
    Task<List<VendorListDto>> GetActiveVendorsAsync(Guid companyId);
    Task<VendorDetailsDto?> GetVendorDetailsAsync(Guid id);
    Task<Vendor> CreateVendorAsync(CreateVendorDto dto, Guid createdByUserId);
    Task<Vendor> UpdateVendorAsync(Guid id, UpdateVendorDto dto);
    Task DeleteVendorAsync(Guid id);
    Task ActivateVendorAsync(Guid id);
    Task DeactivateVendorAsync(Guid id);
    Task<bool> VendorExistsAsync(Guid companyId, string email);
}
