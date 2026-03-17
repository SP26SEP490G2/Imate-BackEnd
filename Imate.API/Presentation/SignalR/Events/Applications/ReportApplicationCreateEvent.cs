using MediatR;
using Imate.API.Models.Entities;

namespace Imate.API.Presentation.SignalR.Events.Applications
{
    public class ReportApplicationCreateEvent : INotification
    {
        public Application Application { get; }

        public ReportApplicationCreateEvent(Application application)
        {
            Application = application;
        }
    }
}
