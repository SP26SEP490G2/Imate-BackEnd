using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.Recruiters
{
    public interface IRecruiterRepository : IRepositoryBase<Recruiter>
    {
        Task<Recruiter> GetRecruiterByIdAsync(int recruiterAccountId);
        Task<Recruiter> UpdateRecruiterAsync(Recruiter recruiter);
    }
}
