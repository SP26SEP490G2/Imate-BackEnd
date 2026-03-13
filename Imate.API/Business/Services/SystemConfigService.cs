using Imate.API.Business.Interfaces;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels;

namespace Imate.API.Business.Services
{
    public class SystemConfigService : ISystemConfigService
    {
        private readonly IUnitOfWork _unitOfWork;
        private const string COMMISSION_RATE_KEY = "COMMISSION_RATE";
        private const string FREE_INTERVIEW_LIMIT_KEY = "FREE_INTERVIEW_LIMIT";
        private const string CONTRIBUTION_REWARD_POINTS_KEY = "CONTRIBUTION_REWARD_POINTS";
        private const string ESCROW_HOURS_KEY = "ESCROW_HOURS";
        private const string MIN_BOOKING_ADVANCE_HOURS_KEY = "MIN_BOOKING_ADVANCE_HOURS";
        private const string CANCELLATION_DEADLINE_HOURS_KEY = "CANCELLATION_DEADLINE_HOURS";
        private const string REPORT_DEADLINE_HOURS_KEY = "REPORT_DEADLINE_HOURS";
        private const string CANCELLATION_REFUND_RATE_KEY = "CANCELLATION_REFUND_RATE";
        private const string MIN_DEPOSIT_AMOUNT_KEY = "MIN_DEPOSIT_AMOUNT";
        private const string PRICE_UPDATE_COOLDOWN_DAYS_KEY = "PRICE_UPDATE_COOLDOWN_DAYS";
        private const string AGORA_TOKEN_EXPIRATION_HOURS_KEY = "AGORA_TOKEN_EXPIRATION_HOURS";
        private const string NOTIFICATION_PAGE_SIZE_KEY = "NOTIFICATION_PAGE_SIZE";
        private const string SUBSCRIPTION_PRO_MAX_LIMIT_KEY = "SUBSCRIPTION_PRO_MAX_LIMIT";
        private const string SUBSCRIPTION_PRO_LIMIT_MULTIPLIER_KEY = "SUBSCRIPTION_PRO_LIMIT_MULTIPLIER";
        private const string SUBSCRIPTION_BASIC_LIMIT_KEY = "SUBSCRIPTION_BASIC_LIMIT";
        private const string SUBSCRIPTION_RUSH_LIMIT_KEY = "SUBSCRIPTION_RUSH_LIMIT";
        private const string INTERVIEW_COST_POINTS_KEY = "INTERVIEW_COST_POINTS";
        private const string PRACTICE_QUESTION_COST_POINTS_KEY = "PRACTICE_QUESTION_COST_POINTS";
        private const string GUARANTEE_DEPOSIT_RATE_KEY = "GUARANTEE_DEPOSIT_RATE";

        public SystemConfigService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<SystemConfigResponse>> GetAllConfigsAsync()
        {
            var configs = await _unitOfWork.SystemConfigs.GetAllAsync();
            return configs.Select(c => new SystemConfigResponse
            {
                Id = c.Id,
                Key = c.Key,
                Value = c.Value,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            });
        }

        public async Task<SystemConfigResponse?> GetConfigByKeyAsync(string key)
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(key);
            if (config == null)
                return null;

            return new SystemConfigResponse
            {
                Id = config.Id,
                Key = config.Key,
                Value = config.Value,
                Description = config.Description,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };
        }

        public async Task<SystemConfigResponse> UpdateConfigAsync(string key, string value)
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(key);
            
            if (config == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy cấu hình với key: {key}");
            }

            // Validate value based on key
            if (key == COMMISSION_RATE_KEY)
            {
                if (!decimal.TryParse(value, out decimal rate) || rate < 0 || rate > 100)
                {
                    throw new ArgumentException("Hoa hồng phải là số từ 0 đến 100");
                }
            }
            else if (key == FREE_INTERVIEW_LIMIT_KEY)
            {
                if (!int.TryParse(value, out int limit) || limit < 0)
                {
                    throw new ArgumentException("Số lượt phỏng vấn miễn phí phải là số nguyên dương");
                }
            }
            else if (key == CONTRIBUTION_REWARD_POINTS_KEY)
            {
                if (!int.TryParse(value, out int points) || points < 0)
                {
                    throw new ArgumentException("Số điểm thưởng đóng góp phải là số nguyên dương");
                }
            }
            else if (key == ESCROW_HOURS_KEY || key == MIN_BOOKING_ADVANCE_HOURS_KEY || 
                     key == CANCELLATION_DEADLINE_HOURS_KEY || key == REPORT_DEADLINE_HOURS_KEY ||
                     key == PRICE_UPDATE_COOLDOWN_DAYS_KEY || key == AGORA_TOKEN_EXPIRATION_HOURS_KEY ||
                     key == NOTIFICATION_PAGE_SIZE_KEY ||                      key == SUBSCRIPTION_PRO_MAX_LIMIT_KEY ||
                     key == SUBSCRIPTION_PRO_LIMIT_MULTIPLIER_KEY || key == SUBSCRIPTION_BASIC_LIMIT_KEY ||
                     key == SUBSCRIPTION_RUSH_LIMIT_KEY || key == MIN_DEPOSIT_AMOUNT_KEY ||
                     key == INTERVIEW_COST_POINTS_KEY || key == PRACTICE_QUESTION_COST_POINTS_KEY)
            {
                if (!int.TryParse(value, out int intValue) || intValue < 0)
                {
                    throw new ArgumentException("Giá trị phải là số nguyên dương");
                }

                // Validate REPORT_DEADLINE_HOURS <= ESCROW_HOURS
                if (key == REPORT_DEADLINE_HOURS_KEY)
                {
                    var escrowConfig = await _unitOfWork.SystemConfigs.GetByKeyAsync(ESCROW_HOURS_KEY);
                    int escrowHours = escrowConfig != null && int.TryParse(escrowConfig.Value, out int escrow) ? escrow : 24;
                    
                    if (intValue > escrowHours)
                    {
                        throw new ArgumentException($"Thời gian tối đa report ({intValue} giờ) phải bé hơn hoặc bằng thời gian escrow ({escrowHours} giờ)");
                    }
                }

                // Validate ESCROW_HOURS >= REPORT_DEADLINE_HOURS
                if (key == ESCROW_HOURS_KEY)
                {
                    var reportDeadlineConfig = await _unitOfWork.SystemConfigs.GetByKeyAsync(REPORT_DEADLINE_HOURS_KEY);
                    int reportDeadlineHours = reportDeadlineConfig != null && int.TryParse(reportDeadlineConfig.Value, out int reportDeadline) ? reportDeadline : 24;
                    
                    if (intValue < reportDeadlineHours)
                    {
                        throw new ArgumentException($"Thời gian escrow ({intValue} giờ) phải lớn hơn hoặc bằng thời gian tối đa report ({reportDeadlineHours} giờ)");
                    }
                }
            }
            else if (key == CANCELLATION_REFUND_RATE_KEY)
            {
                if (!decimal.TryParse(value, out decimal rate) || rate < 0 || rate > 100)
                {
                    throw new ArgumentException("Tỷ lệ hoàn tiền phải là số từ 0 đến 100");
                }
            }
            else if (key == GUARANTEE_DEPOSIT_RATE_KEY)
            {
                if (!decimal.TryParse(value, out decimal rate) || rate < 0 || rate > 100)
                {
                    throw new ArgumentException("Tỷ lệ tiền đảm bảo phải là số từ 0 đến 100");
                }
            }

            config.Value = value;
            config.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SystemConfigs.UpdateAsync(config);
            await _unitOfWork.SaveChangesAsync();

            return new SystemConfigResponse
            {
                Id = config.Id,
                Key = config.Key,
                Value = config.Value,
                Description = config.Description,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };
        }

        public async Task<decimal> GetCommissionRateAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(COMMISSION_RATE_KEY);
            if (config != null && decimal.TryParse(config.Value, out decimal rate))
            {
                return rate;
            }
            // Default to 20% if not configured
            return 20.0m;
        }

        public async Task<int> GetFreeInterviewLimitAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(FREE_INTERVIEW_LIMIT_KEY);
            if (config != null && int.TryParse(config.Value, out int limit))
            {
                return limit;
            }
            // Default to 3 if not configured
            return 3;
        }

        public async Task<int> GetContributionRewardPointsAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(CONTRIBUTION_REWARD_POINTS_KEY);
            if (config != null && int.TryParse(config.Value, out int points))
            {
                return points;
            }
            // Default to 1000 if not configured
            return 1000;
        }

        public async Task<int> GetEscrowHoursAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(ESCROW_HOURS_KEY);
            if (config != null && int.TryParse(config.Value, out int hours))
            {
                return hours;
            }
            return 24; // Default 24 hours
        }

        public async Task<int> GetMinBookingAdvanceHoursAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(MIN_BOOKING_ADVANCE_HOURS_KEY);
            if (config != null && int.TryParse(config.Value, out int hours))
            {
                return hours;
            }
            return 6; // Default 6 hours
        }

        public async Task<int> GetCancellationDeadlineHoursAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(CANCELLATION_DEADLINE_HOURS_KEY);
            if (config != null && int.TryParse(config.Value, out int hours))
            {
                return hours;
            }
            return 24; // Default 24 hours
        }

        public async Task<int> GetReportDeadlineHoursAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(REPORT_DEADLINE_HOURS_KEY);
            if (config != null && int.TryParse(config.Value, out int hours))
            {
                return hours;
            }
            return 24; // Default 24 hours
        }

        public async Task<decimal> GetCancellationRefundRateAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(CANCELLATION_REFUND_RATE_KEY);
            if (config != null && decimal.TryParse(config.Value, out decimal rate))
            {
                return rate;
            }
            return 80.0m; // Default 80%
        }

        public async Task<int> GetMinDepositAmountAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(MIN_DEPOSIT_AMOUNT_KEY);
            if (config != null && int.TryParse(config.Value, out int amount))
            {
                return amount;
            }
            return 1000; // Default 1000 VNĐ
        }

        public async Task<int> GetPriceUpdateCooldownDaysAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(PRICE_UPDATE_COOLDOWN_DAYS_KEY);
            if (config != null && int.TryParse(config.Value, out int days))
            {
                return days;
            }
            return 7; // Default 7 days
        }

        public async Task<int> GetAgoraTokenExpirationHoursAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(AGORA_TOKEN_EXPIRATION_HOURS_KEY);
            if (config != null && int.TryParse(config.Value, out int hours))
            {
                return hours;
            }
            return 1; // Default 1 hour
        }

        public async Task<int> GetNotificationPageSizeAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(NOTIFICATION_PAGE_SIZE_KEY);
            if (config != null && int.TryParse(config.Value, out int size))
            {
                return size;
            }
            return 20; // Default 20
        }

        public async Task<int> GetSubscriptionProMaxLimitAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(SUBSCRIPTION_PRO_MAX_LIMIT_KEY);
            if (config != null && int.TryParse(config.Value, out int limit))
            {
                return limit;
            }
            return 7300; // Default 20 * 365
        }

        public async Task<int> GetSubscriptionProLimitMultiplierAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(SUBSCRIPTION_PRO_LIMIT_MULTIPLIER_KEY);
            if (config != null && int.TryParse(config.Value, out int multiplier))
            {
                return multiplier;
            }
            return 20; // Default 20 per day
        }

        public async Task<int> GetSubscriptionBasicLimitAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(SUBSCRIPTION_BASIC_LIMIT_KEY);
            if (config != null && int.TryParse(config.Value, out int limit))
            {
                return limit;
            }
            return 3; // Default 3
        }

        public async Task<int> GetSubscriptionRushLimitAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(SUBSCRIPTION_RUSH_LIMIT_KEY);
            if (config != null && int.TryParse(config.Value, out int limit))
            {
                return limit;
            }
            return 20; // Default 20
        }

        public async Task<int> GetInterviewCostPointsAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(INTERVIEW_COST_POINTS_KEY);
            if (config != null && int.TryParse(config.Value, out int cost))
            {
                return cost;
            }
            return 2000; // Default 2000 Imateints
        }

        public async Task<int> GetPracticeQuestionCostPointsAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(PRACTICE_QUESTION_COST_POINTS_KEY);
            if (config != null && int.TryParse(config.Value, out int cost))
            {
                return cost;
            }
            return 1000; // Default 1000 Imateints
        }

        public async Task<decimal> GetGuaranteeDepositRateAsync()
        {
            var config = await _unitOfWork.SystemConfigs.GetByKeyAsync(GUARANTEE_DEPOSIT_RATE_KEY);
            if (config != null && decimal.TryParse(config.Value, out decimal rate))
            {
                return rate;
            }
            // Default to commission rate if not configured (backward compatibility)
            return await GetCommissionRateAsync();
        }
    }
}

