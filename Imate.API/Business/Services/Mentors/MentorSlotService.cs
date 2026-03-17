using Imate.API.Business.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Presentation.ResponseModels.Mentors;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Imate.API.Business.Services.Mentors
{
    public class MentorSlotService : IMentorSlotService
    {
        private readonly IUnitOfWork _unitOfWork;
        // Using a similar approach as Imate but adapting to IMATE structure
        // Since there's no dedicated MentorRecurringSlot repository in IUnitOfWork yet, 
        // we might need to access it via context or add it. 
        // For now, let's assume we can use the Slot repository or I'll implement it within BookingService 
        // OR add it to UnitOfWork. Let's add it to UnitOfWork for cleanliness.

        public MentorSlotService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<MentorRecurringSlotsResponse> GetMentorRecurringSlotsAsync(int mentorId)
        {
            var mentorSlots = await _unitOfWork.MentorRecurringSlots.GetByMentorIdAsync(mentorId);
            
            var response = new MentorRecurringSlotsResponse
            {
                MentorId = mentorId,
                SlotsByDay = mentorSlots
                    .GroupBy(ms => ms.Slot.DayOfWeek)
                    .Select(g => new SlotsByDayResponse
                    {
                        DayOfWeek = g.Key,
                        DayName = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName((DayOfWeek)g.Key),
                        Slots = g.Select(s => new MentorSlotDetailResponse
                        {
                            Id = s.Id,
                            MentorId = s.MentorId,
                            SlotId = s.SlotId,
                            Slot = new SlotDetailResponse
                            {
                                Id = s.Slot.Id,
                                DayOfWeek = s.Slot.DayOfWeek,
                                DayOfWeekName = s.Slot.StartTime.ToString("HH:mm") + " - " + s.Slot.EndTime.ToString("HH:mm"),
                                StartTime = s.Slot.StartTime,
                                EndTime = s.Slot.EndTime
                            },
                            IsBooked = false // Logic for checking if specific date is booked would go here or handled client-side
                        }).ToList()
                    })
                    .OrderBy(d => d.DayOfWeek)
                    .ToList()
            };

            return response;
        }
    }
}
