using Microsoft.EntityFrameworkCore;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels.Classification;
using Imate.API.DataAccess.ApplicationDbContext;

namespace Imate.API.DataAccess.Repositories.Classification
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly ImateDbContext _context;

        public CompanyRepository(ImateDbContext context)
        {
            _context = context;
        }

        public async Task<Company?> GetByIdAsync(int id)
        {
            return await _context.Set<Company>()
                                 .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddAsync(Company company)
        {
            _context.Set<Company>().Add(company);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Company company)
        {
            _context.Set<Company>().Update(company);
            await _context.SaveChangesAsync();
        }

        public async Task<Company?> SetStatusAsync(int id, bool isActive)
        {
            var company = await GetByIdAsync(id);
            if (company == null) return null;

            company.IsActive = isActive;

            await UpdateAsync(company);
            return company;
        }

        public async Task<(IEnumerable<Company> Items, int TotalCount)> GetPagedListAsync(CompanyListRequestModel request)
        {
            var query = _context.Set<Company>().AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(c => c.Name.ToLower().Contains(request.SearchTerm.ToLower()));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == request.IsActive.Value);
            }

            var totalCount = await query.CountAsync();

            // Sắp xếp
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                bool isDescending = request.SortOrder?.ToLower() == "desc";

                query = request.SortBy.ToLower() switch
                {
                    "name" => isDescending
                        ? query.OrderByDescending(c => c.Name)
                        : query.OrderBy(c => c.Name),
                    "createdat" => isDescending
                        ? query.OrderByDescending(c => c.CreatedAt)
                        : query.OrderBy(c => c.CreatedAt),
                    "updatedat" => isDescending
                        ? query.OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
                        : query.OrderBy(c => c.UpdatedAt ?? c.CreatedAt),
                    _ => query.OrderByDescending(c => c.Id) // Default sort by Id desc
                };
            }
            else
            {
                // Default sort by Id desc if no sort specified
                query = query.OrderByDescending(c => c.Id);
            }

            var items = await query.Skip((request.PageNumber - 1) * request.PageSize)
                                   .Take(request.PageSize)
                                   .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> NameExistsAsync(string name)
        {
            
            return await _context.Set<Company>()
                                 .AnyAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> NameExistsExcludingIdAsync(string name, int excludeId)
        {
            return await _context.Set<Company>()
                                 .AnyAsync(c => c.Name.ToLower() == name.ToLower() && c.Id != excludeId);
        }
        public async Task<Company?> GetByNameAsync(string name)
        {
            return await _context.Set<Company>()
                                 .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public IQueryable<Company> GetAllQueryable()
        {
            return _context.Set<Company>().AsQueryable();
        }
    }
}
