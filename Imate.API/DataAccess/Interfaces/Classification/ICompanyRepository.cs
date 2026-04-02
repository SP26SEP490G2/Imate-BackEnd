using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels.Classification;

namespace Imate.API.DataAccess.Interfaces.Classification
{
    public interface ICompanyRepository
    {
        Task<Company?> GetByIdAsync(int id);
        Task AddAsync(Company company); 
        Task UpdateAsync(Company company); 
        Task<Company?> SetStatusAsync(int id, bool isActive);
        Task<(IEnumerable<Company> Items, int TotalCount)> GetPagedListAsync(CompanyListRequestModel request);
        Task<bool> NameExistsAsync(string name);
        Task<bool> NameExistsExcludingIdAsync(string name, int excludeId);
        Task<Company?> GetByNameAsync(string name);
        IQueryable<Company> GetAllQueryable();
    }
}
