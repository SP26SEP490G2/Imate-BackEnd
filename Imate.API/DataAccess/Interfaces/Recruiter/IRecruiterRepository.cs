using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces
{
    public interface IRecruiterRepository
    {
        Task<Recruiter> GetRecruiterByIdAsync(int recruiterAccountId);
        Task<Recruiter> UpdateRecruiterAsync(Recruiter recruiter);
    }
}
