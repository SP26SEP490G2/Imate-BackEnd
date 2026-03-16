using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.Applications
{
    public interface IApplicationRepository
    {
        IQueryable<Application> GetAllApplications();
        void AddApplication(Application application);
    }
}
