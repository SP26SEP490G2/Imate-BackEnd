using Microsoft.EntityFrameworkCore;

using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Business.Interfaces.UserManagement;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.Business.Exceptions;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels.Payment;
using Imate.API.Presentation.ResponseModels.UserManagement;
using IRoleService = Imate.API.Business.Interfaces.UserManagement.IRoleService;

namespace Imate.API.Business.Services.UserManagement
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IAwsS3StorageService _awsS3Service;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoleService _roleService;

        public AccountService(IAccountRepository accountRepository, IUnitOfWork unitOfWork, IRoleService roleService, IAwsS3StorageService awsS3Storage)
        {
            _accountRepository = accountRepository;
            _awsS3Service = awsS3Storage;
            _unitOfWork = unitOfWork;
            _roleService = roleService;
        }
        public async Task<bool> AreUsersExisted(int id)
        {
            return await _accountRepository.AreUsersExisted(id);
        }
        public async Task<PagedList<GetAllAccountResponse>> GetAllAccountAsync(AccountParams accountParams)
        {

            // 1. Lấy IQueryable<Account> từ Repository.
            //    Sử dụng IQueryable để các thao tác Where, OrderBy được thực thi ở database.
            var query = _accountRepository.GetAllAccount().Where(a => a.AccountRoles.Any(b => b.Role.Name != RoleName.Admin));
            // Sửa tên phương thức để rõ ràng hơn

            // 2. Áp dụng các bộ lọc (Filtering)
            if (!string.IsNullOrWhiteSpace(accountParams.SearchTerm))
            {
                var searchTerm = accountParams.SearchTerm.ToLower().Trim();
                query = query.Where(c => c.FullName.ToLower().Contains(searchTerm));
            }

            if (accountParams.AccountStatus.HasValue)
            {
                query = query.Where(c => c.Status == accountParams.AccountStatus.Value);
            }

            // 3. Áp dụng sắp xếp (Sorting)
            // Luôn phải có một thứ tự sắp xếp để phân trang hoạt động chính xác
            if (!string.IsNullOrWhiteSpace(accountParams.SortBy))
            {
                // 1. KIỂM TRA SortOrder (hướng sắp xếp)
                // Chỉ cho phép "asc", "desc", hoặc bỏ trống (mặc định là "asc")
                var sortOrder = accountParams.SortOrder?.ToLower();
                if (!string.IsNullOrWhiteSpace(sortOrder) && sortOrder != "asc" && sortOrder != "desc")
                {
                    throw new ArgumentException("Giá trị của SortOrder không hợp lệ. Chỉ chấp nhận 'asc' hoặc 'desc'.");
                }

                bool isDescending = sortOrder == "desc";

                // 2. KIỂM TRA SortBy (trường sắp xếp) và áp dụng
                query = accountParams.SortBy.ToLower() switch
                {
                    "fullname" => isDescending
                        ? query.OrderByDescending(q => q.FullName)
                        : query.OrderBy(q => q.FullName),

                    "createdat" => isDescending
                        ? query.OrderByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.CreatedAt),

                    // Nếu SortBy không khớp với bất kỳ case nào ở trên, ném ra lỗi
                    _ => throw new NotFoundException($"Trường sắp xếp '{accountParams.SortBy}' không được hỗ trợ.")
                };
            }
            else
            {
                // Sắp xếp mặc định khi không có yêu cầu (hành vi này vẫn giữ nguyên)
                query = query.OrderBy(q => q.Id);
            }

            // 4. Thực hiện phân trang TRÊN IQueryable đã được lọc và sắp xếp
            var pagedAccounts = await PagedList<Account>.CreateAsync(query, accountParams.PageNumber, accountParams.PageSize);

            // 5. Ánh xạ (Map) các item TRONG TRANG HIỆN TẠI sang DTO
            var responseDtos = pagedAccounts.Items.Select(account => new GetAllAccountResponse
            {
                Id = account.Id,
                Email = account.Email,
                FullName = account.FullName,
                AvatarUrl = account.AvatarUrl,
                Status = account.Status,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt,
                Roles = account.AccountRoles
         .Select(ar => ar.Role.Name.ToString())
         .ToList()
            }).ToList();

            // 6. Trả về một PagedList<GetAllAccountResponse> chứa cả dữ liệu và thông tin phân trang
            return new PagedList<GetAllAccountResponse>(
                responseDtos,
                pagedAccounts.TotalCount,
                pagedAccounts.PageNumber,
                pagedAccounts.PageSize
            );

        }

        public async Task<GetAllAccountResponse> GetAccountByIdAsync(int id)
        {
            var account = await _accountRepository.GetByIdAsync(id);
            if (account == null)
            {
                throw new NotFoundException($"Không tìm thấy tài khoản với ID {id}");
            }
            var response = new GetAllAccountResponse
            {
                Id = account.Id,
                Email = account.Email,
                FullName = account.FullName,
                AvatarUrl = account.AvatarUrl,
                Status = account.Status,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt,
                Roles = account.AccountRoles
                     .Select(ar => ar.Role.Name.ToString())
                     .ToList()
            };
            return response;
        }
        public async Task<Account> UpdateAccountStatusAsync(int id, string status)
        {
            // Lấy tài khoản từ repository
            var account = await _accountRepository.GetByIdAsync(id);
            if (account == null)
            {
                throw new NotFoundException($"Không tìm thấy tài khoản với ID {id}");
            }

            // Kiểm tra và parse chuỗi trạng thái thành Enum
            // Giả sử enum của bạn tên là 'AccountStatus'
            if (!Enum.TryParse(status, true, out AccountStatus newStatus) ||
                !Enum.IsDefined(typeof(AccountStatus), newStatus))
            {
                throw new BadRequestException($"Trạng thái '{status}' không hợp lệ.");
            }

            // Gán trạng thái mới cho tài khoản (Đây là dòng còn thiếu)
            account.Status = newStatus;

            // Cập nhật tài khoản trong repository
            await _accountRepository.UpdateAsync(account);

            // Trả về tài khoản đã được cập nhật
            return account;
        }

        //Test đến đây rồi

        public async Task<UserProfileResponse> GetUserProfileAsync(int accountId, string subscription)
        {
            // Lấy thông tin tài khoản đầy đủ từ repository
            var account = await _accountRepository.GetByIdWithDetailsAsync(accountId);

            if (account == null)
            {
                throw new NotFoundException("Không tìm thấy tài khoản người dùng.");
            }

            // Giả định mỗi user có 1 vai trò chính
            var userRole = account.AccountRoles.FirstOrDefault()?.Role.Name
                           ?? throw new Exception("Người dùng không có vai trò hợp lệ.");

            // KIỂM TRA VAI TRÒ VÀ QUYẾT ĐỊNH DỮ LIỆU TRẢ VỀ
            if (userRole.ToString().Equals("Mentor", StringComparison.OrdinalIgnoreCase) && account.Mentor != null)
            {
                return new MentorProfileResponse
                {
                    Id = account.Id,
                    Email = account.Email,
                    FullName = account.FullName,
                    AvatarUrl = account.AvatarUrl,
                    Balance = account.Balance,
                    Role = userRole.ToString(),
                    AccountStatus = account.Status.ToString(),
                    Bio = account.Mentor.Bio,
                    Phone = account.Mentor.Phone,
                    BirthDate = account.Mentor.BirthDate,
                    Yoe = account.Mentor.Yoe,
                    CvUrl = account.Mentor.CvUrl,
                    CertificateUrl = account.Mentor.CertificateUrl,
                    PricePerSession = account.Mentor.PricePerSession,
                    AvgRatings = account.Mentor.AvgRatings,
                    TotalRatingCount = account.Mentor.TotalRatingCount,
                    BankAccountHolderName = account.Mentor.BankAccountHolderName,
                    BankAccountNumber = account.Mentor.BankAccountNumber,
                    BankCode = account.Mentor.BankCode,
                    Skills = account.Mentor.MentorSkills.Select(ms => ms.Skill.Name),
                    Positions = account.Mentor.MentorPositions.Select(mp => mp.Position.Name),
                    Companies = account.Mentor.MentorCompanies.Select(mc => mc.Company.Name),
                    VerificationStatus = account.Mentor.VerificationStatus.ToString()
                };
            }

            else if(userRole.ToString().Equals("Recruiter", StringComparison.OrdinalIgnoreCase) && account.Recruiter != null)
            {
                return new RecruiterProfileResponse
                {
                    Id = account.Id,
                    Email = account.Email,
                    FullName = account.FullName,
                    AvatarUrl = account.AvatarUrl,
                    Balance = account.Balance,
                    Role = userRole.ToString(),
                    AccountStatus = account.Status.ToString(),
                    CompanyName = account.Recruiter.CompanyName,
                    CompanyLogo = account.Recruiter?.CompanyLogo,
                    CompanySize = account.Recruiter?.CompanySize,
                    Address = account.Recruiter?.Address,
                    Website = account.Recruiter?.Website,
                    Industry = account.Recruiter?.Industry,
                    Phone = account.Recruiter?.Phone,
                    VerificationStatus = account.Recruiter?.VerificationStatus.ToString()
                };
            } else
            {
                // Với các vai trò khác, chỉ trả về thông tin cơ bản
                return new UserProfileResponse
                {
                    Id = account.Id,
                    Email = account.Email,
                    FullName = account.FullName,
                    AvatarUrl = account.AvatarUrl,
                    Subscription = subscription,
                    Balance = account.Balance,
                    Role = userRole.ToString(),
                    AccountStatus = account.Status.ToString()
                };
            }
        }

        public async Task UpdateGeneralProfileAsync(int accountId, UpdateGeneralProfileRequest request)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountId)
                  ?? throw new NotFoundException("Không tìm thấy tài khoản.");

            if (request.AvatarFile != null)
            {
                // --- BẮT ĐẦU VALIDATE ẢNH ---

                // 1. Kiểm tra file rỗng
                if (request.AvatarFile.Length == 0)
                {
                    throw new BadRequestException("File ảnh không được rỗng.");
                }

                // 2. Kiểm tra định dạng file (Content-Type và Extension)
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(request.AvatarFile.FileName).ToLowerInvariant();

                // Kiểm tra Content-Type (dựa trên header gửi lên)
                if (!request.AvatarFile.ContentType.StartsWith("image/") || !allowedExtensions.Contains(extension))
                {
                    throw new BadRequestException($"File không hợp lệ. Chỉ chấp nhận các định dạng ảnh: {string.Join(", ", allowedExtensions)}.");
                }

                // 3. (Tuỳ chọn) Kiểm tra kích thước file (ví dụ: giới hạn 5MB)
                const long maxFileSize = 5 * 1024 * 1024; // 5MB
                if (request.AvatarFile.Length > maxFileSize)
                {
                    throw new BadRequestException("Dung lượng ảnh quá lớn. Vui lòng tải lên ảnh nhỏ hơn 5MB.");
                }

                // --- KẾT THÚC VALIDATE ---

                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(account.AvatarUrl))
                {
                    await _awsS3Service.DeleteFileAsync(account.AvatarUrl);
                }

                // Upload ảnh mới
                account.AvatarUrl = await _awsS3Service.UploadFileAsync(request.AvatarFile, "avatars");
            }

            account.FullName = request.FullName;
            await _unitOfWork.Accounts.UpdateAsync(account);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task SubmitMentorProfileAsync(int accountId, UpdateMentorProfileRequest request)
        {
            if (request == null)
                throw new BadRequestException("Dữ liệu hồ sơ Mentor không hợp lệ.");

            // Lấy account kèm theo navigation Mentor
            var account = await _unitOfWork.Accounts.GetByIdMentor(accountId)
                ?? throw new NotFoundException("Không tìm thấy tài khoản.");

            // Chỉ cho phép tài khoản role Mentor nộp hồ sơ
            var primaryRole = account.AccountRoles.FirstOrDefault()?.Role.Name;
            if (primaryRole != RoleName.Mentor)
            {
                throw new BadRequestException("Chỉ tài khoản Mentor mới có thể nộp hồ sơ Mentor.");
            }

            // Parse BirthDate nếu có
            DateOnly? birthDate = null;
            if (!string.IsNullOrWhiteSpace(request.BirthDate))
            {
                if (DateOnly.TryParse(request.BirthDate, out var parsed))
                {
                    if (parsed > DateOnly.FromDateTime(DateTime.UtcNow))
                    {
                        throw new BadRequestException("Ngày sinh không được ở trong tương lai.");
                    }
                    birthDate = parsed;
                }
                else
                {
                    throw new BadRequestException("Định dạng ngày sinh không hợp lệ. Vui lòng sử dụng định dạng yyyy-MM-dd.");
                }
            }

            if (account.Mentor == null)
            {
                var mentor = new Mentor
                {
                    AccountId = account.Id,
                    Bio = request.Bio,
                    Phone = request.Phone,
                    BirthDate = birthDate,
                    Yoe = request.Yoe ?? 0,
                    CvUrl = request.CvFile != null ? await _awsS3Service.UploadFileAsync(request.CvFile, "mentors/cvs") : null,
                    CertificateUrl = request.CertificateFile != null ? await _awsS3Service.UploadFileAsync(request.CertificateFile, "mentors/certificates") : null,
                    PricePerSession = request.PricePerSession ?? 0,
                    BankAccountHolderName = request.BankAccountHolderName,
                    BankAccountNumber = request.BankAccountNumber,
                    BankCode = request.BankCode,
                    AvgRatings = null,
                    TotalRatingCount = null,
                    VerificationStatus = VerificationStatus.Pending
                };

                // Add many-to-many
                if (request.PositionIds?.Any() == true)
                    mentor.MentorPositions = request.PositionIds.Select(id => new MentorPosition { MentorId = account.Id, PositionId = id }).ToList();
                if (request.SkillIds?.Any() == true)
                    mentor.MentorSkills = request.SkillIds.Select(id => new MentorSkill { MentorId = account.Id, SkillId = id }).ToList();
                if (request.CompanyIds?.Any() == true)
                    mentor.MentorCompanies = request.CompanyIds.Select(id => new MentorCompany { MentorId = account.Id, CompanyId = id }).ToList();

                _unitOfWork.Mentors.Create(mentor);
            }
            else
            {
                account.Mentor.Bio = request.Bio;
                account.Mentor.Phone = request.Phone;
                account.Mentor.BirthDate = birthDate;
                account.Mentor.PricePerSession = request.PricePerSession ?? account.Mentor.PricePerSession;
                account.Mentor.BankAccountHolderName = request.BankAccountHolderName;
                account.Mentor.BankAccountNumber = request.BankAccountNumber;
                account.Mentor.BankCode = request.BankCode;
                account.Mentor.VerificationStatus = VerificationStatus.Pending;
                account.Mentor.Yoe = request.Yoe ?? account.Mentor.Yoe;

                if (request.CvFile != null)
                {
                    if (!string.IsNullOrEmpty(account.Mentor.CvUrl))
                        await _awsS3Service.DeleteFileAsync(account.Mentor.CvUrl);
                    account.Mentor.CvUrl = await _awsS3Service.UploadFileAsync(request.CvFile, "mentors/cvs");
                }
                if (request.CertificateFile != null)
                {
                    if (!string.IsNullOrEmpty(account.Mentor.CertificateUrl))
                        await _awsS3Service.DeleteFileAsync(account.Mentor.CertificateUrl);
                    account.Mentor.CertificateUrl = await _awsS3Service.UploadFileAsync(request.CertificateFile, "mentors/certificates");
                }

                // Update many-to-many: Clear and re-add
                account.Mentor.MentorPositions?.Clear();
                if (request.PositionIds?.Any() == true)
                {
                    if (account.Mentor.MentorPositions == null) account.Mentor.MentorPositions = new List<MentorPosition>();
                    foreach (var id in request.PositionIds)
                        account.Mentor.MentorPositions.Add(new MentorPosition { MentorId = account.Id, PositionId = id });
                }

                account.Mentor.MentorSkills?.Clear();
                if (request.SkillIds?.Any() == true)
                {
                    if (account.Mentor.MentorSkills == null) account.Mentor.MentorSkills = new List<MentorSkill>();
                    foreach (var id in request.SkillIds)
                        account.Mentor.MentorSkills.Add(new MentorSkill { MentorId = account.Id, SkillId = id });
                }

                account.Mentor.MentorCompanies?.Clear();
                if (request.CompanyIds?.Any() == true)
                {
                    if (account.Mentor.MentorCompanies == null) account.Mentor.MentorCompanies = new List<MentorCompany>();
                    foreach (var id in request.CompanyIds)
                        account.Mentor.MentorCompanies.Add(new MentorCompany { MentorId = account.Id, CompanyId = id });
                }

                _unitOfWork.Mentors.Update(account.Mentor);
            }

            // Đảm bảo trạng thái account là PendingVerification sau khi nộp hồ sơ
            if (account.Status != AccountStatus.PendingVerification)
            {
                account.Status = AccountStatus.PendingVerification;
                await _unitOfWork.Accounts.UpdateAsync(account);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<AccountMentorResponse> GetAccountDetailMentor(int accountId)
        {
            // 1. Lấy dữ liệu Account/Mentor (Sử dụng IUnitOfWork)
            // (Giả sử GetByIdMentor đã Include(a => a.Mentor))
            var account = await _unitOfWork.Accounts.GetByIdMentor(accountId);
            if (account == null)
            {
                throw new NotFoundException($"Không tìm thấy tài khoản mentor với ID {accountId}");
            }
            if (!account.AccountRoles.Any(ar => ar.Role.Name == RoleName.Mentor))
            {
                throw new BadRequestException($"Tài khoản với ID {accountId} không phải là mentor");
            }

            // 2. --- GỌI REPO MỚI ĐỂ LẤY REVIEWS ---
            // (accountId chính là mentorId trong bảng Bookings)
            var reviews = await _unitOfWork.Bookings.GetMappedReviewsByMentorIdAsync(accountId);

            // LẤY COUNT (MỚI)
            var completedCount = await _unitOfWork.Bookings.CountCompletedBookingsByMentorIdAsync(accountId);

            // 3. Map tất cả dữ liệu
            var response = new AccountMentorResponse
            {
                Id = account.Id,
                FullName = account.FullName,
                Email = account.Email,
                Phone = account.Mentor.Phone,
                Bio = account.Mentor.Bio,
                AvatarUrl = account.AvatarUrl,
                AvgRatings = account.Mentor.AvgRatings,
                PricePerSession = account.Mentor.PricePerSession,
                Status = account.Status.ToString(),
                RoleName = account.AccountRoles.FirstOrDefault()?.Role.Name.ToString(),
                TotalCompletedSessions = completedCount,

                // Gán danh sách review
                Reviews = reviews
            };

            return response;
        }

        public async Task<AccountStaffResponse> GetAccountDetailStaff(int accountId)
        {
            var account = await _unitOfWork.Accounts.GetByIdMentor(accountId);
            if (account == null)
            {
                throw new NotFoundException($"Không tìm thấy tài khoản nhân viên với ID {accountId}");
            }
            if (!account.AccountRoles.Any(ar => ar.Role.Name == RoleName.Staff))
            {
                throw new BadRequestException($"Tài khoản với ID {accountId} không phải là nhân viên");
            }

            var response = new AccountStaffResponse
            {
                Id = account.Id,
                FullName = account.FullName,
                Email = account.Email,
                AvatarUrl = account.AvatarUrl,
                QuestionCount = await _unitOfWork.Questions.GetAllSystemQuestionsForStaff().Where(a => a.CreatorId == accountId).CountAsync(),
                Status = account.Status.ToString(),
                RoleName = account.AccountRoles.FirstOrDefault()?.Role.Name.ToString()
            };
            return response;
        }
        public async Task<AccountCandidateResponse> GetAccountDetailCandidate(int accountId)
        {
            // 1. Lấy tài khoản (Giả sử GetByIdAsync đã Include(a => a.AccountRoles.Role))
            // (Tôi đổi GetByIdMentor thành GetByIdAsync cho đúng logic)
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
            if (account == null)
            {
                throw new NotFoundException($"Không tìm thấy tài khoản ứng viên với ID {accountId}");
            }
            if (!account.AccountRoles.Any(ar => ar.Role.Name == RoleName.Candidate))
            {
                throw new BadRequestException($"Tài khoản với ID {accountId} không phải là ứng viên");
            }

            // --- BẮT ĐẦU LOGIC MỚI ---

            // 2. Lấy tất cả các gói đăng ký của ứng viên
            var allSubscriptions = await _unitOfWork.UserSubscriptions
                                                      .GetSubscriptionsByCandidateIdAsync(accountId);
            // 3. Tìm gói HIỆN TẠI (PresentPackage)
            string presentPackageName = null;
            List<string> exPackageNames = new List<string>();

            if (allSubscriptions != null && allSubscriptions.Count > 0)
            {
                var now = DateOnly.FromDateTime(DateTime.UtcNow);

                // Tìm gói đăng ký nào có ngày hết hạn > hôm nay
                var activeSub = allSubscriptions
                    .FirstOrDefault(sub => sub.Package != null &&
                                           sub.StartDate.AddDays(sub.Package.DurationDays ?? 0) > now);

                if (activeSub != null)
                {
                    presentPackageName = activeSub.Package?.Name;
                }

                // 4. Lấy tất cả các gói ĐÃ TỪNG MUA (ExPackages)
                exPackageNames = allSubscriptions
                    .Where(sub => sub.Package != null)
                    .Select(sub => sub.Package.Name)
                    .Distinct()
                    .ToList();
            }
            // 5. LẤY SỐ LẦN PHỎNG VẤN (Yêu cầu mới)
            int mentorSessionCount = await _unitOfWork.Bookings.CountBookingsCompletedByCandidateIdAsync(accountId);
            // --- KẾT THÚC LOGIC MỚI ---

            // 5. Map kết quả
            var response = new AccountCandidateResponse
            {
                Id = account.Id,
                FullName = account.FullName,
                Email = account.Email,
                AvatarUrl = account.AvatarUrl,
                Status = account.Status.ToString(),
                RoleName = account.AccountRoles.FirstOrDefault()?.Role.Name.ToString(),
                ExPackages = exPackageNames,
                PresentPackage = presentPackageName,
                MentorSessionCount = mentorSessionCount
            };

            return response;
        }

        public async Task<AccountRecruiterResponse> GetAccountDetailRecruiter(int accountId)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
            if (account == null)
            {
                throw new NotFoundException($"Không tìm thấy tài khoản nhà tuyển dụng với ID {accountId}");
            }
            if (!account.AccountRoles.Any(ar => ar.Role.Name == RoleName.Recruiter))
            {
                throw new BadRequestException($"Tài khoản với ID {accountId} không phải là nhà tuyển dụng");
            }

            var recruiter = await _unitOfWork.Recruiters.GetRecruiterByIdAsync(accountId);

            // Count job posts by this recruiter
            var jobPostCount = _unitOfWork.Recruiters.GetJobsByRecruiterId(accountId).Count();

            var response = new AccountRecruiterResponse
            {
                Id = account.Id,
                FullName = account.FullName,
                Email = account.Email,
                AvatarUrl = account.AvatarUrl,
                Status = account.Status.ToString(),
                RoleName = account.AccountRoles.FirstOrDefault()?.Role.Name.ToString(),
                CompanyName = recruiter?.CompanyName ?? string.Empty,
                CompanyLogo = recruiter?.CompanyLogo,
                Website = recruiter?.Website,
                Industry = recruiter?.Industry ?? string.Empty,
                CompanySize = recruiter?.CompanySize,
                Address = recruiter?.Address,
                Phone = recruiter?.Phone,
                VerificationStatus = recruiter?.VerificationStatus.ToString(),
                JobPostCount = jobPostCount
            };

            return response;
        }
        public async Task<AccountDashboardResponseModel> GetAccountOverview()
        {
            // ==================================================================
            // START LOGIC (REFACTORED)
            // ==================================================================

            // 1. Define UTC Time Points (Chỉ dùng DateTime.UtcNow)
            var nowUtc = DateTime.UtcNow;

            // --- Time points for ALL cards ---
            var currentMonthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var previousMonthStart = currentMonthStart.AddMonths(-1);

            // Oldest date needed is for the 5-month chart (Card 1)
            var oldestDateNeeded = currentMonthStart.AddMonths(-4);

            // Time points for Card 3 (New Users)
            var sevenDaysAgo = nowUtc.AddDays(-7);
            var fourteenDaysAgo = nowUtc.AddDays(-14);
            // *** THÊM LẠI TIME POINTS CHO 4 TUẦN ***
            var twentyOneDaysAgo = nowUtc.AddDays(-21);
            var twentyEightDaysAgo = nowUtc.AddDays(-28);


            // 2. Fetch necessary data (CHỈ 2 TRUY VẤN)

            // Query 1: Lấy tổng số lượng (all-time)
            long allTimeTotal = await _unitOfWork.Accounts
      .GetAllAccount()
      .Where(a => a.AccountRoles.Any(b => b.Role.Name != RoleName.Admin))
      .LongCountAsync();

            // Query 2: Lấy TẤT CẢ timestamp cần thiết trong 1 lần
            var recentAccountsCreatedAt = await _unitOfWork.Accounts.GetAllAccount()
                .Where(a => a.CreatedAt >= oldestDateNeeded && a.AccountRoles.Any(b => b.Role.Name != RoleName.Admin)) // Lấy từ ngày xa nhất
                .Select(a => a.CreatedAt)
                .ToListAsync();

            // ==================================================================
            // 3. Process Data In-Memory
            // ==================================================================

            // --- Card 1: Total Users (Dùng `recentAccountsCreatedAt`) ---
            // (Logic này giữ nguyên, không thay đổi)
            var monthlyNewUserLookup = recentAccountsCreatedAt
                .GroupBy(dt => new { dt.Year, dt.Month })
                .ToDictionary(g => new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc), g => (long)g.Count());
            long newUsersCurrentMonth = monthlyNewUserLookup.GetValueOrDefault(currentMonthStart, 0);
            long newUsersPreviousMonth = monthlyNewUserLookup.GetValueOrDefault(previousMonthStart, 0);
            var newUsersTrendCard1 = CalculateTrend(newUsersCurrentMonth, newUsersPreviousMonth);
            var newUsersChartData5Months = new List<long>();
            for (int i = 4; i >= 0; i--)
            {
                newUsersChartData5Months.Add(monthlyNewUserLookup.GetValueOrDefault(currentMonthStart.AddMonths(-i), 0));
            }
            var totalUsersCard = new OverviewCardData
            {
                Value = allTimeTotal,
                Data = newUsersChartData5Months,
                Trend = newUsersTrendCard1
            };

            // --- (LOGIC CARD 2 ĐÃ XÓA) ---

            // --- Card 3: New Users (Created in last 7 days) ---
            // (Dùng `recentAccountsCreatedAt`, không truy vấn DB)

            // Value: Tính tổng 7 ngày gần nhất (vẫn giữ)
            long newUsersLast7Days = recentAccountsCreatedAt.Count(dt => dt >= sevenDaysAgo);

            // Trend: So sánh 7 ngày với 7 ngày trước đó (vẫn giữ)
            long newUsers7To14Days = recentAccountsCreatedAt.Count(dt => dt >= fourteenDaysAgo && dt < sevenDaysAgo);
            var newTrendCard3 = CalculateTrend(newUsersLast7Days, newUsers7To14Days);

            // *** LOGIC MỚI CHO DATA (Quay lại 4 tuần) ***
            // Data: Số lượng user MỚI TẠO mỗi 7 ngày, trong 4 lần gần nhất
            // (Dựa theo ví dụ 21->27, 14->20, 7->13 của bạn)
            long newUsers14To21Days = recentAccountsCreatedAt.Count(dt => dt >= twentyOneDaysAgo && dt < fourteenDaysAgo);
            long newUsers21To28Days = recentAccountsCreatedAt.Count(dt => dt >= twentyEightDaysAgo && dt < twentyOneDaysAgo);

            var newChartData4Weeks = new List<long>
    {
        newUsers21To28Days,  // Block 4 (xa nhất, e.g., 7->13)
        newUsers14To21Days, // Block 3 (e.g., 14->20)
        newUsers7To14Days,  // Block 2 (e.g., 21->27)
        newUsersLast7Days   // Block 1 (gần nhất, e.g., 21->27)
    };
            // *** KẾT THÚC LOGIC MỚI ***

            var newUsersCard = new OverviewCardData
            {
                Value = newUsersLast7Days,      // Value: Created < 7 days (e.g., 21->27)
                Data = newChartData4Weeks,      // Data: Created per 7-day block (last 4 blocks)
                Trend = newTrendCard3           // Trend: Created 7 days vs previous 7 days
            };

            // ==================================================================
            // KẾT THÚC LOGIC
            // ==================================================================

            return new AccountDashboardResponseModel
            {
                TotalUsers = totalUsersCard,
                NewUsers = newUsersCard
            };
        }
        private TrendData CalculateTrend(decimal current, decimal previous)
        {
            if (previous == 0)
            {
                return new TrendData
                {
                    Percentage = (current > 0) ? 100 : 0,
                    IsPositive = (current > 0)
                };
            }
            decimal percentageChange = ((current - previous) / previous) * 100;
            return new TrendData
            {
                Percentage = Math.Abs(Math.Round(percentageChange, 2)),
                IsPositive = percentageChange >= 0
            };
        }

        private TrendData CalculateTrend(long current, long previous)
        {
            return CalculateTrend((decimal)current, (decimal)previous);
        }

        public async Task UpdateUserRoleAsync(int accountId, string role)
        {
            // Validate role
            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentException("Role không được để trống.");
            }

            // Convert string role to RoleName enum
            RoleName roleName;
            if (Enum.TryParse<RoleName>(role, true, out var parsedRole))
            {
                roleName = parsedRole;
            }
            else
            {
                throw new ArgumentException($"Role '{role}' không hợp lệ.");
            }

            // Chỉ cho phép update role Candidate hoặc Mentor
            if (roleName != RoleName.Candidate && roleName != RoleName.Mentor)
            {
                throw new ArgumentException("Chỉ có thể cập nhật role thành Candidate hoặc Mentor.");
            }

            // Kiểm tra account có tồn tại không
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
            {
                throw new NotFoundException("Không tìm thấy tài khoản.");
            }

            // Kiểm tra xem account có phải là account mới không (tạo trong vòng 5 phút)
            // Chỉ cho phép update role nếu account vừa mới tạo
            var accountAge = (DateTime.UtcNow - account.CreatedAt).TotalMinutes;
            if (accountAge > 5)
            {
                throw new InvalidOperationException("Không thể thay đổi vai trò sau khi tài khoản đã được tạo. Vui lòng tạo tài khoản mới nếu muốn sử dụng vai trò khác.");
            }

            // Update role
            await _roleService.UpdateUserRoleAsync(accountId, roleName);

            // Update account status nếu cần (Mentor cần PendingVerification)
            if (roleName == RoleName.Mentor && account.Status != AccountStatus.PendingVerification)
            {
                account.Status = AccountStatus.PendingVerification;
                await _unitOfWork.Accounts.UpdateAsync(account);
                await _unitOfWork.SaveChangesAsync();
            }
            else if (roleName == RoleName.Candidate && account.Status != AccountStatus.Active)
            {
                account.Status = AccountStatus.Active;
                await _unitOfWork.Accounts.UpdateAsync(account);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
