using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels;
using Imate.API.Presentation.ResponseModels.Mentors;

namespace Imate.API.Business.Services.Mentors
{
    public class MentorService : IMentorService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MentorService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedList<MentorResponse.ListPreviewMentor>> GetListPreviewMentorsAsync(CommonParams mentorParams)
        {
            try
            {
                var query = _unitOfWork.Mentors.FindAll(trackChanges: false)
                    .Where(m => m.Account.Status == Models.Enums.AccountStatus.Active);

                if (mentorParams.PositionId.HasValue)
                {
                    query = query.Where(m => m.MentorPositions.Any(mp => mp.PositionId == mentorParams.PositionId.Value));
                }

                if (!string.IsNullOrEmpty(mentorParams.PositionName))
                {
                    query = query.Where(m => m.MentorPositions.Any(mp => mp.Position.Name.Contains(mentorParams.PositionName)));
                }

                if (!string.IsNullOrEmpty(mentorParams.SkillName))
                {
                    query = query.Where(m => m.MentorSkills.Any(ms => ms.Skill.Name.Contains(mentorParams.SkillName)));
                }

                if (!string.IsNullOrEmpty(mentorParams.CompanyName))
                {
                    query = query.Where(m => m.MentorCompanies.Any(mc => mc.Company.Name.Contains(mentorParams.CompanyName)));
                }

                if (!string.IsNullOrEmpty(mentorParams.SearchTerm))
                {
                    query = query.Where(m => 
                        m.Account.FullName.Contains(mentorParams.SearchTerm) || 
                        m.Bio.Contains(mentorParams.SearchTerm) ||
                        m.MentorPositions.Any(mp => mp.Position.Name.Contains(mentorParams.SearchTerm)) ||
                        m.MentorSkills.Any(ms => ms.Skill.Name.Contains(mentorParams.SearchTerm))
                    );
                }

                var resultQuery = query
                    .OrderByDescending(m => m.AvgRatings)
                    .Select(m => new MentorResponse.ListPreviewMentor
                    {
                        AccountId = m.AccountId,
                        FullName = m.Account.FullName,
                        Position = m.MentorPositions.FirstOrDefault() != null ? m.MentorPositions.FirstOrDefault().Position.Name : string.Empty,
                        Yoe = m.Yoe,
                        Company = m.MentorCompanies.FirstOrDefault() != null ? m.MentorCompanies.FirstOrDefault().Company.Name : string.Empty,
                        AvgRatings = m.AvgRatings,
                        TotalRatingCount = m.TotalRatingCount
                    });

                return await PagedList<MentorResponse.ListPreviewMentor>.CreateAsync(
                    resultQuery,
                    mentorParams.PageNumber,
                    mentorParams.PageSize
                );
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while retrieving mentors.", ex);
            }
        }

        public async Task UpdateMentorProfileAsync(int accountId, UpdateMentorProfileRequest request)
        {
            var mentor = await _unitOfWork.Mentors.GetMentorByIdAsync(accountId)
                ?? throw new NotFoundException("Không tìm thấy hồ sơ Mentor.");

            // QUY TẮC NGHIỆP VỤ: Xử lý cập nhật giá
            if (request.PricePerSession.HasValue && request.PricePerSession != mentor.PricePerSession)
            {
                //if (mentor.PriceLastUpdatedDate.HasValue && mentor.PriceLastUpdatedDate.Value.AddMonths(1) > DateTime.UtcNow)
                //{
                //    var availableDate = mentor.PriceLastUpdatedDate.Value.AddMonths(1);
                //    throw new BadRequestException($"Bạn chỉ có thể cập nhật giá mỗi tháng một lần. Lần cập nhật tiếp theo vào ngày {availableDate:dd/MM/yyyy}.");
                //}
                mentor.PricePerSession = request.PricePerSession.Value;
                mentor.PriceLastUpdatedDate = DateTime.UtcNow;
            }

            mentor.Bio = request.Bio;
            mentor.Phone = request.Phone;
            mentor.PricePerSession = request.PricePerSession.Value;
            mentor.BankAccountHolderName = request.BankAccountHolderName;
            mentor.BankAccountNumber = request.BankAccountNumber;
            mentor.BankCode = request.BankCode;
            mentor.Yoe = request.Yoe ?? mentor.Yoe;

            if (!string.IsNullOrWhiteSpace(request.BirthDate))
            {
                if (DateOnly.TryParse(request.BirthDate, out var parsed))
                {
                    if (parsed > DateOnly.FromDateTime(DateTime.UtcNow))
                    {
                        throw new BadRequestException("Ngày sinh không được ở trong tương lai.");
                    }
                    mentor.BirthDate = parsed;
                }
                else
                {
                    throw new BadRequestException("Định dạng ngày sinh không hợp lệ. Vui lòng sử dụng định dạng yyyy-MM-dd.");
                }
            }

            await _unitOfWork.Mentors.UpdateMentorAsync(mentor);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<CandidateRatingsResponseModel> GetCandidateRatingsAsync(int mentorAccountId)
        {
            // Verify mentor exists
            var mentor = await _unitOfWork.Mentors.GetMentorByIdAsync(mentorAccountId);
            if (mentor == null)
            {
                throw new NotFoundException($"Không tìm thấy mentor với AccountId {mentorAccountId}.");
            }

            // Get all ratings from candidates for this mentor
            var ratings = await _unitOfWork.Bookings.GetCandidateRatingsByMentorIdAsync(mentorAccountId);

            // Calculate total count and average rating
            var totalCount = ratings.Count;
            var averageRating = totalCount > 0
                ? (decimal?)ratings.Average(r => r.RatingScore)
                : null;

            // Round average to 2 decimal places
            if (averageRating.HasValue)
            {
                averageRating = Math.Round(averageRating.Value, 2);
            }

            var response = new CandidateRatingsResponseModel
            {
                TotalRatingCount = totalCount,
                AverageRating = averageRating,
                Ratings = ratings
            };

            return response;
        }

        public async Task UpdateMentorPriceAsync(int accountId, int newPrice)
        {
            var mentor = await _unitOfWork.Mentors.GetMentorByIdAsync(accountId);
            if (mentor == null)
            {
                throw new NotFoundException($"Không tìm thấy mentor với AccountId {accountId}.");
            }

            // Optional: Cooldown logic
            // if (mentor.PriceLastUpdatedDate.HasValue && mentor.PriceLastUpdatedDate.Value.AddMonths(1) > DateTime.UtcNow) ...

            mentor.PricePerSession = newPrice;
            mentor.PriceLastUpdatedDate = DateTime.UtcNow;

            _unitOfWork.Mentors.Update(mentor);
            await _unitOfWork.SaveAsync();
        }
    }
}
