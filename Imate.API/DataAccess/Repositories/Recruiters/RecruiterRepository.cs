using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Recruiters;
using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Repositories.Recruiters
{
    public class RecruiterRepository : RepositoryBase<Recruiter>, IRecruiterRepository
    {
        public RecruiterRepository(ImateDbContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
