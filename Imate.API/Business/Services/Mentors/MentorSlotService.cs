using Imate.API.Business.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Presentation.ResponseModels.Mentors;
using Imate.API.Models.Entities;
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

        public async Task<IEnumerable<SlotDetailResponse>> GetAllSlotsAsync()
        {
            var slots = await _unitOfWork.Slots.FindAll(false).ToListAsync();
            return slots.Select(s => new SlotDetailResponse
            {
                Id = s.Id,
                DayOfWeek = s.DayOfWeek,
                DayOfWeekName = s.StartTime.ToString("HH:mm") + " - " + s.EndTime.ToString("HH:mm"),
                StartTime = s.StartTime,
                EndTime = s.EndTime
            });
        }
        public async Task<bool> AddMentorRecurringSlotsAsync(int mentorId, List<int> slotIds)
        {
            var mentor = await _unitOfWork.Mentors.GetMentorByIdAsync(mentorId);
            if (mentor == null)
            {
                throw new KeyNotFoundException("Hồ sơ Mentor chưa được tạo. Vui lòng cập nhật hồ sơ cá nhân trước khi thiết lập lịch hẹn.");
            }

            var existingSlots = await _unitOfWork.MentorRecurringSlots.GetByMentorIdAsync(mentorId);
            var existingSlotIds = existingSlots.Select(s => s.SlotId).ToHashSet();

            var newSlotIds = slotIds.Where(id => !existingSlotIds.Contains(id)).Distinct();

            foreach (var slotId in newSlotIds)
            {
                var mentorSlot = new MentorRecurringSlot
                {
                    MentorId = mentorId,
                    SlotId = slotId,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _unitOfWork.MentorRecurringSlots.Create(mentorSlot);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMentorRecurringSlotAsync(int mentorId, int mentorRecurringSlotId)
        {
            var slot = await _unitOfWork.MentorRecurringSlots
                .FindByCondition(s => s.Id == mentorRecurringSlotId && s.MentorId == mentorId, true)
                .FirstOrDefaultAsync();

            if (slot == null) return false;

            slot.IsActive = false;
            slot.UpdatedAt = DateTimeOffset.UtcNow;
            _unitOfWork.MentorRecurringSlots.Update(slot);

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
