using Imate.API.Presentation.ResponseModels.Mentors;

namespace Imate.API.Business.Interfaces.Mentors
{
    public interface IMentorSlotService
    {
        Task<MentorRecurringSlotsResponse> GetMentorRecurringSlotsAsync(int mentorId);
        Task<IEnumerable<SlotDetailResponse>> GetAllSlotsAsync();
        Task<bool> AddMentorRecurringSlotsAsync(int accountId, List<int> slotIds);
        Task<bool> DeleteMentorRecurringSlotAsync(int accountId, int mentorRecurringSlotId);
    }
}
