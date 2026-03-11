using MediatR;
using Imate.API.Models.Entities;
namespace Imate.API.Presentation.SignalR.Events.QuestionBanks
{
    public class QuestionRejectedEvent : INotification
    {
        public Question Question { get; }
        public int StaffId { get; }

        public QuestionRejectedEvent(Question question, int staffId)
        {
            Question = question;
            StaffId = staffId;
        }
    }
}

