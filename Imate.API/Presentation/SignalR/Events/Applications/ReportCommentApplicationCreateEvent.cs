using MediatR;
using Imate.API.Models.Entities;

namespace Imate.API.Presentation.SignalR.Events.Applications
{
    public class ReportCommentApplicationCreateEvent : INotification
    {
        public Application Application { get; }

        public ReportCommentApplicationCreateEvent(Application application)
        {
            Application = application;
        }
    }
}
