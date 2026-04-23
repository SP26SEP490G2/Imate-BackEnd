using MediatR;
using Imate.API.Models.Entities;

namespace Imate.API.Presentation.SignalR.Events.Staff
{
    public class MentorReviewCompletedEvent : INotification
    {
        public Account Account { get; }
        public bool IsApproved { get; }
        public string? Note { get; }

        public MentorReviewCompletedEvent(Account account, bool isApproved, string? note)
        {
            Account = account;
            IsApproved = isApproved;
            Note = note;
        }
    }
}
