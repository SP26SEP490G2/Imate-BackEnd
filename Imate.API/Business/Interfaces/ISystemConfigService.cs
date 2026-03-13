using Imate.API.Presentation.RequestModels;
using Imate.API.Presentation.ResponseModels;

namespace Imate.API.Business.Interfaces
{
    public interface ISystemConfigService
    {
        Task<IEnumerable<SystemConfigResponse>> GetAllConfigsAsync();
        Task<SystemConfigResponse?> GetConfigByKeyAsync(string key);
        Task<SystemConfigResponse> UpdateConfigAsync(string key, string value);
        Task<decimal> GetCommissionRateAsync();
        Task<int> GetFreeInterviewLimitAsync();
        Task<int> GetContributionRewardPointsAsync();
        Task<int> GetEscrowHoursAsync();
        Task<int> GetMinBookingAdvanceHoursAsync();
        Task<int> GetCancellationDeadlineHoursAsync();
        Task<int> GetReportDeadlineHoursAsync();
        Task<decimal> GetCancellationRefundRateAsync();
        Task<int> GetMinDepositAmountAsync();
        Task<int> GetPriceUpdateCooldownDaysAsync();
        Task<int> GetAgoraTokenExpirationHoursAsync();
        Task<int> GetNotificationPageSizeAsync();
        Task<int> GetSubscriptionProMaxLimitAsync();
        Task<int> GetSubscriptionProLimitMultiplierAsync();
        Task<int> GetSubscriptionBasicLimitAsync();
        Task<int> GetSubscriptionRushLimitAsync();
        Task<int> GetInterviewCostPointsAsync();
        Task<int> GetPracticeQuestionCostPointsAsync();
        Task<decimal> GetGuaranteeDepositRateAsync();
    }
}

