using MediatR;
using Imate.API.Models.Entities;

namespace Imate.API.Presentation.SignalR.Events.QuestionBanks
{
    public class QuestionApprovedEvent : INotification
    {
        public Question Question { get; }
        public int StaffId { get; }

        public QuestionApprovedEvent(Question question, int staffId)
        {
            Question = question;
            StaffId = staffId;
        }
    }
}

