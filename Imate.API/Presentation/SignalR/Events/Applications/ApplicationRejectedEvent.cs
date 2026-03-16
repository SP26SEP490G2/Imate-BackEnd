using MediatR;
using Imate.API.Models.Entities;

namespace Imate.API.Presentation.SignalR.Events.Applications
{
    public class ApplicationRejectedEvent : INotification
    {
        public Application Application { get; }

        public ApplicationRejectedEvent(Application application)
        {
            Application = application;
        }
    }
}

