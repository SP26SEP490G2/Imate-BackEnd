using Imate.API.Presentation.ResponseModels.Mentors;

namespace Imate.API.Business.Interfaces.Mentors
{
    public interface IMentorSlotService
    {
        Task<MentorRecurringSlotsResponse> GetMentorRecurringSlotsAsync(int mentorId);
    }
}
