using Imate.API.Presentation.ResponseModels.Payment;

namespace Imate.API.Presentation.ResponseModels.UserManagement
{
    public class AccountDashboardResponseModel
    {
        //// 1. Tổng số người dùng
        public OverviewCardData TotalUsers { get; set; }
        //// 3. Số người dùng mới (1 tuần)
        public OverviewCardData NewUsers { get; set; }

    }
}
