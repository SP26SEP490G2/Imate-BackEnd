using Imate.API.Models.Enums;

namespace Imate.API.Presentation.ResponseModels.Mentors
{
    public class BookingResponseModel
    {
        public int Id { get; set; }
        public string MentorName { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; }
        public int Price { get; set; }
        public BookingStatus Status { get; set; }
    }
}
