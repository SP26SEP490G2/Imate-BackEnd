using Moq;
using FluentAssertions;
using Imate.API.Business.Services.Classification;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.Classification;
using Imate.API.Presentation.ResponseModels.Classification;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Imate.API.UnitTest.Services
{
    public class CompanyServiceTests
    {
        private readonly Mock<ICompanyRepository> _mockCompanyRepo;
        private readonly Mock<IAwsS3StorageService> _mockAwsS3Service;
        private readonly CompanyService _service;

        public CompanyServiceTests()
        {
            _mockCompanyRepo = new Mock<ICompanyRepository>();
            _mockAwsS3Service = new Mock<IAwsS3StorageService>();

            _service = new CompanyService(
                _mockCompanyRepo.Object,
                _mockAwsS3Service.Object);
        }

        #region CreateCompanyAsync

        // Kiểm tra tạo company thành công khi tên chưa tồn tại, không có ảnh → ImageUrl rỗng, verify repo gọi 1 lần
        [Fact]
        public async Task CreateCompanyAsync_ShouldCreateCompany_WhenNameIsUnique()
        {
            var request = new CreateCompanyRequestModel { Name = "FPT Software", ImageFile = null };
            _mockCompanyRepo.Setup(r => r.NameExistsAsync("FPT Software")).ReturnsAsync(false);
            _mockCompanyRepo.Setup(r => r.AddAsync(It.IsAny<Company>())).Returns(Task.CompletedTask);

            var result = await _service.CreateCompanyAsync(request);

            result.Should().NotBeNull();
            result.Name.Should().Be("FPT Software");
            result.IsActive.Should().BeTrue();
            result.ImageUrl.Should().BeEmpty();
            _mockCompanyRepo.Verify(r => r.AddAsync(It.Is<Company>(c => c.Name == "FPT Software" && c.IsActive)), Times.Once);
        }

        // Kiểm tra upload ảnh lên S3 khi ImageFile được cung cấp → ImageUrl chứa URL từ S3
        [Fact]
        public async Task CreateCompanyAsync_ShouldUploadImage_WhenImageFileProvided()
        {
            var mockFile = CreateMockFormFile("logo.png", 1024);
            var request = new CreateCompanyRequestModel { Name = "VNG Corp", ImageFile = mockFile.Object };

            _mockCompanyRepo.Setup(r => r.NameExistsAsync("VNG Corp")).ReturnsAsync(false);
            _mockCompanyRepo.Setup(r => r.AddAsync(It.IsAny<Company>())).Returns(Task.CompletedTask);
            _mockAwsS3Service
                .Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), "companies"))
                .ReturnsAsync("https://s3.amazonaws.com/companies/logo.png");

            var result = await _service.CreateCompanyAsync(request);

            result.ImageUrl.Should().Be("https://s3.amazonaws.com/companies/logo.png");
            _mockAwsS3Service.Verify(s => s.UploadFileAsync(It.IsAny<IFormFile>(), "companies"), Times.Once);
        }

        // Kiểm tra KHÔNG gọi upload S3 khi ImageFile là null
        [Fact]
        public async Task CreateCompanyAsync_ShouldNotUploadImage_WhenImageFileIsNull()
        {
            var request = new CreateCompanyRequestModel { Name = "Tiki", ImageFile = null };
            _mockCompanyRepo.Setup(r => r.NameExistsAsync("Tiki")).ReturnsAsync(false);
            _mockCompanyRepo.Setup(r => r.AddAsync(It.IsAny<Company>())).Returns(Task.CompletedTask);

            await _service.CreateCompanyAsync(request);

            _mockAwsS3Service.Verify(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
        }

        // Kiểm tra ném InvalidOperationException khi tên company đã tồn tại trong database
        [Fact]
        public async Task CreateCompanyAsync_ShouldThrowInvalidOperationException_WhenNameAlreadyExists()
        {
            var request = new CreateCompanyRequestModel { Name = "FPT Software" };
            _mockCompanyRepo.Setup(r => r.NameExistsAsync("FPT Software")).ReturnsAsync(true);

            var act = () => _service.CreateCompanyAsync(request);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Tên công ty đã tồn tại*");
        }

        // Kiểm tra company mới tạo luôn có IsActive = true mặc định
        [Fact]
        public async Task CreateCompanyAsync_ShouldSetIsActiveTrue_ByDefault()
        {
            var request = new CreateCompanyRequestModel { Name = "Zalo" };
            _mockCompanyRepo.Setup(r => r.NameExistsAsync("Zalo")).ReturnsAsync(false);
            _mockCompanyRepo.Setup(r => r.AddAsync(It.IsAny<Company>())).Returns(Task.CompletedTask);

            var result = await _service.CreateCompanyAsync(request);

            result.IsActive.Should().BeTrue();
        }

        #endregion

        #region GetCompanyDetailsAsync

        // Kiểm tra trả về company response đầy đủ khi tìm thấy theo ID
        [Fact]
        public async Task GetCompanyDetailsAsync_ShouldReturnCompany_WhenFound()
        {
            var company = new Company
            {
                Id = 1, Name = "FPT Software", ImageUrl = "https://img.com/fpt.png",
                IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = null
            };
            _mockCompanyRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(company);

            var result = await _service.GetCompanyDetailsAsync(1);

            result.Should().NotBeNull();
            result!.Name.Should().Be("FPT Software");
            result.ImageUrl.Should().Be("https://img.com/fpt.png");
            result.IsActive.Should().BeTrue();
        }

        // Kiểm tra trả null khi company ID không tồn tại
        [Fact]
        public async Task GetCompanyDetailsAsync_ShouldReturnNull_WhenNotFound()
        {
            _mockCompanyRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Company?)null);

            var result = await _service.GetCompanyDetailsAsync(999);

            result.Should().BeNull();
        }

        // Kiểm tra MapToResponse: khi UpdatedAt = null → UpdatedAt fallback về CreatedAt
        [Fact]
        public async Task GetCompanyDetailsAsync_ShouldMapUpdatedAtToCreatedAt_WhenUpdatedAtIsNull()
        {
            var createdAt = DateTimeOffset.UtcNow.AddDays(-5);
            var company = new Company
            {
                Id = 1, Name = "Test", IsActive = true,
                CreatedAt = createdAt, UpdatedAt = null
            };
            _mockCompanyRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(company);

            var result = await _service.GetCompanyDetailsAsync(1);

            result!.UpdatedAt.Should().Be(createdAt);
        }

        #endregion

        #region UpdateCompanyAsync

        // Kiểm tra cập nhật Name thành công khi company tồn tại, verify repo UpdateAsync gọi 1 lần
        [Fact]
        public async Task UpdateCompanyAsync_ShouldUpdateNameAndStatus_WhenCompanyExists()
        {
            var existingCompany = new Company { Id = 1, Name = "Old Name", IsActive = true };
            var request = new UpdateCompanyRequestModel { Name = "New Name", IsActive = true, NewImageFile = null };

            _mockCompanyRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingCompany);
            _mockCompanyRepo.Setup(r => r.UpdateAsync(It.IsAny<Company>())).Returns(Task.CompletedTask);

            var result = await _service.UpdateCompanyAsync(1, request);

            result.Should().NotBeNull();
            result!.Name.Should().Be("New Name");
            _mockCompanyRepo.Verify(r => r.UpdateAsync(It.Is<Company>(c => c.Name == "New Name")), Times.Once);
        }

        // Kiểm tra trả null khi company ID không tồn tại
        [Fact]
        public async Task UpdateCompanyAsync_ShouldReturnNull_WhenCompanyNotFound()
        {
            _mockCompanyRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Company?)null);
            var request = new UpdateCompanyRequestModel { Name = "Test", IsActive = true };

            var result = await _service.UpdateCompanyAsync(999, request);

            result.Should().BeNull();
        }

        // Kiểm tra ném InvalidOperationException khi đổi sang tên đã được sử dụng bởi company khác
        [Fact]
        public async Task UpdateCompanyAsync_ShouldThrowInvalidOperationException_WhenNameIsDuplicate()
        {
            var existingCompany = new Company { Id = 1, Name = "FPT", IsActive = true };
            var request = new UpdateCompanyRequestModel { Name = "VNG", IsActive = true };

            _mockCompanyRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingCompany);
            _mockCompanyRepo.Setup(r => r.NameExistsExcludingIdAsync("VNG", 1)).ReturnsAsync(true);

            var act = () => _service.UpdateCompanyAsync(1, request);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Tên công ty đã được sử dụng*");
        }

        // Kiểm tra KHÔNG gọi NameExistsExcludingIdAsync khi tên không thay đổi (skip duplicate check)
        [Fact]
        public async Task UpdateCompanyAsync_ShouldNotCheckDuplicateName_WhenNameIsUnchanged()
        {
            var existingCompany = new Company { Id = 1, Name = "FPT", IsActive = true };
            var request = new UpdateCompanyRequestModel { Name = "FPT", IsActive = false, NewImageFile = null };

            _mockCompanyRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingCompany);
            _mockCompanyRepo.Setup(r => r.UpdateAsync(It.IsAny<Company>())).Returns(Task.CompletedTask);

            await _service.UpdateCompanyAsync(1, request);

            _mockCompanyRepo.Verify(r => r.NameExistsExcludingIdAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        // Kiểm tra upload ảnh mới + xóa ảnh cũ trên S3 khi có NewImageFile và ảnh cũ tồn tại
        [Fact]
        public async Task UpdateCompanyAsync_ShouldUploadNewImage_AndDeleteOldImage()
        {
            var existingCompany = new Company
            {
                Id = 1, Name = "FPT", IsActive = true,
                ImageUrl = "https://s3.amazonaws.com/old-logo.png"
            };
            var mockFile = CreateMockFormFile("new-logo.png", 2048);
            var request = new UpdateCompanyRequestModel { Name = "FPT", IsActive = true, NewImageFile = mockFile.Object };

            _mockCompanyRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingCompany);
            _mockCompanyRepo.Setup(r => r.UpdateAsync(It.IsAny<Company>())).Returns(Task.CompletedTask);
            _mockAwsS3Service.Setup(s => s.DeleteFileAsync("https://s3.amazonaws.com/old-logo.png")).Returns(Task.CompletedTask);
            _mockAwsS3Service
                .Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), "companies"))
                .ReturnsAsync("https://s3.amazonaws.com/new-logo.png");

            var result = await _service.UpdateCompanyAsync(1, request);

            result!.ImageUrl.Should().Be("https://s3.amazonaws.com/new-logo.png");
            _mockAwsS3Service.Verify(s => s.DeleteFileAsync("https://s3.amazonaws.com/old-logo.png"), Times.Once);
            _mockAwsS3Service.Verify(s => s.UploadFileAsync(It.IsAny<IFormFile>(), "companies"), Times.Once);
        }

        // Kiểm tra KHÔNG xóa ảnh cũ khi OldImageUrl rỗng, chỉ upload ảnh mới
        [Fact]
        public async Task UpdateCompanyAsync_ShouldNotDeleteOldImage_WhenOldImageUrlIsEmpty()
        {
            var existingCompany = new Company { Id = 1, Name = "FPT", IsActive = true, ImageUrl = "" };
            var mockFile = CreateMockFormFile("logo.png", 1024);
            var request = new UpdateCompanyRequestModel { Name = "FPT", IsActive = true, NewImageFile = mockFile.Object };

            _mockCompanyRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingCompany);
            _mockCompanyRepo.Setup(r => r.UpdateAsync(It.IsAny<Company>())).Returns(Task.CompletedTask);
            _mockAwsS3Service
                .Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), "companies"))
                .ReturnsAsync("https://s3.amazonaws.com/logo.png");

            await _service.UpdateCompanyAsync(1, request);

            _mockAwsS3Service.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
            _mockAwsS3Service.Verify(s => s.UploadFileAsync(It.IsAny<IFormFile>(), "companies"), Times.Once);
        }

        // Kiểm tra KHÔNG upload/delete ảnh khi NewImageFile là null → giữ nguyên ảnh cũ
        [Fact]
        public async Task UpdateCompanyAsync_ShouldNotUploadImage_WhenNewImageFileIsNull()
        {
            var existingCompany = new Company { Id = 1, Name = "FPT", IsActive = true, ImageUrl = "https://old.png" };
            var request = new UpdateCompanyRequestModel { Name = "FPT", IsActive = true, NewImageFile = null };

            _mockCompanyRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingCompany);
            _mockCompanyRepo.Setup(r => r.UpdateAsync(It.IsAny<Company>())).Returns(Task.CompletedTask);

            await _service.UpdateCompanyAsync(1, request);

            _mockAwsS3Service.Verify(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
            _mockAwsS3Service.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
        }

        // Kiểm tra UpdatedAt được gán gần thời điểm hiện tại khi cập nhật
        [Fact]
        public async Task UpdateCompanyAsync_ShouldSetUpdatedAt()
        {
            var existingCompany = new Company { Id = 1, Name = "FPT", IsActive = true };
            var request = new UpdateCompanyRequestModel { Name = "FPT", IsActive = false, NewImageFile = null };

            _mockCompanyRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingCompany);
            _mockCompanyRepo.Setup(r => r.UpdateAsync(It.IsAny<Company>())).Returns(Task.CompletedTask);

            await _service.UpdateCompanyAsync(1, request);

            existingCompany.UpdatedAt.Should().NotBeNull();
            existingCompany.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        #endregion

        #region SetCompanyStatusAsync

        // Kiểm tra trả null khi company ID không tồn tại
        [Fact]
        public async Task SetCompanyStatusAsync_ShouldReturnNull_WhenCompanyNotFound()
        {
            _mockCompanyRepo.Setup(r => r.SetStatusAsync(It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync((Company?)null);

            var result = await _service.SetCompanyStatusAsync(999, true);

            result.Should().BeNull();
        }

        // Kiểm tra bật company: IsActive = true, Name đúng
        [Fact]
        public async Task SetCompanyStatusAsync_ShouldActivateCompany()
        {
            var company = new Company { Id = 1, Name = "FPT", IsActive = true, CreatedAt = DateTimeOffset.UtcNow };
            _mockCompanyRepo.Setup(r => r.SetStatusAsync(1, true)).ReturnsAsync(company);

            var result = await _service.SetCompanyStatusAsync(1, true);

            result.Should().NotBeNull();
            result!.IsActive.Should().BeTrue();
            result.Name.Should().Be("FPT");
        }

        // Kiểm tra tắt company: IsActive = false
        [Fact]
        public async Task SetCompanyStatusAsync_ShouldDeactivateCompany()
        {
            var company = new Company { Id = 1, Name = "FPT", IsActive = false, CreatedAt = DateTimeOffset.UtcNow };
            _mockCompanyRepo.Setup(r => r.SetStatusAsync(1, false)).ReturnsAsync(company);

            var result = await _service.SetCompanyStatusAsync(1, false);

            result.Should().NotBeNull();
            result!.IsActive.Should().BeFalse();
        }

        #endregion

        #region GetCompanyListAsync

        // Kiểm tra trả về kết quả phân trang đúng: 2 items, TotalCount, PageNumber, PageSize
        [Fact]
        public async Task GetCompanyListAsync_ShouldReturnPaginatedResult()
        {
            var request = new CompanyListRequestModel { PageNumber = 1, PageSize = 10 };
            var companies = new List<Company>
            {
                new Company { Id = 1, Name = "FPT", ImageUrl = "fpt.png", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
                new Company { Id = 2, Name = "VNG", ImageUrl = "vng.png", IsActive = true, CreatedAt = DateTimeOffset.UtcNow }
            };
            _mockCompanyRepo.Setup(r => r.GetPagedListAsync(request)).ReturnsAsync((companies.AsEnumerable(), 2));

            var result = await _service.GetCompanyListAsync(request);

            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        // Kiểm tra trả list rỗng khi không có company nào, TotalPages = 0
        [Fact]
        public async Task GetCompanyListAsync_ShouldReturnEmptyList_WhenNoCompanies()
        {
            var request = new CompanyListRequestModel { PageNumber = 1, PageSize = 10 };
            _mockCompanyRepo.Setup(r => r.GetPagedListAsync(request)).ReturnsAsync((Enumerable.Empty<Company>(), 0));

            var result = await _service.GetCompanyListAsync(request);

            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.TotalPages.Should().Be(0);
        }

        // Kiểm tra tính TotalPages chính xác: ceil(7/3) = 3
        [Fact]
        public async Task GetCompanyListAsync_ShouldCalculateTotalPages_Correctly()
        {
            var request = new CompanyListRequestModel { PageNumber = 1, PageSize = 3 };
            var companies = new List<Company>
            {
                new Company { Id = 1, Name = "A", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
                new Company { Id = 2, Name = "B", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
                new Company { Id = 3, Name = "C", IsActive = true, CreatedAt = DateTimeOffset.UtcNow }
            };
            _mockCompanyRepo.Setup(r => r.GetPagedListAsync(request)).ReturnsAsync((companies.AsEnumerable(), 7));

            var result = await _service.GetCompanyListAsync(request);

            result.TotalPages.Should().Be(3);
            result.TotalCount.Should().Be(7);
            result.Items.Should().HaveCount(3);
        }

        // Kiểm tra mapping đúng từng field: Id, Name, ImageUrl, IsActive, CreatedAt
        [Fact]
        public async Task GetCompanyListAsync_ShouldMapCompanyFieldsCorrectly()
        {
            var request = new CompanyListRequestModel { PageNumber = 1, PageSize = 10 };
            var createdAt = DateTimeOffset.UtcNow.AddDays(-10);
            var companies = new List<Company>
            {
                new Company { Id = 5, Name = "Shopee", ImageUrl = "shopee.png", IsActive = false, CreatedAt = createdAt }
            };
            _mockCompanyRepo.Setup(r => r.GetPagedListAsync(request)).ReturnsAsync((companies.AsEnumerable(), 1));

            var result = await _service.GetCompanyListAsync(request);

            var item = result.Items[0];
            item.Id.Should().Be(5);
            item.Name.Should().Be("Shopee");
            item.ImageUrl.Should().Be("shopee.png");
            item.IsActive.Should().BeFalse();
            item.CreatedAt.Should().Be(createdAt);
        }

        #endregion

        #region Helper Methods

        private static Mock<IFormFile> CreateMockFormFile(string fileName, long length)
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
            return mockFile;
        }

        #endregion
    }
}
