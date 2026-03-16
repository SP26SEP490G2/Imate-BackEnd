using MediatR;
using Imate.API.Models.Entities;

namespace Imate.API.Presentation.SignalR.Events.Applications
{
    public class ApplicationApprovedEvent : INotification
    {
        public Application Application { get; }

        public ApplicationApprovedEvent(Application application)
        {
            Application = application;
        }
    }
}

