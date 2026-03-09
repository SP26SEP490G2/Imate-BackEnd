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
                    .Where(m => m.Account.Status == Models.Enums.AccountStatus.Active)
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

                // Có thể thêm SearchTerm / SortBy sau nếu cần
                return await PagedList<MentorResponse.ListPreviewMentor>.CreateAsync(
                    query,
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
    }
}
