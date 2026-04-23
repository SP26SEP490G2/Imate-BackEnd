using MediatR;
using Microsoft.EntityFrameworkCore;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.Applications;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.DataAccess;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Comunity;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.Applications;
using Imate.API.Presentation.ResponseModels.Applications;
using Imate.API.Presentation.SignalR.Events.Applications;
using Imate.API.DataAccess.ApplicationDbContext;
using System.Text.Json;

namespace Imate.API.Business.Services.Applications
{
    public class ApplicationService : IApplicationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAwsS3StorageService _awsS3Service;
        private readonly ICommentRepository _commentRepository;
        private readonly IVoteRepository _voteRepository;
        private readonly ImateDbContext _context;
        private readonly IMediator _mediator;
        private readonly ISystemConfigService _systemConfigService;
        private readonly IAuditLogService _auditLogService;

        public ApplicationService(
            IUnitOfWork unitOfWork,
            IAwsS3StorageService awsS3Service,
            ICommentRepository commentRepository,
            IVoteRepository voteRepository,
            ImateDbContext context,
             IMediator mediator,
             ISystemConfigService systemConfigService,
             IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _awsS3Service = awsS3Service;
            _commentRepository = commentRepository;
            _voteRepository = voteRepository;
            _context = context;
            _mediator = mediator;
            _systemConfigService = systemConfigService;
            _auditLogService = auditLogService;
        }
        public async Task<PagedList<ApplicationListResponse>> GetApplicationsByIdAsync(int id, ApplicationParams appParams)
        {
            if (await _unitOfWork.Accounts.GetByIdAsync(id) == null)
            {
                throw new NotFoundException($"Không tìm thấy đơn với người dùng {id}.");
            }
            // Giả định _repo.GetAll() trả về IQueryable<Application>
            var query = _unitOfWork.Applications.GetAllApplications().Where(a => a.UserId == id);


            // --- 1. LOGIC TÌM KIẾM CHUNG (SEARCH) ---
            // Dùng SearchTerm (từ class cha)
            if (!string.IsNullOrWhiteSpace(appParams.SearchTerm))
            {
                var searchTerm = appParams.SearchTerm.ToLower().Trim();
                // Tìm searchTerm ở cả Name và Email
                query = query.Where(a =>
                    a.Content.ToLower().Contains(searchTerm) // Thêm content nếu muốn
                );
            }
            // --- 2. LOGIC LỌC (FILTER) ---
            // Dùng Status
            if (!string.IsNullOrWhiteSpace(appParams.Status))
            {
                if (Enum.TryParse<ApplicationStatus>(appParams.Status, true, out var parsedStatus))
                {
                    query = query.Where(a => a.Status == parsedStatus);
                }
            }
            // ReviewId
            if (appParams.ReviewId != null)
            {
                query = query.Where(a => a.ReviewerId == appParams.ReviewId);
            }
            // Type
            if (!string.IsNullOrWhiteSpace(appParams.Type))
            {
                if (Enum.TryParse<ApplicationType>(appParams.Type, true, out var parsedType))
                {
                    query = query.Where(a => a.ApplicationType == parsedType);
                }
            }
            // --- 3. LOGIC SẮP XẾP (SORT) ---
            // Dùng SortBy và SortOrder
            if (!string.IsNullOrWhiteSpace(appParams.SortBy))
            {
                bool isDescending = appParams.SortOrder?.ToLower() == "desc";

                query = appParams.SortBy.ToLower() switch
                {

                    "createdat" => isDescending
                        ? query.OrderByDescending(a => a.CreatedAt)
                        : query.OrderBy(a => a.CreatedAt),

                    // Sắp xếp mặc định
                    _ => throw new NotFoundException($"SortBy không hợp lệ : {appParams.SortBy}")
                };
            }
            else
            {
                // Sắp xếp mặc định nếu không truyền SortBy
                query = query.OrderByDescending(a => a.CreatedAt);
            }
            // --- 4. CHIẾU (PROJECT) SANG DTO (THAY ĐỔI CHÍNH) ---
            // Chuyển IQueryable<Application> thành IQueryable<ApplicationListResponse>
            var responseQuery = query.Select(a => new ApplicationListResponse
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content, // Dùng Title (hoặc Content tùy bạn)
                ResponseNote = a.Response,
                CreatedAt = DateOnly.FromDateTime(a.CreatedAt.DateTime),

                // Dùng helper để chuyển Enum sang chuỗi Tiếng Việt
                ApplicationType = a.ApplicationType.ToString(),
                Status = a.Status.ToString(),

                // Ánh xạ nested DTO, BẮT BUỘC kiểm tra Reviewer null
                Reviewer = (a.Reviewer == null) ? null : new ReviewerInfoResponse
                {
                    Id = a.Reviewer.Id,
                    FullName = a.Reviewer.FullName,
                    AvatarUrl = a.Reviewer.AvatarUrl // Giả định Account có AvatarUrl
                }
            });
            // --- 5. PHÂN TRANG (PAGING) ---
            // Trả về PagedList của DTO
            return await PagedList<ApplicationListResponse>.CreateAsync(
                responseQuery, // Dùng query đã được .Select()
                appParams.PageNumber,
                appParams.PageSize
            );
        }

        public async Task<ApplicationDetailResponse> CreateTechnicalApplicationAsync(CreateTechnicalApplicationRequest request, int userId)
        {
            // 1. Kiểm tra Account (Giống RegisterProfileAsync)
            var account = await _unitOfWork.Accounts.GetByIdAsync(userId);
            if (account == null)
            {
                throw new NotFoundException("Không tìm thấy người dùng.");
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                throw new BadRequestException("Mô tả lỗi không được để trống.");
            }

            // 3. Xử lý File Upload (Giống RegisterProfileAsync)
            // Thay vì 3 biến string?, chúng ta dùng 1 List<string>
            var evidenceUrls = new List<string>();

            if (request.EvidenceFiles != null && request.EvidenceFiles.Any())
            {
                foreach (var file in request.EvidenceFiles)
                {
                    if (file != null && file.Length > 0)
                    {
                        // Đặt tên folder là "applications" (giống như "avatars", "cvs" của bạn)
                        string url = await _awsS3Service.UploadFileAsync(file, "applications");
                        evidenceUrls.Add(url);
                    }
                }
            }

            // (Không có bước cập nhật Account như trong RegisterProfileAsync,
            // vì đơn kỹ thuật không cần cập nhật FullName hay AvatarUrl của user)

            // 4. Tạo Entity mới (Giống RegisterProfileAsync)
            var newApplication = new Application
            {
                UserId = userId, // Giống AccountId
                ApplicationType = ApplicationType.TechnicalError,
                Status = ApplicationStatus.Pending, // Mặc định

                Title = request.Title ?? $"Báo cáo lỗi kỹ thuật - {DateTime.UtcNow:yyyy-MM-dd}",
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,

                // Gán các URL đã upload (giống CvUrl, CertificateUrl)
                EvidenceUrls = JsonSerializer.Serialize(evidenceUrls),

                Response = string.Empty,
                ReviewerId = null
            };


            // 5. Thêm vào Unit of Work (Giống RegisterProfileAsync)
            _unitOfWork.Applications.AddApplication(newApplication); // Giả định bạn có Applications repo

            // 6. Lưu thay đổi (Giống RegisterProfileAsync)
            await _unitOfWork.SaveChangesAsync(); // Hoặc _unitOfWork.CompleteAsync() tùy bạn đặt tên
            await _mediator.Publish(new TechnicalApplicationCreateEvent(newApplication));
            // 7. Trả về DTO (Cải tiến so với void của RegisterProfileAsync)
            var urls = string.IsNullOrEmpty(newApplication.EvidenceUrls)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(newApplication.EvidenceUrls);
            return new ApplicationDetailResponse
            {
                Id = newApplication.Id,
                Content = newApplication.Content,
                DateSent = DateOnly.FromDateTime(newApplication.CreatedAt.DateTime),
                Status = newApplication.Status.ToString(),
                Attachments = urls

            };
        }

        public async Task<ApplicationDetailResponse> CreateReportApplicationAsync(CreateReportApplicationRequest request, int userId)
        {
            // 1. Kiểm tra Account (Giống RegisterProfileAsync)
            var account = await _unitOfWork.Accounts.GetByIdAsync(userId);
            if (account == null)
            {
                throw new NotFoundException("Không tìm thấy tài khoản.");
            }
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                throw new BadRequestException("Mô tả lỗi không được để trống.");
            }
            if (await _unitOfWork.Bookings.GetBookingByIdAsync(request.BookingId) == null)
            {
                throw new NotFoundException("Không tìm thấy booking tương ứng.");
            }

            // 3. Xử lý File Upload (Giống RegisterProfileAsync)
            // Thay vì 3 biến string?, chúng ta dùng 1 List<string>
            var evidenceUrls = new List<string>();

            if (request.EvidenceFiles != null && request.EvidenceFiles.Any())
            {
                foreach (var file in request.EvidenceFiles)
                {
                    if (file != null && file.Length > 0)
                    {
                        // Đặt tên folder là "applications" (giống như "avatars", "cvs" của bạn)
                        string url = await _awsS3Service.UploadFileAsync(file, "applications");
                        evidenceUrls.Add(url);
                    }
                }
            }

            // (Không có bước cập nhật Account như trong RegisterProfileAsync,
            // vì đơn kỹ thuật không cần cập nhật FullName hay AvatarUrl của user)

            // 4. Tạo Entity mới (Giống RegisterProfileAsync)
            var type = ApplicationType.ReportMentor;
            var title = request.Title ?? $"Đơn tố cáo mentor - {DateTime.UtcNow:yyyy-MM-dd}";
            if (account.AccountRoles.Any(a => a.Role.Name == RoleName.Mentor))
            {
                type = ApplicationType.ReportRating;
                title = request.Title ?? $"Đơn tố cáo rating - {DateTime.UtcNow:yyyy-MM-dd}";
            }

            // 4.0. Không cho gửi nếu đã có đơn đang chờ/đang xử lý cho cùng booking
            var existingPendingApplication = await _unitOfWork.Applications.GetAllApplications()
                .Where(a => a.UserId == userId
                    && a.BookingId == request.BookingId
                    && a.ApplicationType == type
                    && (a.Status == ApplicationStatus.Pending || a.Status == ApplicationStatus.InReview))
                .FirstOrDefaultAsync();

            if (existingPendingApplication != null)
            {
                throw new BadRequestException("Bạn đã gửi đơn báo cáo cho booking này và đơn đang được xử lý. Vui lòng chờ kết quả.");
            }

            // 4.1. Kiểm tra xem đã có đơn report cho booking này đã được xử lý chưa
            var existingProcessedApplication = await _unitOfWork.Applications.GetAllApplications()
                .Where(a => a.UserId == userId
                    && a.BookingId == request.BookingId
                    && a.ApplicationType == type
                    && (a.Status == ApplicationStatus.Approved || a.Status == ApplicationStatus.Rejected))
                .FirstOrDefaultAsync();

            if (existingProcessedApplication != null)
            {
                var statusText = existingProcessedApplication.Status == ApplicationStatus.Approved ? "đã được duyệt" : "đã bị từ chối";
                throw new BadRequestException($"Bạn đã gửi đơn báo cáo cho booking này và đơn đó {statusText}. Không thể gửi lại.");
            }

            var newApplication = new Application
            {
                UserId = userId, // Giống AccountId
                BookingId = request.BookingId,
                ApplicationType = type,
                Status = ApplicationStatus.Pending, // Mặc định

                Title = title,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,

                // Gán các URL đã upload (giống CvUrl, CertificateUrl)
                EvidenceUrls = JsonSerializer.Serialize(evidenceUrls),

                Response = string.Empty,
                ReviewerId = null,

            };


            // 5. Thêm vào Unit of Work (Giống RegisterProfileAsync)
            _unitOfWork.Applications.AddApplication(newApplication); // Giả định bạn có Applications repo

            // 6. Lưu thay đổi (Giống RegisterProfileAsync)
            await _unitOfWork.SaveChangesAsync(); // Hoặc _unitOfWork.CompleteAsync() tùy bạn đặt tên
            await _mediator.Publish(new ReportApplicationCreateEvent(newApplication));

            // 7. Trả về DTO (Cải tiến so với void của RegisterProfileAsync)
            var urls = string.IsNullOrEmpty(newApplication.EvidenceUrls)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(newApplication.EvidenceUrls);
            return new ApplicationDetailResponse
            {
                Id = newApplication.Id,
                Content = newApplication.Content,
                DateSent = DateOnly.FromDateTime(newApplication.CreatedAt.DateTime),
                Status = newApplication.Status.ToString(),
                Attachments = urls

            };
        }

        public async Task<PagedList<object>> GetAllApplicationsAsync(Application2Params appParams)
        {
            // Giả định _repo.GetAll() trả về IQueryable<Application>
            var query = _unitOfWork.Applications.GetAllApplications();


            // --- 1. LOGIC TÌM KIẾM CHUNG (SEARCH) ---
            // (Giữ nguyên logic cũ)
            if (!string.IsNullOrWhiteSpace(appParams.SearchTerm))
            {
                var searchTerm = appParams.SearchTerm.ToLower().Trim();
                query = query.Where(a =>
                    a.Content.ToLower().Contains(searchTerm)
                );
            }

            // --- 2. LOGIC LỌC (FILTER) ---
            // (Giữ nguyên logic cũ)
            if (!string.IsNullOrWhiteSpace(appParams.Status))
            {
                if (Enum.TryParse<ApplicationStatus>(appParams.Status, true, out var parsedStatus))
                {
                    query = query.Where(a => a.Status == parsedStatus);
                }
            }
            if (!string.IsNullOrWhiteSpace(appParams.Type))
            {
                if (Enum.TryParse<ApplicationType>(appParams.Type, true, out var parsedType))
                {
                    query = query.Where(a => a.ApplicationType == parsedType);
                }
            }
            if (appParams.UserId != null)
            {
                query = query.Where(a => a.UserId == appParams.UserId);
            }

            // --- 3. LOGIC SẮP XẾP (SORT) ---
            // (Giữ nguyên logic cũ)
            if (!string.IsNullOrWhiteSpace(appParams.SortBy))
            {
                bool isDescending = appParams.SortOrder?.ToLower() == "desc";

                query = appParams.SortBy.ToLower() switch
                {
                    "createdat" => isDescending
                        ? query.OrderByDescending(a => a.CreatedAt)
                        : query.OrderBy(a => a.CreatedAt),
                    _ => throw new NotFoundException($"SortBy không hợp lệ: {appParams.SortBy}")
                };
            }
            else
            {
                query = query.OrderByDescending(a => a.CreatedAt);
            }

            // --- 4. CHIẾU (PROJECT) SANG OBJECT ANONYMOUS (THAY ĐỔI CHÍNH) ---
            // Chuyển IQueryable<Application> thành IQueryable<object> (kiểu ẩn danh)
            var responseQuery = query.Select(a => new
            {
                Id = a.Id,
                UserId = a.UserId,

                // Lấy thông tin từ Reviewer (BẮT BUỘC kiểm tra null)
                // Giả định 'a.Reviewer' (Account) có các thuộc tính 'AvatarUrl', 'Email', 'FullName'
                AvatarUrl = a.User.AvatarUrl,
                Email = a.User.Email,
                FullName = a.User.FullName,

                // Lấy thông tin từ Application
                Status = a.Status.ToString(),
                ApplicationType = a.ApplicationType.ToString(),
                CreatedAt = DateOnly.FromDateTime(a.CreatedAt.DateTime),
                UpdatedAt = a.UpdatedAt, // Giả định 'a.Application' có 'UpdatedAt'
                Title = a.Title,
                Content = a.Content,
                CommentId = a.CommentId,

                // Lấy thông tin comment nếu có
                CommentContent = a.Comment != null ? a.Comment.Content : null,
                CommentUserId = a.Comment != null ? a.Comment.UserId : (int?)null,
                CommentUserName = a.Comment != null ? a.Comment.User.FullName : null
            });

            // --- 5. PHÂN TRANG (PAGING) ---
            // THAY ĐỔI 2: Trả về PagedList<object>
            return await PagedList<object>.CreateAsync(
                responseQuery, // Dùng query đã được .Select()
                appParams.PageNumber,
                appParams.PageSize
            );
        }
        public async Task<object> GetApplicationDetails(int applicationId)
        {
            // 1. Lấy IQueryable (giả định phương thức này trả về IQueryable<Application>
            // và đã bao gồm (Include) thông tin Reviewer)
            var query = _unitOfWork.Applications.GetAllApplications();

            // 2. Lọc theo ID và Chiếu (Project) sang anonymous object
            // Dùng .Select() trước .FirstOrDefaultAsync() để tối ưu hóa SQL
            var applicationDetails = await query
                .Where(a => a.Id == applicationId)
                .Select(a => new
                {
                    // Các trường từ Application
                    Id = a.Id,
                    UserId = a.UserId,
                    UserName = a.User.FullName,
                    UserAvatarUrl = a.User.AvatarUrl,
                    ApplicationType = a.ApplicationType,
                    Status = a.Status,
                    Title = a.Title,
                    Content = a.Content,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    CommentId = a.CommentId,

                    // Lấy thông tin comment nếu có
                    CommentContent = a.Comment != null ? a.Comment.Content : null,
                    CommentUserId = a.Comment != null ? a.Comment.UserId : (int?)null,
                    CommentUserName = a.Comment != null ? a.Comment.User.FullName : null,

                    // Lấy Email từ Reviewer
                    Email = a.User.Email,
                    EvidenceUrls = a.EvidenceUrls,
                    Response = a.Response,
                    ReviewerId = a.ReviewerId,
                    ReviewerName = a.Reviewer != null ? a.Reviewer.FullName : null
                })
                .FirstOrDefaultAsync(); // Lấy duy nhất 1 object

            // 3. Kiểm tra nếu không tìm thấy
            if (applicationDetails == null)
            {
                // Giả định bạn có một exception tùy chỉnh tên là NotFoundException
                throw new NotFoundException($"Đơn với ID {applicationId} không tìm thấy.");
            }

            // 4. Trả về kết quả (đã là object ẩn danh)
            return applicationDetails;
        }
        public async Task<object> GetReportRatingDetails(int applicationId)
        {
            // 1. Lấy IQueryable (giả định đã Include Reviewer và Comment.User)
            var query = _unitOfWork.Applications.GetAllApplications();

            // 2. Lọc theo ID và Chiếu sang anonymous object
            var reportDetails = await query
                .Where(a => a.Id == applicationId && a.ApplicationType == ApplicationType.ReportRating)
                .Select(a => new
                {
                    Id = a.Id,
                    UserId = a.UserId,

                    // Lấy thông tin từ Reviewer (BẮT BUỘC kiểm tra null)
                    // Giả định 'a.Reviewer' (Account) có các thuộc tính 'AvatarUrl', 'Email', 'FullName'
                    AvatarUrl = a.User.AvatarUrl,
                    Email = a.User.Email,
                    FullName = a.User.FullName,

                    // Lấy thông tin từ Application
                    Status = a.Status.ToString(),
                    ApplicationType = a.ApplicationType,
                    CreatedAt = DateOnly.FromDateTime(a.CreatedAt.DateTime),
                    UpdatedAt = a.UpdatedAt, // Giả định 'a.Application' có 'UpdatedAt'
                    Title = a.Title,
                    Content = a.Content,
                    Response = a.Response,
                    ReviewerName = a.Reviewer != null ? a.Reviewer.FullName : null,
                    EvidenceUrls = a.EvidenceUrls, // Link video, GitHub...     
                    RatingDetails = a.Booking == null ? null : new
                    {
                        a.Booking.RatingScore,
                        a.Booking.ReviewText,
                        a.Booking.RatingCreatedAt,
                        MentorDetails = new
                        {
                            Id = a.Booking.Mentor.AccountId,
                            FullName = a.Booking.Mentor.Account.FullName,
                            Email = a.Booking.Mentor.Account.Email,
                            AvatarUrl = a.Booking.Mentor.Account.AvatarUrl,

                        }
                    }
                })
                .FirstOrDefaultAsync();

            // 3. Kiểm tra nếu không tìm thấy
            if (reportDetails == null)
            {
                throw new NotFoundException($"Không tìm thấy đơn tố cáo Rating {applicationId}.");
            }

            // 4. Trả về kết quả
            return reportDetails;
        }

        public async Task<ReportCommentDetailResponse> GetReportCommentDetails(int applicationId)
        {
            var application = await _unitOfWork.Applications.GetAllApplications()
                .Where(a => a.Id == applicationId && a.ApplicationType == ApplicationType.ReportComment)
                .FirstOrDefaultAsync();

            if (application == null)
                throw new NotFoundException($"Không tìm thấy đơn tố cáo comment với ID {applicationId}.");

            var response = new ReportCommentDetailResponse
            {
                Id = application.Id,
                Title = application.Title,
                Content = application.Content,
                Status = application.Status.ToString(),
                ApplicationType = application.ApplicationType.ToString(),
                EvidenceUrls = application.EvidenceUrls,
                Response = application.Response,
                CreatedAt = DateOnly.FromDateTime(application.CreatedAt.DateTime),
                UpdatedAt = application.UpdatedAt?.DateTime,
                CommentId = application.CommentId,
                ReviewerId = application.ReviewerId,
                ReviewerName = application.Reviewer?.FullName,
                Reporter = new ReportCommentUserInfo
                {
                    Id = application.User.Id,
                    FullName = application.User.FullName,
                    Email = application.User.Email,
                    AvatarUrl = application.User.AvatarUrl,
                },
            };

            if (application.CommentId == null)
                return response;

            var comment = await _unitOfWork.Comments.GetCommentWithDetailsByIdAsync(application.CommentId.Value);
            if (comment == null)
                return response;

            response.CommentDetail = new ReportCommentDetail
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt.DateTime,
                UpdatedAt = comment.UpdatedAt?.DateTime,
                Author = new ReportCommentUserInfo
                {
                    Id = comment.User.Id,
                    FullName = comment.User.FullName,
                    Email = comment.User.Email,
                    AvatarUrl = comment.User.AvatarUrl,
                },
                Question = comment.Question == null ? null : new ReportCommentQuestionInfo
                {
                    Id = comment.Question.Id,
                    Content = comment.Question.Content,
                    CreatedByUser = new ReportCommentUserInfo
                    {
                        Id = comment.Question.CreatorId,
                        FullName = comment.Question.Creator.FullName,
                        AvatarUrl = comment.Question.Creator.AvatarUrl,
                    }
                }
            };

            return response;
        }

        public async Task<object> GetReportMentorDetails(int applicationId)
        {
            // 1. Lấy IQueryable (giả định đã Include Reviewer)
            var query = _unitOfWork.Applications.GetAllApplications();

            // 2. Lọc theo ID và Chiếu sang anonymous object
            // Chúng ta chiếu vào một object lồng nhau để thể hiện rõ đây là chi tiết của Mentor
            var mentorProjection = await query
                .Where(a => a.Id == applicationId && a.ApplicationType == ApplicationType.ReportMentor)
                .Select(a => new
                {
                    Id = a.Id,
                    UserId = a.UserId,

                    // Lấy thông tin từ Reviewer (BẮT BUỘC kiểm tra null)
                    // Giả định 'a.Reviewer' (Account) có các thuộc tính 'AvatarUrl', 'Email', 'FullName'
                    AvatarUrl = a.User.AvatarUrl,
                    Email = a.User.Email,
                    FullName = a.User.FullName,

                    // Lấy thông tin từ Application
                    Status = a.Status.ToString(),
                    ApplicationType = a.ApplicationType,
                    CreatedAt = DateOnly.FromDateTime(a.CreatedAt.DateTime),
                    UpdatedAt = a.UpdatedAt, // Giả định 'a.Application' có 'UpdatedAt'
                    Title = a.Title,
                    Content = a.Content,
                    Response = a.Response,
                    ReviewerName = a.Reviewer != null ? a.Reviewer.FullName : null,
                    EvidenceUrls = a.EvidenceUrls, // Link video, GitHub...     
                    // Tạo một object con cho thông tin mentor
                    // 1. Tạo object con cho Booking và chỉ lấy các trường cần thiết
                    BookingDetails = a.Booking == null ? null : new
                    {
                        // Thay thế bằng các tên trường thực tế trong Booking của bạn
                        BookingId = a.Booking.Id,
                        Price = a.Booking.PriceAtBooking,
                        a.Booking.BookDate,
                        StarTime = a.Booking.StartTime,
                        MentorDetails = new
                        {
                            Id = a.Booking.Mentor.AccountId,
                            FullName = a.Booking.Mentor.Account.FullName,
                            Email = a.Booking.Mentor.Account.Email,
                            AvatarUrl = a.Booking.Mentor.Account.AvatarUrl,

                        }
                    },

                })
                .FirstOrDefaultAsync();

            // 3. Kiểm tra nếu không tìm thấy (ApplicationId không tồn tại)
            if (mentorProjection == null)
            {
                throw new NotFoundException($"Không tìm thấy đơn tố cáo mentor {applicationId}.");
            }

            // 4. Trả về kết quả
            // mentorProjection.MentorDetails có thể là null nếu đơn tồn tại
            // nhưng chưa được gán mentor, điều này là hợp lệ.
            return mentorProjection;
        }
        public async Task<object> GetTechnicalDetails(int applicationId)
        {
            // 1. Lấy IQueryable
            var query = _unitOfWork.Applications.GetAllApplications();

            // 2. Lọc theo ID và Chiếu sang anonymous object
            var techDetails = await query
                .Where(a => a.Id == applicationId && a.ApplicationType == ApplicationType.TechnicalError)
                .Select(a => new
                {
                    Id = a.Id,
                    UserId = a.UserId,

                    // Lấy thông tin từ Reviewer (BẮT BUỘC kiểm tra null)
                    // Giả định 'a.Reviewer' (Account) có các thuộc tính 'AvatarUrl', 'Email', 'FullName'
                    AvatarUrl = a.User.AvatarUrl,
                    Email = a.User.Email,
                    FullName = a.User.FullName,

                    // Lấy thông tin từ Application
                    Status = a.Status.ToString(),
                    ApplicationType = a.ApplicationType.ToString(),
                    CreatedAt = DateOnly.FromDateTime(a.CreatedAt.DateTime),
                    UpdatedAt = a.UpdatedAt, // Giả định 'a.Application' có 'UpdatedAt'
                    Title = a.Title,
                    Content = a.Content,
                    Response = a.Response,
                    ReviewerName = a.Reviewer != null ? a.Reviewer.FullName : null,
                    EvidenceUrls = a.EvidenceUrls, // Link video, GitHub...               

                })
                .FirstOrDefaultAsync();

            // 3. Kiểm tra nếu không tìm thấy
            if (techDetails == null)
            {
                throw new NotFoundException($"Không tìm thấy đơn lỗi kĩ thuật {applicationId}.");
            }

            // 4. Trả về kết quả
            return techDetails;
        }

        public async Task<ApplicationDetailResponse> CreateReportCommentApplicationAsync(CreateReportCommentRequest request, int userId)
        {
            // 1. Kiểm tra Account
            var account = await _unitOfWork.Accounts.GetByIdAsync(userId);
            if (account == null)
            {
                throw new NotFoundException("Không tìm thấy người dùng.");
            }

            // 2. Kiểm tra Comment tồn tại
            var comment = await _commentRepository.GetCommentByIdAsync(request.CommentId);
            if (comment == null)
            {
                throw new NotFoundException("Không tìm thấy comment.");
            }

            // 3. Kiểm tra người dùng không được report comment của chính mình
            if (comment.UserId == userId)
            {
                throw new BadRequestException("Bạn không thể report comment của chính mình.");
            }

            // 4. Xử lý File Upload (nếu có)
            var evidenceUrls = new List<string>();
            if (request.EvidenceFiles != null && request.EvidenceFiles.Any())
            {
                foreach (var file in request.EvidenceFiles)
                {
                    if (file != null && file.Length > 0)
                    {
                        string url = await _awsS3Service.UploadFileAsync(file, "applications");
                        evidenceUrls.Add(url);
                    }
                }
            }

            // 5. Tạo content từ reason và additional details
            var reasonText = GetReportReasonString(request.Reason);
            var content = $"{reasonText}";
            if (!string.IsNullOrWhiteSpace(request.AdditionalDetails))
            {
                content = $"Khác: {request.AdditionalDetails}";
            }

            // 6. Tạo Application mới
            var newApplication = new Application
            {
                UserId = userId,
                CommentId = request.CommentId,
                ApplicationType = ApplicationType.ReportComment,
                Status = ApplicationStatus.Pending,
                Title = $"Đơn tố cáo comment - {DateTime.UtcNow:yyyy-MM-dd}",
                Content = content,
                CreatedAt = DateTime.UtcNow,
                EvidenceUrls = JsonSerializer.Serialize(evidenceUrls),
                Response = string.Empty,
                ReviewerId = null
            };

            // 7. Lưu Application
            _unitOfWork.Applications.AddApplication(newApplication);
            await _unitOfWork.SaveChangesAsync();
            await _mediator.Publish(new ReportCommentApplicationCreateEvent(newApplication));
            // 8. Trả về DTO
            var urls = string.IsNullOrEmpty(newApplication.EvidenceUrls)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(newApplication.EvidenceUrls);
            return new ApplicationDetailResponse
            {
                Id = newApplication.Id,
                Content = newApplication.Content,
                DateSent = DateOnly.FromDateTime(newApplication.CreatedAt.DateTime),
                Status = newApplication.Status.ToString(),
                Attachments = urls
            };
        }

        public async Task ApproveApplicationAsync(int applicationId, int reviewerId, string? responseNote = null)
        {
            var application = await _unitOfWork.Applications.GetAllApplications()
                .FirstOrDefaultAsync(a => a.Id == applicationId);
            if (application == null)
            {
                throw new NotFoundException($"Không tìm thấy đơn với ID {applicationId}.");
            }

            if (application.Status != ApplicationStatus.Pending && application.Status != ApplicationStatus.InReview)
            {
                throw new BadRequestException("Đơn này đã được xử lý.");
            }

            // Get old value before update
            var oldValue = new { 
                Status = application.Status.ToString(), 
                ApplicationType = application.ApplicationType.ToString(),
                Response = application.Response
            };

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Xử lý theo loại application
                    if (application.ApplicationType == ApplicationType.ReportComment && application.CommentId.HasValue)
                    {
                        var commentId = application.CommentId.Value;

                        // Xóa comment và tất cả votes liên quan
                        var comment = await _commentRepository.GetCommentByIdAsync(commentId);
                        if (comment != null)
                        {
                            // Xóa tất cả votes liên quan trước
                            var relatedVotes = await _voteRepository.GetVotesByCommentIdAsync(commentId);
                            if (relatedVotes.Count > 0)
                            {
                                _context.Set<Imate.API.Models.Entities.Vote>().RemoveRange(relatedVotes);
                                await _voteRepository.SaveChangesAsync();
                            }

                            // Tìm và set CommentId = null cho TẤT CẢ Application tham chiếu đến Comment này
                            // để tránh foreign key constraint violation
                            var allApplicationsWithComment = await _context.Set<Application>()
                                .Where(a => a.CommentId == commentId)
                                .ToListAsync();

                            foreach (var relatedApp in allApplicationsWithComment)
                            {
                                if (relatedApp.Status == ApplicationStatus.Pending || relatedApp.Status == ApplicationStatus.InReview)
                                {
                                    relatedApp.Status = ApplicationStatus.Approved;
                                    relatedApp.ReviewerId = reviewerId;
                                    relatedApp.Response = $"Tự động duyệt: bình luận đã bị xóa";
                                    relatedApp.UpdatedAt = DateTime.UtcNow;
                                }
                            }

                            foreach (var app in allApplicationsWithComment)
                            {
                                app.CommentId = null;
                            }

                            // Save changes để remove tất cả foreign key references
                            await _unitOfWork.SaveChangesAsync();

                            // Sau đó mới xóa Comment
                            await _commentRepository.DeleteCommentAsync(comment);
                            await _commentRepository.SaveChangesAsync();
                        }
                    }
                    else if (application.ApplicationType == ApplicationType.ReportMentor && application.BookingId.HasValue)
                    {
                        // Xử lý report mentor: refund cho user và penalty cho mentor
                        var bookingId = application.BookingId.Value;

                        // 1. Lấy booking
                        var booking = await _unitOfWork.Bookings.GetBookingByIdAsync(bookingId);
                        if (booking == null)
                        {
                            throw new NotFoundException($"Không tìm thấy booking với ID {bookingId}.");
                        }

                        // 2. Lấy escrow transaction của booking
                        var escrowTransaction = await _unitOfWork.Transactions.GetBookingTransactionAsync(bookingId);
                        if (escrowTransaction == null)
                        {
                            throw new NotFoundException($"Không tìm thấy giao dịch escrow cho booking {bookingId}.");
                        }

                        // 3. Kiểm tra transaction có phải escrow không
                        if (escrowTransaction.Status != TransactionStatus.Escrow)
                        {
                            throw new BadRequestException("Giao dịch này không ở trạng thái escrow hoặc đã được xử lý.");
                        }

                        // 4. Đảm bảo buổi học đã kết thúc (tránh duyệt trước khi hoàn thành)
                        // Buổi học kéo dài 1 giờ, nên thời gian kết thúc = StartTime + 1 giờ
                        var bookingEndTime = booking.StartTime.AddHours(1);
                        var now = DateTime.UtcNow;
                        var hoursSinceBookingEnd = (now - bookingEndTime).TotalHours;

                        if (hoursSinceBookingEnd < 0)
                        {
                            throw new BadRequestException("Không thể duyệt report trước khi buổi học kết thúc.");
                        }

                        // 5. Lấy candidate account (người report)
                        var candidateAccount = await _unitOfWork.Accounts.GetByIdAsync(booking.CandidateId);
                        if (candidateAccount == null)
                        {
                            throw new NotFoundException("Không tìm thấy tài khoản candidate.");
                        }

                        // 6. Lấy mentor account
                        var mentor = await _unitOfWork.Mentors.GetMentorByIdAsync(booking.MentorId);
                        if (mentor == null)
                        {
                            throw new NotFoundException("Không tìm thấy mentor.");
                        }

                        var mentorAccount = await _unitOfWork.Accounts.GetByIdAsync(mentor.AccountId);
                        if (mentorAccount == null)
                        {
                            throw new NotFoundException("Không tìm thấy tài khoản mentor.");
                        }

                        // 7. Tính toán commission rate (từ config)
                        decimal commissionRate = await _systemConfigService.GetCommissionRateAsync();
                        if (escrowTransaction.CommissionRateApplied.HasValue)
                        {
                            commissionRate = escrowTransaction.CommissionRateApplied.Value;
                        }

                        var bookingAmount = escrowTransaction.Amount;
                        var penaltyAmount = (int)(bookingAmount * commissionRate / 100); // 10% của booking amount

                        // 8. Refund 100% cho candidate
                        candidateAccount.Balance += bookingAmount;
                        await _unitOfWork.Accounts.UpdateAsync(candidateAccount);

                        // 9. Trừ penalty từ mentor (nếu mentor có đủ balance)
                        if (mentorAccount.Balance < penaltyAmount)
                        {
                            // Nếu mentor không đủ balance, trừ hết số dư hiện có
                            penaltyAmount = mentorAccount.Balance;
                        }
                        mentorAccount.Balance -= penaltyAmount;
                        await _unitOfWork.Accounts.UpdateAsync(mentorAccount);

                        // 10. Tạo transaction refund cho candidate
                        var refundTransaction = new Imate.API.Models.Entities.Transaction
                        {
                            SourceAccountId = null, // System refund
                            TargetAccountId = candidateAccount.Id,
                            TransactionType = TransactionType.Refund,
                            Amount = bookingAmount,
                            BookingId = bookingId,
                            Status = TransactionStatus.Completed,
                            Reason = responseNote ?? $"Hoàn tiền 100% do report mentor được duyệt (Booking #{bookingId})",
                            ReviewerId = reviewerId,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Transactions.AddAsync(refundTransaction);
                        await _unitOfWork.SaveChangesAsync(); // Lưu để lấy ID
                        
                        // Set ExternalTransactionCode nếu chưa có (entity đã được track, không cần UpdateAsync)
                        refundTransaction.EnsureExternalTransactionCode();
                        if (refundTransaction.ExternalTransactionCode != null)
                        {
                            await _unitOfWork.SaveChangesAsync(); // Lưu ExternalTransactionCode
                        }

                        // 11. Tạo transaction penalty cho mentor
                        var penaltyTransaction = new Imate.API.Models.Entities.Transaction
                        {
                            SourceAccountId = mentorAccount.Id,
                            TargetAccountId = null, // System receives penalty
                            TransactionType = TransactionType.Penalty,
                            Amount = penaltyAmount,
                            BookingId = bookingId,
                            Status = TransactionStatus.Completed,
                            CommissionRateApplied = commissionRate,
                            Reason = responseNote ?? $"Phí phạt {commissionRate}% do report được duyệt (Booking #{bookingId})",
                            ReviewerId = reviewerId,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Transactions.AddAsync(penaltyTransaction);
                        await _unitOfWork.SaveChangesAsync(); // Lưu để lấy ID
                        
                        // Set ExternalTransactionCode nếu chưa có (entity đã được track, không cần UpdateAsync)
                        penaltyTransaction.EnsureExternalTransactionCode();
                        if (penaltyTransaction.ExternalTransactionCode != null)
                        {
                            await _unitOfWork.SaveChangesAsync(); // Lưu ExternalTransactionCode
                        }

                        // 12. Update escrow transaction status (không còn escrow nữa)
                        escrowTransaction.Status = TransactionStatus.Completed;
                        escrowTransaction.ReviewerId = reviewerId;
                        escrowTransaction.Reason = responseNote ?? $"Đã xử lý report mentor - Refund và penalty đã được thực hiện";
                        escrowTransaction.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.Transactions.UpdateAsync(escrowTransaction);
                    }

                    else if (application.ApplicationType == ApplicationType.ReportRating && application.BookingId.HasValue)
                    {
                        var bookingId = application.BookingId.Value;

                        // Lấy booking có chứa rating
                        var booking = await _unitOfWork.Bookings.GetBookingByIdAsync(bookingId);
                        if (booking == null)
                            throw new NotFoundException($"Không tìm thấy booking với ID {bookingId}.");

                        // Kiểm tra có rating hay không
                        if (booking.RatingScore == null && string.IsNullOrEmpty(booking.ReviewText))
                            throw new BadRequestException("Booking này không có rating hoặc rating đã bị xóa.");

                        // Xóa rating
                        booking.RatingScore = null;
                        booking.ReviewText = null;

                        await _unitOfWork.Bookings.UpdateAsync(booking);

                        // (Optional) trừ điểm hoặc xử lý user vi phạm – nếu có chính sách
                    }

                    // Cập nhật trạng thái application
                    application.Status = ApplicationStatus.Approved;
                    application.ReviewerId = reviewerId;
                    application.Response = responseNote ?? string.Empty;
                    application.UpdatedAt = DateTime.UtcNow;

                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    // Create audit log
                    var newValue = new { 
                        Status = application.Status.ToString(), 
                        ApplicationType = application.ApplicationType.ToString(),
                        Response = application.Response,
                        ReviewerId = reviewerId
                    };
                    await _auditLogService.CreateAuditLogAsync(
                        reviewerId,
                        AuditAction.ApproveApplication,
                        "Application",
                        applicationId,
                        oldValue,
                        newValue
                    );
                    
                    await _mediator.Publish(new ApplicationApprovedEvent(application));
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task RejectApplicationAsync(int applicationId, int reviewerId, string? responseNote = null)
        {
            var application = await _unitOfWork.Applications.GetAllApplications()
                .FirstOrDefaultAsync(a => a.Id == applicationId);
            if (application == null)
            {
                throw new NotFoundException($"Không tìm thấy đơn với ID {applicationId}.");
            }

            if (application.Status != ApplicationStatus.Pending && application.Status != ApplicationStatus.InReview)
            {
                throw new BadRequestException("Đơn này đã được xử lý.");
            }

            // Get old value before update
            var oldValue = new { 
                Status = application.Status.ToString(), 
                ApplicationType = application.ApplicationType.ToString(),
                Response = application.Response
            };

            // Chỉ cập nhật trạng thái, không xóa comment
            application.Status = ApplicationStatus.Rejected;
            application.ReviewerId = reviewerId;
            application.Response = responseNote ?? string.Empty;
            application.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            
            // Create audit log
            var newValue = new { 
                Status = application.Status.ToString(), 
                ApplicationType = application.ApplicationType.ToString(),
                Response = application.Response,
                ReviewerId = reviewerId
            };
            await _auditLogService.CreateAuditLogAsync(
                reviewerId,
                AuditAction.RejectApplication,
                "Application",
                applicationId,
                oldValue,
                newValue
            );
            
            // THÊM: Phát sự kiện "Đã từ chối"
            // MediatR sẽ tự động tìm ApplicationRejectedHandler để xử lý
            await _mediator.Publish(new ApplicationRejectedEvent(application));
        }
        public async Task<IEnumerable<ApplicationNeedProcessSummaryResponse>> GetPendingSummaryAsync()
        {
            var reportTypes = new List<ApplicationType>
                {
                    ApplicationType.TechnicalError,
                    ApplicationType.ReportMentor,
                    ApplicationType.ReportRating,
                    ApplicationType.ReportComment
                };
            var summaries = await _unitOfWork.Applications
                .GetAllApplications()
                .Where(a => a.Status == ApplicationStatus.Pending)
                .GroupBy(a => a.ApplicationType)
                .Select(g => new ApplicationNeedProcessSummaryResponse
                {
                    Type = g.Key.ToString(),
                    TotalNeedProcess = g.Count()
                })
                .ToListAsync();


            foreach (var type in reportTypes)
            {
                if (!summaries.Any(s => s.Type == type.ToString()))
                {
                    summaries.Add(new ApplicationNeedProcessSummaryResponse
                    {
                        Type = type.ToString(),
                        TotalNeedProcess = 0
                    });
                }
            }

            return summaries.ToList();
        }

        private static string GetReportReasonString(ReportReason reason)
        {
            return reason switch
            {
                ReportReason.Spam => "Spam hoặc quảng cáo",
                ReportReason.Harassment => "Quấy rối, lăng mạ",
                ReportReason.InappropriateContent => "Nội dung không phù hợp",
                ReportReason.HateSpeech => "Ngôn từ thù địch",
                ReportReason.FalseInformation => "Thông tin sai lệch",
                ReportReason.OffTopic => "Lạc đề, không liên quan",
                ReportReason.CopyrightViolation => "Vi phạm bản quyền",
                ReportReason.PersonalAttack => "Công kích cá nhân",
                ReportReason.InappropriateLanguage => "Ngôn ngữ không phù hợp",
                ReportReason.Other => "Lý do khác",
                _ => reason.ToString()
            };
        }
    }
}
