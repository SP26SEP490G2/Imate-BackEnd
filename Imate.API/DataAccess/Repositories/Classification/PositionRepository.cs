using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.DataAccess.Repositories.Classification
{
    public class PositionRepository : IPositionRepository
    {
        private readonly ImateDbContext _context;
        public PositionRepository(ImateDbContext context)
        {
            _context = context;
        }
        public async Task<List<int>> GetNonExistingPositionIdsAsync(IEnumerable<int> positionIds)
        {
            // 1. Xử lý danh sách rỗng/null an toàn
            if (positionIds == null || !positionIds.Any())
            {
                return new List<int>(); // Không có ID nào không tồn tại
            }

            // 2. TÌM ID TỒN TẠI (1 Query)
            var distinctIds = positionIds.Distinct().ToList();
            var existingIds = await _context.Positions
                .Where(c => distinctIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            // 3. TÌM ID KHÔNG TỒN TẠI (LINQ Except)
            var nonExistingIds = distinctIds.Except(existingIds).ToList();

            return nonExistingIds; // Trả về danh sách [99, 101]

        }
        public async Task<List<string>> GetNonExistingPositionNames(IEnumerable<string> positionNames)
        {
            if (positionNames == null || !positionNames.Any())
            {
                return new List<string>();
            }
            var distinctNames = positionNames.Select(name => name.Trim().ToLower()).Distinct().ToList();
            var existingNames = await _context.Positions
                .Where(c => distinctNames.Contains(c.Name.ToLower()))
                .Select(c => c.Name.ToLower())
                .ToListAsync();
            var nonExistingNames = distinctNames.Except(existingNames).ToList();
            return nonExistingNames;
        }
        public async Task<List<Position>> FindPositionsByNamesAsync(IEnumerable<string> names)
        {
            if (names == null || !names.Any())
            {
                return new List<Position>();
            }

            // Chuyển tất cả tên sang chữ thường để tìm kiếm không phân biệt hoa/thường
            var lowerCaseNames = names.Select(n => n.ToLower()).ToList();

            return await _context.Positions
                .Where(s => lowerCaseNames.Contains(s.Name.ToLower()))
                .ToListAsync();
        }
        public IQueryable<Position> GetAllPositions()
        {
            return _context.Positions.AsNoTracking();
        }
        public async Task<Position> GetPositionByIdAsync(int id)
        {
            return await _context.Positions.FirstOrDefaultAsync(c => c.Id == id);
        }
        public async Task<Position> AddPositionAsync(Position position)
        {
            _context.Positions.Add(position);
            await _context.SaveChangesAsync();
            return position;
        }
        public async Task<Position> UpdatePositionAsync(Position position)
        {
            _context.Positions.Update(position);
            await _context.SaveChangesAsync();
            return position;
        }
        public void SaveChangeAsync()
        {
            _context.SaveChangesAsync();
        }
    }
}
