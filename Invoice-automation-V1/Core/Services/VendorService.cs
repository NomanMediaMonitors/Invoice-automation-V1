using InvoiceAutomation.Core.DTOs.Vendor;
using InvoiceAutomation.Core.Entities;
using InvoiceAutomation.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InvoiceAutomation.Core.Services;

public class VendorService : IVendorService
{
    private readonly IVendorRepository _vendorRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<VendorService> _logger;

    public VendorService(
        IVendorRepository vendorRepository,
        IUserRepository userRepository,
        ILogger<VendorService> logger)
    {
        _vendorRepository = vendorRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<List<VendorListDto>> GetCompanyVendorsAsync(Guid companyId)
    {
        var vendors = await _vendorRepository.GetByCompanyIdAsync(companyId);

        return vendors.Select(v => new VendorListDto
        {
            Id = v.Id,
            Name = v.Name,
            Email = v.Email,
            Phone = v.Phone,
            Type = v.Type,
            City = v.City,
            Ntn = v.Ntn,
            IsActive = v.IsActive,
            PaymentTermDays = v.PaymentTermDays,
            CreatedAt = v.CreatedAt
        }).ToList();
    }

    public async Task<List<VendorListDto>> GetActiveVendorsAsync(Guid companyId)
    {
        var vendors = await _vendorRepository.GetActiveByCompanyIdAsync(companyId);

        return vendors.Select(v => new VendorListDto
        {
            Id = v.Id,
            Name = v.Name,
            Email = v.Email,
            Phone = v.Phone,
            Type = v.Type,
            City = v.City,
            Ntn = v.Ntn,
            IsActive = v.IsActive,
            PaymentTermDays = v.PaymentTermDays,
            CreatedAt = v.CreatedAt
        }).ToList();
    }

    public async Task<VendorDetailsDto?> GetVendorDetailsAsync(Guid id)
    {
        var vendor = await _vendorRepository.GetByIdAsync(id);
        if (vendor == null)
        {
            return null;
        }

        var createdByUser = await _userRepository.GetByIdAsync(vendor.CreatedBy);

        return new VendorDetailsDto
        {
            Id = vendor.Id,
            CompanyId = vendor.CompanyId,
            Name = vendor.Name,
            Email = vendor.Email,
            Phone = vendor.Phone,
            MobilePhone = vendor.MobilePhone,
            Website = vendor.Website,
            Type = vendor.Type,
            Ntn = vendor.Ntn,
            Strn = vendor.Strn,
            RegistrationNumber = vendor.RegistrationNumber,
            Address = vendor.Address,
            City = vendor.City,
            State = vendor.State,
            PostalCode = vendor.PostalCode,
            Country = vendor.Country,
            ContactPersonName = vendor.ContactPersonName,
            ContactPersonEmail = vendor.ContactPersonEmail,
            ContactPersonPhone = vendor.ContactPersonPhone,
            BankName = vendor.BankName,
            BankAccountNumber = vendor.BankAccountNumber,
            BankAccountTitle = vendor.BankAccountTitle,
            Iban = vendor.Iban,
            SwiftCode = vendor.SwiftCode,
            PaymentTermDays = vendor.PaymentTermDays,
            PaymentTermsNotes = vendor.PaymentTermsNotes,
            Notes = vendor.Notes,
            IsActive = vendor.IsActive,
            CreatedAt = vendor.CreatedAt,
            UpdatedAt = vendor.UpdatedAt,
            CreatedByName = createdByUser?.FullName ?? "Unknown"
        };
    }

    public async Task<Vendor> CreateVendorAsync(CreateVendorDto dto, Guid createdByUserId)
    {
        // Check if vendor with same email already exists in this company
        if (await _vendorRepository.ExistsAsync(dto.CompanyId, dto.Email))
        {
            throw new InvalidOperationException($"A vendor with email '{dto.Email}' already exists in this company");
        }

        var vendor = new Vendor
        {
            CompanyId = dto.CompanyId,
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            MobilePhone = dto.MobilePhone,
            Website = dto.Website,
            Type = dto.Type,
            Ntn = dto.Ntn,
            Strn = dto.Strn,
            RegistrationNumber = dto.RegistrationNumber,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            PostalCode = dto.PostalCode,
            Country = dto.Country,
            ContactPersonName = dto.ContactPersonName,
            ContactPersonEmail = dto.ContactPersonEmail,
            ContactPersonPhone = dto.ContactPersonPhone,
            BankName = dto.BankName,
            BankAccountNumber = dto.BankAccountNumber,
            BankAccountTitle = dto.BankAccountTitle,
            Iban = dto.Iban,
            SwiftCode = dto.SwiftCode,
            PaymentTermDays = dto.PaymentTermDays,
            PaymentTermsNotes = dto.PaymentTermsNotes,
            Notes = dto.Notes,
            IsActive = true,
            CreatedBy = createdByUserId
        };

        vendor = await _vendorRepository.AddAsync(vendor);

        _logger.LogInformation("Vendor {VendorId} created for company {CompanyId} by user {UserId}",
            vendor.Id, dto.CompanyId, createdByUserId);

        return vendor;
    }

    public async Task<Vendor> UpdateVendorAsync(Guid id, UpdateVendorDto dto)
    {
        var vendor = await _vendorRepository.GetByIdAsync(id);
        if (vendor == null)
        {
            throw new InvalidOperationException("Vendor not found");
        }

        // Check if email is being changed and if new email already exists
        if (vendor.Email != dto.Email && await _vendorRepository.ExistsAsync(vendor.CompanyId, dto.Email))
        {
            throw new InvalidOperationException($"A vendor with email '{dto.Email}' already exists in this company");
        }

        vendor.Name = dto.Name;
        vendor.Email = dto.Email;
        vendor.Phone = dto.Phone;
        vendor.MobilePhone = dto.MobilePhone;
        vendor.Website = dto.Website;
        vendor.Type = dto.Type;
        vendor.Ntn = dto.Ntn;
        vendor.Strn = dto.Strn;
        vendor.RegistrationNumber = dto.RegistrationNumber;
        vendor.Address = dto.Address;
        vendor.City = dto.City;
        vendor.State = dto.State;
        vendor.PostalCode = dto.PostalCode;
        vendor.Country = dto.Country;
        vendor.ContactPersonName = dto.ContactPersonName;
        vendor.ContactPersonEmail = dto.ContactPersonEmail;
        vendor.ContactPersonPhone = dto.ContactPersonPhone;
        vendor.BankName = dto.BankName;
        vendor.BankAccountNumber = dto.BankAccountNumber;
        vendor.BankAccountTitle = dto.BankAccountTitle;
        vendor.Iban = dto.Iban;
        vendor.SwiftCode = dto.SwiftCode;
        vendor.PaymentTermDays = dto.PaymentTermDays;
        vendor.PaymentTermsNotes = dto.PaymentTermsNotes;
        vendor.Notes = dto.Notes;

        await _vendorRepository.UpdateAsync(vendor);

        _logger.LogInformation("Vendor {VendorId} updated", vendor.Id);

        return vendor;
    }

    public async Task DeleteVendorAsync(Guid id)
    {
        var vendor = await _vendorRepository.GetByIdAsync(id);
        if (vendor == null)
        {
            throw new InvalidOperationException("Vendor not found");
        }

        await _vendorRepository.DeleteAsync(vendor);

        _logger.LogInformation("Vendor {VendorId} deleted", id);
    }

    public async Task ActivateVendorAsync(Guid id)
    {
        var vendor = await _vendorRepository.GetByIdAsync(id);
        if (vendor == null)
        {
            throw new InvalidOperationException("Vendor not found");
        }

        vendor.IsActive = true;
        await _vendorRepository.UpdateAsync(vendor);

        _logger.LogInformation("Vendor {VendorId} activated", id);
    }

    public async Task DeactivateVendorAsync(Guid id)
    {
        var vendor = await _vendorRepository.GetByIdAsync(id);
        if (vendor == null)
        {
            throw new InvalidOperationException("Vendor not found");
        }

        vendor.IsActive = false;
        await _vendorRepository.UpdateAsync(vendor);

        _logger.LogInformation("Vendor {VendorId} deactivated", id);
    }

    public async Task<bool> VendorExistsAsync(Guid companyId, string email)
    {
        return await _vendorRepository.ExistsAsync(companyId, email);
    }
}
