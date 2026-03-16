using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Imate.API.DataAccess.Interfaces.Applications;

namespace Imate.API.DataAccess.Repositories.Applications
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly ImateDbContext _context;
        public ApplicationRepository(ImateDbContext context)
        {
            _context = context;
        }
        public IQueryable<Application> GetAllApplications()
        {
            return _context.Applications
                .Include(a => a.User)
                .Include(a => a.Reviewer)
                .Include(a => a.Booking);

        }
        public void AddApplication(Application application)
        {
            _context.Applications.Add(application);
        }
    }
}
