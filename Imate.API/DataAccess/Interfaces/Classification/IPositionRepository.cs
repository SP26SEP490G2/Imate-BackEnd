using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.Classification
{
    public interface IPositionRepository
    {
        Task<List<int>> GetNonExistingPositionIdsAsync(IEnumerable<int> positionIds);
        Task<List<string>> GetNonExistingPositionNames(IEnumerable<string> positionNames);
        Task<List<Position>> FindPositionsByNamesAsync(IEnumerable<string> names);

        IQueryable<Position> GetAllPositions();
        Task<Position> GetPositionByIdAsync(int id);
        Task<Position> AddPositionAsync(Position position);
        Task<Position> UpdatePositionAsync(Position position);
        void SaveChangeAsync();

    }
}
