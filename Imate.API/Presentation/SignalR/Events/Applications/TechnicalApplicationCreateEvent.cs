using MediatR;
using Imate.API.Models.Entities;

namespace Imate.API.Presentation.SignalR.Events.Applications
{
    public class TechnicalApplicationCreateEvent : INotification
    {
        public Application Application { get; }

        public TechnicalApplicationCreateEvent(Application application)
        {
            Application = application;
        }
    }
}
