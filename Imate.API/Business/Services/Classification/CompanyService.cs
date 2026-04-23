using Imate.API.Business.Interfaces.Classification;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.Classification;
using Imate.API.Presentation.ResponseModels.Classification;
using static Imate.API.Common.Router.APIConfig;

namespace Imate.API.Business.Services.Classification
{
    public class CompanyService : ICompanyService
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IAwsS3StorageService _awsS3Service;

        public CompanyService(ICompanyRepository companyRepository, IAwsS3StorageService awsS3Service)
        {
            _companyRepository = companyRepository;
            _awsS3Service = awsS3Service;
        }

        private CompanyResponseModel MapToResponse(Company company)
        {
            return new CompanyResponseModel
            {
                Id = company.Id,
                Name = company.Name,
                ImageUrl = company.ImageUrl,
                IsActive = company.IsActive,
                CreatedAt = company.CreatedAt,
                UpdatedAt = company.UpdatedAt ?? company.CreatedAt
            };
        }

        public async Task<CompanyResponseModel> CreateCompanyAsync(CreateCompanyRequestModel model)
        {
            if (await _companyRepository.NameExistsAsync(model.Name)) 
            {
                throw new InvalidOperationException($"Tên công ty đã tồn tại.");
            }
            string imageUrl = string.Empty;

            // Xử lý Upload ảnh lên AWS S3
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                imageUrl = await _awsS3Service.UploadFileAsync(model.ImageFile, "companies");
            }

            var newCompany = new Company
            {
                Name = model.Name,
                ImageUrl = imageUrl,
                IsActive = true 
            };

            await _companyRepository.AddAsync(newCompany);

            return MapToResponse(newCompany);
        }

        public async Task<CompanyResponseModel?> GetCompanyDetailsAsync(int id)
        {
            var company = await _companyRepository.GetByIdAsync(id);
            return company != null ? MapToResponse(company) : null;
        }

        public async Task<CompanyResponseModel?> UpdateCompanyAsync(int id, UpdateCompanyRequestModel model)
        {
            var companyToUpdate = await _companyRepository.GetByIdAsync(id);
            if (companyToUpdate == null) return null;

            // DEBUG: Log để kiểm tra
            Console.WriteLine($"[DEBUG] NewImageFile is null: {model.NewImageFile == null}");
            if (model.NewImageFile != null)
            {
                Console.WriteLine($"[DEBUG] NewImageFile.Length: {model.NewImageFile.Length}");
                Console.WriteLine($"[DEBUG] NewImageFile.FileName: {model.NewImageFile.FileName}");
            }

            if (companyToUpdate.Name.ToLower() != model.Name.ToLower())
            {
                if (await _companyRepository.NameExistsExcludingIdAsync(model.Name, id))
                {
                    throw new InvalidOperationException($"Tên công ty đã được sử dụng");
                }
            }

            companyToUpdate.Name = model.Name;
            companyToUpdate.IsActive = model.IsActive;
            companyToUpdate.UpdatedAt = DateTime.UtcNow;


            Console.WriteLine($"[DEBUG] About to check image file...");

            if (model.NewImageFile != null && model.NewImageFile.Length > 0)
            {
                Console.WriteLine($"[DEBUG] ENTERING image upload block");

                
                if (!string.IsNullOrEmpty(companyToUpdate.ImageUrl))
                {
                    Console.WriteLine($"[DEBUG] Deleting old image: {companyToUpdate.ImageUrl}");
                    await _awsS3Service.DeleteFileAsync(companyToUpdate.ImageUrl);
                }

                
                    Console.WriteLine($"[DEBUG] Uploading new image...");
                string newImageUrl = await _awsS3Service.UploadFileAsync(model.NewImageFile, "companies");
                Console.WriteLine($"[DEBUG] New image URL: {newImageUrl}");

                companyToUpdate.ImageUrl = newImageUrl;
            }
            else
            {
                Console.WriteLine($"[DEBUG] NOT entering image upload block");
            }

            Console.WriteLine($"[DEBUG] Final ImageUrl before save: {companyToUpdate.ImageUrl}");

            await _companyRepository.UpdateAsync(companyToUpdate);
            return MapToResponse(companyToUpdate);
        }

        public async Task<CompanyResponseModel?> SetCompanyStatusAsync(int id, bool isActive)
        {
            var updatedCompany = await _companyRepository.SetStatusAsync(id, isActive);
            return updatedCompany != null ? MapToResponse(updatedCompany) : null;
        }

        public async Task<PaginatedCompanyResponseModel> GetCompanyListAsync(CompanyListRequestModel request)
        {
            var (companies, totalCount) = await _companyRepository.GetPagedListAsync(request);

            var items = companies.Select(c => MapToListItemResponse(c)).ToList();

            int totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            return new PaginatedCompanyResponseModel
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Items = items
            };
        }

        private CompanyListItemResponseModel MapToListItemResponse(Company company)
        {
            return new CompanyListItemResponseModel
            {
                Id = company.Id,
                Name = company.Name,
                ImageUrl = company.ImageUrl,
                IsActive = company.IsActive,
                CreatedAt = company.CreatedAt
            };
        }
    }
}
